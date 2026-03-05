using AdminApi.Host.Data;
using AdminApi.Host.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/admin/content-categories")]
public class AdminContentCategoriesController(AdminDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContentCategoryDto>>> GetList()
    {
        var list = await dbContext.ContentCategories
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ContentCategoryDto(
                x.Id,
                x.Name,
                x.SortOrder,
                x.IsEnabled,
                x.UpdatedAt
            ))
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<ContentCategoryDto>> Create([FromBody] CreateContentCategoryRequest request)
    {
        var exists = await dbContext.ContentCategories
            .AnyAsync(x => x.Name == request.Name);
        if (exists)
        {
            return BadRequest("分类名称已存在");
        }

        var now = DateTime.UtcNow;
        var item = new ContentCategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            SortOrder = request.SortOrder,
            IsEnabled = request.IsEnabled,
            UpdatedAt = now,
        };

        dbContext.ContentCategories.Add(item);
        await dbContext.SaveChangesAsync();

        return Ok(new ContentCategoryDto(
            item.Id,
            item.Name,
            item.SortOrder,
            item.IsEnabled,
            item.UpdatedAt
        ));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContentCategoryDto>> Update(Guid id, [FromBody] UpdateContentCategoryRequest request)
    {
        var item = await dbContext.ContentCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        var duplicate = await dbContext.ContentCategories
            .AnyAsync(x => x.Id != id && x.Name == request.Name);
        if (duplicate)
        {
            return BadRequest("分类名称已存在");
        }

        item.Name = request.Name;
        item.SortOrder = request.SortOrder;
        item.IsEnabled = request.IsEnabled;
        item.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(new ContentCategoryDto(
            item.Id,
            item.Name,
            item.SortOrder,
            item.IsEnabled,
            item.UpdatedAt
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await dbContext.ContentCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null)
        {
            return NotFound();
        }

        var used = await dbContext.Contents.AnyAsync(x => x.CategoryId == id);
        if (used)
        {
            return BadRequest("分类已被内容引用，无法删除");
        }

        dbContext.ContentCategories.Remove(item);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
