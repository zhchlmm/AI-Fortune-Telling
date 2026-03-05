using AdminApi.Host.Models;
using AdminApi.Host.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/admin/fortune-sessions")]
public class AdminFortuneSessionsController(AdminDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<FortuneSessionDto>>> GetPaged(
        [FromQuery] string? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.FortuneSessions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(x => x.UserId == userId);
        }

        var ordered = query.OrderByDescending(x => x.CreatedAt);
        var total = await ordered.CountAsync();
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FortuneSessionDto(
                x.Id,
                x.UserId,
                x.FortuneType,
                x.InputSummary,
                x.ResultSummary,
                x.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PagedResult<FortuneSessionDto>(page, pageSize, total, items));
    }
}
