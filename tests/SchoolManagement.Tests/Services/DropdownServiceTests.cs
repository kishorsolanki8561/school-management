using System.Dynamic;
using System.Text.Json;
using FluentAssertions;
using Moq;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;
using SchoolManagement.Models.DTOs;
using SchoolManagement.Models.Enums;
using SchoolManagement.Services.Implementations;
using Xunit;

namespace SchoolManagement.Tests.Services;

public sealed class DropdownServiceTests
{
    private readonly Mock<IReadRepository> _readRepoMock = new();
    private readonly DropdownService       _sut;

    public DropdownServiceTests()
    {
        _sut = new DropdownService(_readRepoMock.Object);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IEnumerable<dynamic> MakeDynamic(params (string name, object value)[][] rows)
    {
        foreach (var row in rows)
        {
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var (k, v) in row)
                expando[k] = v;
            yield return (dynamic)expando;
        }
    }

    private static JsonElement JsonOf(object? value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonDocument.Parse(json).RootElement;
    }

    // ── Unknown key ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_UnknownKey_Throws()
    {
        var request = new DropdownRequest { Key = (DropdownKey)999 };

        var act = async () => await _sut.GetDropdownAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*999*");
    }

    // ── Invalid extra column ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_InvalidExtraColumn_Throws()
    {
        var request = new DropdownRequest
        {
            Key          = DropdownKey.CountryDDL,
            ExtraColumns = new[] { "HackerColumn" },
        };

        var act = async () => await _sut.GetDropdownAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*HackerColumn*");
    }

    // ── Invalid filter key ────────────────────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_InvalidFilterKey_Throws()
    {
        var request = new DropdownRequest
        {
            Key     = DropdownKey.CountryDDL,
            Filters = new Dictionary<string, JsonElement>
            {
                ["DROP TABLE"] = JsonOf(1),
            },
        };

        var act = async () => await _sut.GetDropdownAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*DROP TABLE*");
    }

    // ── Happy path: name + value only ─────────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_CountryDDL_NoExtras_ReturnsNameAndValue()
    {
        var fakeRows = MakeDynamic(
            new[] { ("name", (object)"USA"),    ("value", (object)1) },
            new[] { ("name", (object)"Canada"), ("value", (object)2) });

        _readRepoMock
            .Setup(r => r.QueryDynamicAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(fakeRows);

        var request = new DropdownRequest { Key = DropdownKey.CountryDDL };
        var result  = (await _sut.GetDropdownAsync(request)).ToList();

        result.Should().HaveCount(2);
        result[0]["name"].Should().Be("USA");
        result[0]["value"].Should().Be(1);
        result[1]["name"].Should().Be("Canada");
    }

    // ── Extra columns appear camelCased ───────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_ExtraColumns_KeysAreCamelCased()
    {
        var fakeRows = MakeDynamic(
            new[] { ("name", (object)"USA"), ("value", (object)1), ("code", (object)"US") });

        _readRepoMock
            .Setup(r => r.QueryDynamicAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(fakeRows);

        var request = new DropdownRequest
        {
            Key          = DropdownKey.CountryDDL,
            ExtraColumns = new[] { "Code" },
        };
        var result = (await _sut.GetDropdownAsync(request)).ToList();

        result[0].Keys.Should().Contain("code");
        result[0]["code"].Should().Be("US");
    }

    // ── Filters are passed to the repository ─────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_StateDDL_WithFilter_CallsQueryDynamic()
    {
        _readRepoMock
            .Setup(r => r.QueryDynamicAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(Enumerable.Empty<dynamic>());

        var request = new DropdownRequest
        {
            Key     = DropdownKey.StateDDL,
            Filters = new Dictionary<string, JsonElement>
            {
                ["CountryId"] = JsonOf(3),
            },
        };

        await _sut.GetDropdownAsync(request);

        _readRepoMock.Verify(
            r => r.QueryDynamicAsync(
                It.Is<string>(s => s.Contains("@CountryId")),
                It.IsNotNull<object>()),
            Times.Once);
    }

    // ── Empty result is not null ──────────────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_EmptyResult_ReturnsEmptyEnumerable()
    {
        _readRepoMock
            .Setup(r => r.QueryDynamicAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(Enumerable.Empty<dynamic>());

        var request = new DropdownRequest { Key = DropdownKey.RolesDDL };
        var result  = await _sut.GetDropdownAsync(request);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ── Multiple extra columns ────────────────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_MultipleExtraColumns_AllCamelCased()
    {
        var fakeRows = MakeDynamic(
            new[]
            {
                ("name",       (object)"Main Menu"),
                ("value",      (object)1),
                ("parentMenuId", (object)0),
                ("hasChild",   (object)true),
            });

        _readRepoMock
            .Setup(r => r.QueryDynamicAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(fakeRows);

        var request = new DropdownRequest
        {
            Key          = DropdownKey.MenuDDL,
            ExtraColumns = new[] { "ParentMenuId", "HasChild" },
        };
        var result = (await _sut.GetDropdownAsync(request)).ToList();

        result[0].Keys.Should().Contain("parentMenuId");
        result[0].Keys.Should().Contain("hasChild");
    }

    // ── IsOrgRole filter on RolesDDL ─────────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_IsOrgRoleFilter_DoesNotThrow()
    {
        _readRepoMock
            .Setup(r => r.QueryDynamicAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(Enumerable.Empty<dynamic>());

        var request = new DropdownRequest
        {
            Key     = DropdownKey.RolesDDL,
            Filters = new Dictionary<string, JsonElement>
            {
                ["IsOrgRole"] = JsonOf(true),
            },
        };

        var act = async () => await _sut.GetDropdownAsync(request);
        await act.Should().NotThrowAsync();
    }

    // ── SQL contains correct table name ───────────────────────────────────────

    [Fact]
    public async Task GetDropdownAsync_PageDDL_SqlContainsPageMasters()
    {
        _readRepoMock
            .Setup(r => r.QueryDynamicAsync(It.IsAny<string>(), It.IsAny<object?>()))
            .ReturnsAsync(Enumerable.Empty<dynamic>());

        var request = new DropdownRequest { Key = DropdownKey.PageDDL };
        await _sut.GetDropdownAsync(request);

        _readRepoMock.Verify(
            r => r.QueryDynamicAsync(
                It.Is<string>(s => s.Contains("[PageMasters]")),
                It.IsAny<object?>()),
            Times.Once);
    }
}
