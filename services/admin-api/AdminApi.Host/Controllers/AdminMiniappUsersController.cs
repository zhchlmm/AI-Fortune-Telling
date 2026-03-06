using AdminApi.Host.Data;
using AdminApi.Host.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Host.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/admin/miniapp-users")]
public class AdminMiniappUsersController(AdminDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminMiniappUserDto>>> GetPaged(
        [FromQuery] string? keyword,
        [FromQuery] bool? isBlocked,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.MiniappUsers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(x =>
                x.OpenId.Contains(term)
                || (x.Nickname != null && x.Nickname.Contains(term))
                || (x.PhoneNumber != null && x.PhoneNumber.Contains(term))
                || (x.Email != null && x.Email.Contains(term)));
        }

        if (isBlocked.HasValue)
        {
            query = query.Where(x => x.IsBlocked == isBlocked.Value);
        }

        var ordered = query.OrderByDescending(x => x.UpdatedAt);
        var total = await ordered.CountAsync();
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminMiniappUserDto(
                x.Id,
                x.OpenId,
                x.Nickname,
                x.Email,
                x.PhoneNumber,
                x.IsBlocked,
                x.UpdatedAt
            ))
            .ToListAsync();

        return Ok(new PagedResult<AdminMiniappUserDto>(page, pageSize, total, items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminMiniappUserDetailDto>> GetDetail(Guid id)
    {
        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        return Ok(new AdminMiniappUserDetailDto(
            user.Id,
            user.OpenId,
            user.Nickname,
            user.Avatar,
            user.Email,
            user.PhoneNumber,
            user.IsBlocked,
            user.BlockedAt,
            user.CreatedAt,
            user.UpdatedAt
        ));
    }

    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id)
    {
        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        user.IsBlocked = true;
        user.BlockedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return Ok(new { message = "用户已封禁" });
    }

    [HttpPost("{id:guid}/unblock")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        var user = await dbContext.MiniappUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        user.IsBlocked = false;
        user.BlockedAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return Ok(new { message = "用户已解封" });
    }
}
