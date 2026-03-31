using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Constants;
using SchoolManagement.Common.Services;
using SchoolManagement.Common.Utilities;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.DTOs.Auth;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Constants;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly SchoolManagementDbContext _context;
    private readonly IEmailService            _emailService;
    private readonly IReadRepository          _readRepo;
    private readonly JwtSettings              _jwtSettings;
    private readonly EmailSettings            _emailSettings;

    public AuthService(SchoolManagementDbContext context, IEmailService emailService,
                       IReadRepository readRepo)
    {
        _context       = context;
        _emailService  = emailService;
        _readRepo      = readRepo;
        _jwtSettings   = InitializeConfiguration.JwtSettings;
        _emailSettings = InitializeConfiguration.EmailSettings;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted && u.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException(AppMessages.Auth.InvalidCredentials);

        if (!HashingUtility.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException(AppMessages.Auth.InvalidCredentials);

        return await GenerateTokensAsync(user, cancellationToken);
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Duplicate username / email check
        var exists = await _context.Users
            .AnyAsync(u => u.Username == request.Username || u.Email == request.Email, cancellationToken);

        if (exists)
            throw new InvalidOperationException(AppMessages.Auth.UsernameTaken);

        // 2. Keep only non-zero RoleIds / OrgIds
        var distinctRoleIds = request.RoleIds?.Where(s => s != 0).Distinct().ToList() ?? new List<int>();
        var distinctOrgIds  = request.OrgIds?.Where(s => s != 0).Distinct().ToList() ?? new List<int>();

        // 4. Build user + all mappings — use User navigation property so EF Core resolves
        //    the FK after the INSERT and the AuditInterceptor can link ParentAuditLogId
        var user = new User
        {
            Username     = request.Username,
            Email        = request.Email,
            PasswordHash = HashingUtility.HashPassword(request.Password),
        };

        var roleMappings = distinctRoleIds
            .Select(roleId => new UserRoleMapping { User = user, RoleId = roleId })
            .ToList();

        var orgMappings = distinctOrgIds
            .Select(orgId => new UserOrganizationMapping { User = user, OrgId = orgId })
            .ToList();

        await _context.Users.AddAsync(user, cancellationToken);
        if (roleMappings.Count > 0)
            await _context.UserRoleMappings.AddRangeAsync(roleMappings, cancellationToken);
        if (orgMappings.Count > 0)
            await _context.UserOrganizationMappings.AddRangeAsync(orgMappings, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return await GenerateTokensAsync(user, cancellationToken);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(AppMessages.Auth.InvalidToken);

        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.Token == request.RefreshToken &&
                rt.UserId == int.Parse(userId) &&
                !rt.IsRevoked,
                cancellationToken)
            ?? throw new UnauthorizedAccessException(AppMessages.Auth.RefreshTokenInvalid);

        if (DateTimeUtility.IsExpired(storedToken.ExpiresAt))
            throw new UnauthorizedAccessException(AppMessages.Auth.RefreshTokenExpired);

        var user = storedToken.User!;
        var newTokens = await GenerateTokensAsync(user, cancellationToken);

        // Revoke old token and link to new
        storedToken.IsRevoked = true;
        storedToken.ReplacedByToken = newTokens.RefreshToken;
        await _context.SaveChangesAsync(cancellationToken);

        return newTokens;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked, cancellationToken);

        if (token is not null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // Always return same response to prevent email enumeration
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted && u.IsActive, cancellationToken);

        if (user is null)
            return;

        // Invalidate any existing unused tokens for this user
        var existingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var t in existingTokens)
            t.IsUsed = true;

        // Generate a new secure token
        var tokenBytes = new byte[64];
        RandomNumberGenerator.Fill(tokenBytes);
        var tokenValue = Convert.ToBase64String(tokenBytes);

        var resetToken = new PasswordResetToken
        {
            UserId    = user.Id,
            Token     = tokenValue,
            ExpiresAt = DateTime.UtcNow.AddHours(_emailSettings.TokenExpirationHours)
        };

        await _context.PasswordResetTokens.AddAsync(resetToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var resetUrl = $"{_emailSettings.ResetPasswordBaseUrl}?token={Uri.EscapeDataString(tokenValue)}";
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, resetUrl, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token && !t.IsUsed, cancellationToken)
            ?? throw new InvalidOperationException(AppMessages.Auth.ResetTokenInvalid);

        if (DateTimeUtility.IsExpired(resetToken.ExpiresAt))
            throw new InvalidOperationException(AppMessages.Auth.ResetTokenInvalid);

        var user = resetToken.User!;
        user.PasswordHash = HashingUtility.HashPassword(request.NewPassword);

        // Mark token as used
        resetToken.IsUsed = true;

        // Revoke all refresh tokens so existing sessions are invalidated
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var rt in refreshTokens)
            rt.IsRevoked = true;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<LoginResponse> SwitchSchoolAsync(int userId, int orgId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive, cancellationToken)
            ?? throw new UnauthorizedAccessException(AppMessages.Auth.InvalidToken);

        // Confirm the user belongs to the target org
        var isMember = await _context.UserOrganizationMappings
            .AnyAsync(m => m.UserId == userId && m.OrgId == orgId && !m.IsDeleted, cancellationToken);

        if (!isMember)
            throw new UnauthorizedAccessException("User is not a member of the requested organisation.");

        return await GenerateTokensAsync(user, cancellationToken, overrideOrgId: orgId);
    }

    private async Task<LoginResponse> GenerateTokensAsync(User user, CancellationToken cancellationToken, int? overrideOrgId = null)
    {
        // Load role names + ids in one projection (no Include needed)
        var roleMappings = await _context.UserRoleMappings
            .Where(urm => urm.UserId == user.Id && !urm.IsDeleted)
            .Select(urm => new { urm.RoleId, RoleName = urm.Role!.Name })
            .ToListAsync(cancellationToken);

        var roleNames    = roleMappings.Select(r => r.RoleName).ToList();
        var roleIds      = roleMappings.Select(r => r.RoleId).ToList();
        var isOwnerAdmin = roleIds.Contains((int)UserRole.OwnerAdmin);

        // Resolve active OrgId — OwnerAdmin has no tenant, others use first active org (or override)
        int? activeOrgId = null;
        string? activeOrgName = null;

        if (!isOwnerAdmin)
        {
            if (overrideOrgId.HasValue)
            {
                var org = await _context.Organizations
                    .Where(o => o.Id == overrideOrgId.Value && !o.IsDeleted && o.IsActive)
                    .Select(o => new { o.Id, o.Name })
                    .FirstOrDefaultAsync(cancellationToken);

                if (org != null) { activeOrgId = org.Id; activeOrgName = org.Name; }
            }
            else
            {
                var firstOrg = await _context.UserOrganizationMappings
                    .Where(uom => uom.UserId == user.Id && !uom.IsDeleted)
                    .OrderBy(uom => uom.OrgId)
                    .Select(uom => new { uom.OrgId, OrgName = uom.Organization!.Name })
                    .FirstOrDefaultAsync(cancellationToken);

                if (firstOrg != null) { activeOrgId = firstOrg.OrgId; activeOrgName = firstOrg.OrgName; }
            }
        }

        var accessToken       = GenerateAccessToken(user, roleNames, activeOrgId);
        var refreshTokenValue = GenerateRefreshTokenValue();
        var expiry            = DateTimeUtility.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var refreshToken = new RefreshToken
        {
            Token     = refreshTokenValue,
            UserId    = user.Id,
            ExpiresAt = DateTimeUtility.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Build dynamic menu tree — exactly 2 SQL queries regardless of tree depth
        var menus = roleIds.Count > 0
            ? await GetDynamicMenusAsync(roleIds, isOwnerAdmin, activeOrgId, cancellationToken)
            : new List<DynamicMenuResponse>();

        return new LoginResponse
        {
            AccessToken       = accessToken,
            RefreshToken      = refreshTokenValue,
            AccessTokenExpiry = expiry,
            Username          = user.Username,
            Role              = roleNames.FirstOrDefault() ?? string.Empty,
            OrgId             = activeOrgId,
            OrgName           = activeOrgName,
            Menus             = menus,
        };
    }

    /// <summary>
    /// Fetches the full menu tree visible to the given roles in exactly 3 SQL queries,
    /// then assembles the parent→child→pages→modules→actions hierarchy in memory.
    /// </summary>
    private async Task<IList<DynamicMenuResponse>> GetDynamicMenusAsync(
        IList<int> roleIds, bool isOwnerAdmin, int? orgId, CancellationToken ct)
    {
        // OwnerAdmin uses system-level permissions (OrgId IS NULL in DB).
        // All other users use their org-specific permission copies.
        var param = new { RoleIds = roleIds.ToArray(), OrgId = isOwnerAdmin ? (int?)null : orgId };

        // Query 1: all visible menus (flat)
        var flatMenus = (await _readRepo.QueryAsync<DynamicMenuResponse>(
                             AuthQueries.GetDynamicMenus, param))
                        .ToList();

        if (flatMenus.Count == 0)
            return new List<DynamicMenuResponse>();

        // Query 2: all permitted pages across every menu in one shot
        var flatPages = (await _readRepo.QueryAsync<DynamicPageResponse>(
                             AuthQueries.GetDynamicPages, param))
                        .ToList();

        // Query 3: all permitted module+action rows across every page in one shot
        var moduleRows = (await _readRepo.QueryAsync<ModuleActionRow>(
                              AuthQueries.GetDynamicModules, param))
                         .ToList();

        // Build modules: group (PageId, ModuleId, ModuleName) → collect ActionIds
        var modulesByPage = moduleRows
            .GroupBy(r => new { r.PageId, r.ModuleId, r.ModuleName })
            .Select(g => new DynamicModuleResponse
            {
                Id      = g.Key.ModuleId,
                Name    = g.Key.ModuleName,
                PageId  = g.Key.PageId,
                Actions = g.Select(r => r.ActionId).ToList(),
            })
            .GroupBy(m => m.PageId)
            .ToDictionary(g => g.Key, g => (IList<DynamicModuleResponse>)g.ToList());

        // Assign modules to pages
        foreach (var page in flatPages)
            page.Modules = modulesByPage.GetValueOrDefault(page.Id, new List<DynamicModuleResponse>());

        // Group pages by MenuId and assign to menus
        var pagesByMenu = flatPages
            .GroupBy(p => p.MenuId)
            .ToDictionary(g => g.Key, g => (IList<DynamicPageResponse>)g.ToList());

        var menuById = flatMenus.ToDictionary(m => m.Id);
        foreach (var menu in flatMenus)
            menu.Pages = pagesByMenu.GetValueOrDefault(menu.Id, new List<DynamicPageResponse>());

        // Wire children to their parent nodes
        foreach (var menu in flatMenus.Where(m => m.ParentMenuId.HasValue))
        {
            if (menuById.TryGetValue(menu.ParentMenuId!.Value, out var parent))
                parent.Children.Add(menu);
        }

        // Return only root nodes; children are already nested
        return flatMenus.Where(m => m.ParentMenuId == null).ToList();
    }

    /// <summary>Flat projection row returned by <see cref="AuthQueries.GetDynamicModules"/>.</summary>
    private sealed record ModuleActionRow(
        int        ModuleId,
        string     ModuleName,
        int        PageId,
        ActionType ActionId);

    private string GenerateAccessToken(User user, IEnumerable<string> roleNames, int? orgId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        foreach (var role in roleNames)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (orgId.HasValue)
            claims.Add(new Claim("OrgId", orgId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTimeUtility.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshTokenValue()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)),
            ValidateLifetime = false // Allow expired tokens for refresh
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(AppMessages.Auth.InvalidTokenAlgorithm);

        return principal;
    }
}
