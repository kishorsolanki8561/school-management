using Microsoft.Extensions.Configuration;

namespace SchoolManagement.Common.Helpers;

public interface IFilePathHelper
{
    string GetAbsolutePath(string relativePath);
    string GetUploadPath(string? subfolder = null);
    bool EnsureDirectoryExists(string path);
}

public sealed class FilePathHelper : IFilePathHelper
{
    private readonly string _basePath;
    private readonly string _uploadRoot;

    public FilePathHelper(IConfiguration configuration)
    {
        _basePath = AppContext.BaseDirectory;
        _uploadRoot = configuration["FileStorage:UploadRoot"] ?? Path.Combine(_basePath, "uploads");
    }

    public string GetAbsolutePath(string relativePath) =>
        Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.GetFullPath(Path.Combine(_basePath, relativePath));

    public string GetUploadPath(string? subfolder = null)
    {
        var path = subfolder is null
            ? _uploadRoot
            : Path.Combine(_uploadRoot, subfolder);
        EnsureDirectoryExists(path);
        return path;
    }

    public bool EnsureDirectoryExists(string path)
    {
        if (Directory.Exists(path)) return false;
        Directory.CreateDirectory(path);
        return true;
    }
}
