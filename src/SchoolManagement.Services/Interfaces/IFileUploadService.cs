using Microsoft.AspNetCore.Http;
using SchoolManagement.Models.DTOs;

namespace SchoolManagement.Services.Interfaces;

public interface IFileUploadService
{
    Task<IList<FileUploadResponse>> UploadAsync(
        IList<IFormFile> files,
        int?             pageId,
        int?             orgId,
        CancellationToken ct = default);
}
