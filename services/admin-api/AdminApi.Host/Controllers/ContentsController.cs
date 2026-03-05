using AdminApi.Host.Models;
using AdminApi.Host.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Host.Controllers;

[ApiController]
[Route("api/v1/contents")]
public class ContentsController(AdminDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContentItemDto>>> GetPublished(
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Contents
            .Include(x => x.Category)
            .Where(x => x.IsPublished)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var list = await query
            .OrderByDescending(x => x.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ContentItemDto(
                x.Id,
                x.Title,
                x.Summary,
                x.Content,
                x.CategoryId,
                x.Category != null ? x.Category.Name : "未分类",
                x.IsPublished,
                x.PublishedAt
            ))
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContentItemDto>> GetPublishedDetail(Guid id)
    {
        var item = await dbContext.Contents
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(new ContentItemDto(
            item.Id,
            item.Title,
            item.Summary,
            item.Content,
            item.CategoryId,
            item.Category?.Name ?? "未分类",
            item.IsPublished,
            item.PublishedAt
        ));
    }
}
