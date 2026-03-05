using AdminApi.Host.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Host.Controllers;

[ApiController]
[Route("api/v1/uploads")]
public class UploadsController(IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    [HttpPost("images")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<UploadImageResponse>> UploadImage([FromForm] IFormFile? file)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest("文件不能为空");
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest("文件大小不能超过10MB");
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("仅支持图片文件上传");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = file.ContentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".bin"
            };
        }

        var uploadsDir = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads", "images");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/uploads/images/{fileName}";

        return Ok(new UploadImageResponse(url, fileName, file.Length, file.ContentType));
    }
}
