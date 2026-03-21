using FluentAssertions;
using SchoolManagement.Common.Helpers;
using Xunit;

namespace SchoolManagement.Tests.Common;

public sealed class FilesValidatorTests
{
    private readonly FilesValidator _sut = new();

    private static readonly string[] AllowedExtensions = new[] { "pdf", "docx", "jpg", "png" };
    private static readonly string[] AllowedMimeTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "image/jpeg", "image/png" };
    private const long MaxSize = 5 * 1024 * 1024; // 5 MB

    [Theory]
    [InlineData("document.pdf", true)]
    [InlineData("image.JPG", true)]
    [InlineData("file.exe", false)]
    [InlineData("virus.bat", false)]
    public void IsValidExtension_ShouldMatchAllowedExtensions(string fileName, bool expected)
    {
        _sut.IsValidExtension(fileName, AllowedExtensions).Should().Be(expected);
    }

    [Theory]
    [InlineData(1024, true)]
    [InlineData(5 * 1024 * 1024, true)]
    [InlineData(5 * 1024 * 1024 + 1, false)]
    [InlineData(0, false)]
    public void IsValidSize_ShouldRespectMaxSize(long size, bool expected)
    {
        _sut.IsValidSize(size, MaxSize).Should().Be(expected);
    }

    [Theory]
    [InlineData("application/pdf", true)]
    [InlineData("image/png", true)]
    [InlineData("text/html", false)]
    [InlineData("application/x-msdownload", false)]
    public void IsValidContentType_ShouldMatchAllowedMimeTypes(string contentType, bool expected)
    {
        _sut.IsValidContentType(contentType, AllowedMimeTypes).Should().Be(expected);
    }

    [Fact]
    public void Validate_ShouldReturnSuccess_ForValidFile()
    {
        var result = _sut.Validate("doc.pdf", 1024, "application/pdf", AllowedExtensions, AllowedMimeTypes, MaxSize);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldReturnMultipleErrors_ForInvalidFile()
    {
        var result = _sut.Validate("virus.exe", 10 * 1024 * 1024, "application/x-msdownload",
            AllowedExtensions, AllowedMimeTypes, MaxSize);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
    }
}
