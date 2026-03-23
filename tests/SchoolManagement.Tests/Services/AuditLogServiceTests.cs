using FluentAssertions;
using Moq;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.Common;
using SchoolManagement.Models.Entities;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class AuditLogServiceTests
{
    private readonly Mock<IReadRepository> _readRepoMock = new();
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        _sut = new AuditLogService(_readRepoMock.Object);
    }

    [Fact]
    public async Task GetByEntityAsync_ShouldReturnPagedResult()
    {
        var expected = PagedResult<AuditLog>.Create(
            new List<AuditLog> { new AuditLog { EntityName = "Student" } }, 1, 1, 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<AuditLog>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), 1, 20))
            .ReturnsAsync(expected);

        var result = await _sut.GetByEntityAsync("Student", "some-id", new PaginationRequest());

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnPagedResult()
    {
        var expected = PagedResult<AuditLog>.Create(new List<AuditLog>(), 0, 1, 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<AuditLog>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), 1, 20))
            .ReturnsAsync(expected);

        var result = await _sut.GetByUserAsync("user-id", new PaginationRequest());

        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetByScreenAsync_ShouldReturnPagedResult()
    {
        var expected = PagedResult<AuditLog>.Create(
            new List<AuditLog> { new AuditLog { ScreenName = "Country Management" } }, 1, 1, 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<AuditLog>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), 1, 20))
            .ReturnsAsync(expected);

        var result = await _sut.GetByScreenAsync("Country Management", new PaginationRequest());

        result.Total.Should().Be(1);
        result.Items.First().ScreenName.Should().Be("Country Management");
    }

    [Fact]
    public async Task GetByTableAsync_ShouldReturnPagedResult()
    {
        var expected = PagedResult<AuditLog>.Create(
            new List<AuditLog> { new AuditLog { TableName = "Countries" } }, 1, 1, 20);

        _readRepoMock
            .Setup(r => r.QueryPagedAsync<AuditLog>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), 1, 20))
            .ReturnsAsync(expected);

        var result = await _sut.GetByTableAsync("Countries", new PaginationRequest());

        result.Total.Should().Be(1);
        result.Items.First().TableName.Should().Be("Countries");
    }
}
