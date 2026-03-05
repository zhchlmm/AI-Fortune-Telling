using AdminApi.Host.Data;
using AdminApi.Host.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AdminApi.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/admin/auth-audits")]
public class AdminAuthAuditsController(AdminDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<LoginAuditDto>>> GetPaged(
        [FromQuery] string? username,
        [FromQuery] bool? isSuccess,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = BuildFilteredQuery(username, isSuccess, fromUtc, toUtc);

        var ordered = query.OrderByDescending(x => x.CreatedAt);
        var total = await ordered.CountAsync();
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LoginAuditDto(
                x.Id,
                x.Username,
                x.IpAddress,
                x.IsSuccess,
                x.Reason,
                x.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PagedResult<LoginAuditDto>(page, pageSize, total, items));
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? username,
        [FromQuery] bool? isSuccess,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc)
    {
        var items = await BuildFilteredQuery(username, isSuccess, fromUtc, toUtc)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LoginAuditDto(
                x.Id,
                x.Username,
                x.IpAddress,
                x.IsSuccess,
                x.Reason,
                x.CreatedAt
            ))
            .ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("id,username,ipAddress,isSuccess,reason,createdAt");
        foreach (var item in items)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(item.Id.ToString()),
                EscapeCsv(item.Username),
                EscapeCsv(item.IpAddress),
                EscapeCsv(item.IsSuccess ? "true" : "false"),
                EscapeCsv(item.Reason),
                EscapeCsv(item.CreatedAt.ToUniversalTime().ToString("O"))
            ));
        }

        var content = builder.ToString();
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray();
        var fileName = $"login-audits-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private IQueryable<LoginAuditEntity> BuildFilteredQuery(
        string? username,
        bool? isSuccess,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var query = dbContext.LoginAudits.AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(x => x.Username == username);
        }

        if (isSuccess.HasValue)
        {
            query = query.Where(x => x.IsSuccess == isSuccess.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= toUtc.Value);
        }

        return query;
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        var escaped = text.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
