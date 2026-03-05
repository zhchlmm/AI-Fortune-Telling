using AdminApi.Host.Data;
using AdminApi.Host.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Host.Controllers;

[ApiController]
[Route("api/v1/content-categories")]
public class ContentCategoriesController(AdminDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContentCategoryDto>>> GetEnabledList()
    {
        var list = await dbContext.ContentCategories
            .Where(x => x.IsEnabled)
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
}
