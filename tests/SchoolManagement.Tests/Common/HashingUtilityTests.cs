using FluentAssertions;
using SchoolManagement.Common.Utilities;
using Xunit;

namespace SchoolManagement.Tests.Common;

public sealed class HashingUtilityTests
{
    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHash()
    {
        var hash = HashingUtility.HashPassword("MySecurePassword!");
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_ShouldProduceDifferentHashesForSameInput()
    {
        var hash1 = HashingUtility.HashPassword("password");
        var hash2 = HashingUtility.HashPassword("password");
        hash1.Should().NotBe(hash2); // BCrypt uses unique salt per hash
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatchesHash()
    {
        const string password = "Correct$Horse!Battery";
        var hash = HashingUtility.HashPassword(password);
        HashingUtility.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        var hash = HashingUtility.HashPassword("correct");
        HashingUtility.VerifyPassword("incorrect", hash).Should().BeFalse();
    }
}
