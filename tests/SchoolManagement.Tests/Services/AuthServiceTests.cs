using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Services;
using SchoolManagement.Common.Utilities;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.DTOs.Auth;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class AuthServiceTests : IDisposable
{
    private readonly SchoolManagementDbContext _context;
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]                    = "test-secret-key-must-be-at-least-32-chars!!",
                ["JwtSettings:Issuer"]                       = "TestIssuer",
                ["JwtSettings:Audience"]                     = "TestAudience",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"]   = "7",
                ["EmailSettings:SmtpHost"]                   = "localhost",
                ["EmailSettings:SmtpPort"]                   = "25",
                ["EmailSettings:FromEmail"]                  = "test@test.com",
                ["EmailSettings:ResetPasswordBaseUrl"]       = "https://app.test/reset-password",
                ["EmailSettings:TokenExpirationHours"]       = "24",
            })
            .Build();

        InitializeConfiguration.Initialize(config);

        var options = new DbContextOptionsBuilder<SchoolManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SchoolManagementDbContext(options);
        _sut = new AuthService(_context, _emailServiceMock.Object);
    }

    public void Dispose() => _context.Dispose();

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        await SeedUserAsync("alice", "alice@test.com", "Password1!");

        var result = await _sut.LoginAsync(new LoginRequest { Username = "alice", Password = "Password1!" });

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("alice");
    }

    [Fact]
    public async Task LoginAsync_UnknownUsername_ThrowsUnauthorized()
    {
        var act = () => _sut.LoginAsync(new LoginRequest { Username = "nobody", Password = "x" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        await SeedUserAsync("bob", "bob@test.com", "Correct1!");

        var act = () => _sut.LoginAsync(new LoginRequest { Username = "bob", Password = "Wrong1!" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorized()
    {
        await SeedUserAsync("carol", "carol@test.com", "Pass1!", isActive: false);

        var act = () => _sut.LoginAsync(new LoginRequest { Username = "carol", Password = "Pass1!" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsTokensAndPersistsUser()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "dave",
            Email    = "dave@test.com",
            Password = "Pass1!",
            // RoleIds and OrgId omitted — both optional
        });

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("dave");
        result.Role.Should().BeEmpty(); // no role assigned

        var saved = await _context.Users.FirstOrDefaultAsync(u => u.Username == "dave");
        saved.Should().NotBeNull();
        HashingUtility.VerifyPassword("Pass1!", saved!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithMultipleRolesAndMultipleOrgs_CreatesMappings()
    {
        // Seed roles
        await _context.Roles.AddRangeAsync(
            new SchoolManagement.Models.Entities.Role { Id = 1, Name = "Owner Admin" },
            new SchoolManagement.Models.Entities.Role { Id = 2, Name = "Super Admin" });
        // Seed organisations
        await _context.Organizations.AddRangeAsync(
            new SchoolManagement.Models.Entities.Organization { Id = 1, Name = "Org A" },
            new SchoolManagement.Models.Entities.Organization { Id = 2, Name = "Org B" });
        await _context.SaveChangesAsync();

        await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "multi",
            Email    = "multi@test.com",
            Password = "Pass1!",
            RoleIds  = new List<int> { 1, 2 },
            OrgIds   = new List<int> { 1, 2 },
        });

        var user = await _context.Users.FirstAsync(u => u.Username == "multi");
        var roleMappings = await _context.UserRoleMappings.Where(m => m.UserId == user.Id).ToListAsync();
        var orgMappings  = await _context.UserOrganizationMappings.Where(m => m.UserId == user.Id).ToListAsync();

        roleMappings.Should().HaveCount(2);
        orgMappings.Should().HaveCount(2);
        orgMappings.Select(m => m.OrgId).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task RegisterAsync_WithZeroRoleIdOrOrgId_ZeroIsIgnored()
    {
        // Zero IDs must be silently filtered out — no mapping rows created for them
        await _context.Roles.AddAsync(
            new SchoolManagement.Models.Entities.Role { Id = 1, Name = "Owner Admin" });
        await _context.Organizations.AddAsync(
            new SchoolManagement.Models.Entities.Organization { Id = 1, Name = "Org A" });
        await _context.SaveChangesAsync();

        await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "zerotest",
            Email    = "zero@test.com",
            Password = "Pass1!",
            RoleIds  = new List<int> { 1, 0 },   // 0 must be ignored
            OrgIds   = new List<int> { 1, 0 },   // 0 must be ignored
        });

        var user = await _context.Users.FirstAsync(u => u.Username == "zerotest");
        var roleMappings = await _context.UserRoleMappings.Where(m => m.UserId == user.Id).ToListAsync();
        var orgMappings  = await _context.UserOrganizationMappings.Where(m => m.UserId == user.Id).ToListAsync();

        roleMappings.Should().HaveCount(1);   // only Id=1, zero skipped
        orgMappings.Should().HaveCount(1);    // only Id=1, zero skipped
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ThrowsInvalidOperation()
    {
        await SeedUserAsync("eve", "eve@test.com", "Pass1!");

        var act = () => _sut.RegisterAsync(new RegisterRequest
        {
            Username = "eve",
            Email    = "other@test.com",
            Password = "Pass1!",
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        await SeedUserAsync("frank", "shared@test.com", "Pass1!");

        var act = () => _sut.RegisterAsync(new RegisterRequest
        {
            Username = "newuser",
            Email    = "shared@test.com",
            Password = "Pass1!",
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Refresh ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokens()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "grace", Email = "grace@test.com", Password = "Pass1!",
        });

        var result = await _sut.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken  = login.AccessToken,
            RefreshToken = login.RefreshToken,
        });

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(login.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_OldTokenIsRevokedAfterRefresh()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "hank", Email = "hank@test.com", Password = "Pass1!",
        });

        await _sut.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken  = login.AccessToken,
            RefreshToken = login.RefreshToken,
        });

        var old = await _context.RefreshTokens.FirstAsync(t => t.Token == login.RefreshToken);
        old.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidRefreshToken_ThrowsUnauthorized()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "iris", Email = "iris@test.com", Password = "Pass1!",
        });

        var act = () => _sut.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken  = login.AccessToken,
            RefreshToken = "invalid-token",
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedRefreshToken_ThrowsUnauthorized()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "jake", Email = "jake@test.com", Password = "Pass1!",
        });

        await _sut.LogoutAsync(login.RefreshToken);

        var act = () => _sut.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken  = login.AccessToken,
            RefreshToken = login.RefreshToken,
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesToken()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "kim", Email = "kim@test.com", Password = "Pass1!",
        });

        await _sut.LogoutAsync(login.RefreshToken);

        var token = await _context.RefreshTokens.FirstAsync(t => t.Token == login.RefreshToken);
        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_UnknownToken_DoesNotThrow()
    {
        var act = () => _sut.LogoutAsync("non-existent-token");

        await act.Should().NotThrowAsync();
    }

    // ── Forgot Password ───────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_DoesNotThrowAndDoesNotSendEmail()
    {
        var act = () => _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "ghost@test.com" });

        await act.Should().NotThrowAsync();
        _emailServiceMock.Verify(
            e => e.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_KnownEmail_CreatesResetTokenAndSendsEmail()
    {
        await SeedUserAsync("leo", "leo@test.com", "Pass1!");

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "leo@test.com" });

        var tokenExists = await _context.PasswordResetTokens.AnyAsync(t => !t.IsUsed);
        tokenExists.Should().BeTrue();

        _emailServiceMock.Verify(
            e => e.SendPasswordResetEmailAsync(
                "leo@test.com",
                It.IsAny<string>(),
                It.Is<string>(url => url.Contains("reset-password") && url.Contains("token=")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_CalledTwice_InvalidatesOldTokenBeforeCreatingNew()
    {
        await SeedUserAsync("mia", "mia@test.com", "Pass1!");

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "mia@test.com" });
        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "mia@test.com" });

        var activeCount = await _context.PasswordResetTokens.CountAsync(t => !t.IsUsed);
        activeCount.Should().Be(1);
    }

    [Fact]
    public async Task ForgotPasswordAsync_InactiveUser_DoesNotSendEmail()
    {
        await SeedUserAsync("ned", "ned@test.com", "Pass1!", isActive: false);

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "ned@test.com" });

        _emailServiceMock.Verify(
            e => e.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Reset Password ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordHash()
    {
        await SeedUserAsync("olivia", "olivia@test.com", "OldPass1!");
        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "olivia@test.com" });

        var token = await _context.PasswordResetTokens.FirstAsync(t => !t.IsUsed);

        await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token       = token.Token,
            NewPassword = "NewPass1!",
        });

        var user = await _context.Users.FirstAsync(u => u.Email == "olivia@test.com");
        HashingUtility.VerifyPassword("NewPass1!", user.PasswordHash).Should().BeTrue();
        HashingUtility.VerifyPassword("OldPass1!", user.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_MarksTokenAsUsed()
    {
        await SeedUserAsync("peter", "peter@test.com", "Pass1!");
        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "peter@test.com" });

        var token = await _context.PasswordResetTokens.FirstAsync(t => !t.IsUsed);

        await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token       = token.Token,
            NewPassword = "NewPass1!",
        });

        var used = await _context.PasswordResetTokens.FirstAsync(t => t.Token == token.Token);
        used.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_RevokesAllActiveRefreshTokens()
    {
        var login = await _sut.RegisterAsync(new RegisterRequest
        {
            Username = "quinn", Email = "quinn@test.com", Password = "Pass1!",
        });

        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "quinn@test.com" });
        var token = await _context.PasswordResetTokens.FirstAsync(t => !t.IsUsed);

        await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token       = token.Token,
            NewPassword = "NewPass1!",
        });

        var anyActive = await _context.RefreshTokens
            .Where(rt => rt.Token == login.RefreshToken)
            .AnyAsync(rt => !rt.IsRevoked);

        anyActive.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ThrowsInvalidOperation()
    {
        var act = () => _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token       = "bad-token",
            NewPassword = "NewPass1!",
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_AlreadyUsedToken_ThrowsInvalidOperation()
    {
        await SeedUserAsync("rose", "rose@test.com", "Pass1!");
        await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "rose@test.com" });

        var token = await _context.PasswordResetTokens.FirstAsync(t => !t.IsUsed);
        await _sut.ResetPasswordAsync(new ResetPasswordRequest { Token = token.Token, NewPassword = "New1!" });

        // Use the same token again
        var act = () => _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token       = token.Token,
            NewPassword = "New2!",
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ThrowsInvalidOperation()
    {
        await SeedUserAsync("sam", "sam@test.com", "Pass1!");

        // Insert an already-expired token directly
        var user = await _context.Users.FirstAsync(u => u.Email == "sam@test.com");
        var expired = new PasswordResetToken
        {
            UserId    = user.Id,
            Token     = "expired-token-value",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            IsUsed    = false,
        };
        await _context.PasswordResetTokens.AddAsync(expired);
        await _context.SaveChangesAsync();

        var act = () => _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token       = "expired-token-value",
            NewPassword = "New1!",
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task SeedUserAsync(string username, string email, string password, bool isActive = true)
    {
        var user = new User
        {
            Username     = username,
            Email        = email,
            PasswordHash = HashingUtility.HashPassword(password),
            IsActive     = isActive,
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
}
