using AdminApi.Host.Data;
using AdminApi.Host.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/admin/contents")]
public class AdminContentsController(AdminDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContentItemDto>>> GetList(
        [FromQuery] string? keyword,
        [FromQuery] bool? isPublished,
        [FromQuery] Guid? categoryId)
    {
        var query = dbContext.Contents
            .Include(x => x.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.Title.Contains(keyword) ||
                x.Summary.Contains(keyword) ||
                x.Content.Contains(keyword));
        }

        if (isPublished.HasValue)
        {
            query = query.Where(x => x.IsPublished == isPublished.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var list = await query
            .OrderByDescending(x => x.PublishedAt)
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
    public async Task<ActionResult<ContentItemDto>> GetDetail(Guid id)
    {
        var item = await dbContext.Contents
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

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

    [HttpPost]
    public async Task<ActionResult<ContentItemDto>> Create([FromBody] CreateContentRequest request)
    {
        var category = await dbContext.ContentCategories
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId);
        if (category == null)
        {
            return BadRequest("分类不存在");
        }

        var now = DateTime.UtcNow;
        var item = new ContentItemEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Summary = request.Summary,
            Content = request.Content,
            CategoryId = request.CategoryId,
            IsPublished = request.IsPublished,
            PublishedAt = now
        };

        dbContext.Contents.Add(item);
        await dbContext.SaveChangesAsync();

        return Ok(new ContentItemDto(
            item.Id,
            item.Title,
            item.Summary,
            item.Content,
            item.CategoryId,
            category.Name,
            item.IsPublished,
            item.PublishedAt
        ));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContentItemDto>> Update(Guid id, [FromBody] UpdateContentRequest request)
    {
        var item = await dbContext.Contents.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        var category = await dbContext.ContentCategories
            .FirstOrDefaultAsync(x => x.Id == request.CategoryId);
        if (category == null)
        {
            return BadRequest("分类不存在");
        }

        var willPublishNow = !item.IsPublished && request.IsPublished;

        item.Title = request.Title;
        item.Summary = request.Summary;
        item.Content = request.Content;
        item.CategoryId = request.CategoryId;
        item.IsPublished = request.IsPublished;
        if (willPublishNow)
        {
            item.PublishedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        return Ok(new ContentItemDto(
            item.Id,
            item.Title,
            item.Summary,
            item.Content,
            item.CategoryId,
            category.Name,
            item.IsPublished,
            item.PublishedAt
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await dbContext.Contents.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        dbContext.Contents.Remove(item);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
