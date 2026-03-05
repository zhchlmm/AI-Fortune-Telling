using AdminApi.Host.Models;
using AdminApi.Host.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/admin/ai-audits")]
public class AdminAiAuditsController(AiAuditStore auditStore) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AiAuditSummaryDto>> GetSummary([FromQuery] int windowMinutes = 60)
    {
        windowMinutes = Math.Clamp(windowMinutes, 1, 24 * 60);
        var windowEndUtc = DateTime.UtcNow;
        var windowStartUtc = windowEndUtc.AddMinutes(-windowMinutes);

        var query = auditStore.Query(
            fortuneType: null,
            degraded: null,
            fromUtc: windowStartUtc,
            toUtc: windowEndUtc);

        var totalCalls = await query.CountAsync();
        var degradedCalls = await query.CountAsync(x => x.Degraded);
        var degradedRate = totalCalls == 0
            ? 0
            : Math.Round((double)degradedCalls * 100 / totalCalls, 2);
        var avgElapsedMs = totalCalls == 0
            ? 0
            : Math.Round(await query.AverageAsync(x => (double)x.ElapsedMs), 2);

        return Ok(new AiAuditSummaryDto(
            windowMinutes,
            totalCalls,
            degradedCalls,
            degradedRate,
            avgElapsedMs,
            windowStartUtc,
            windowEndUtc));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AiAuditDto>>> GetPaged(
        [FromQuery] string? fortuneType,
        [FromQuery] bool? degraded,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = auditStore.Query(fortuneType, degraded, fromUtc, toUtc);
        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AiAuditDto(
                x.Id,
                x.FortuneType,
                x.Model,
                x.Degraded,
                x.Reason,
                x.PromptSource,
                x.QuestionLength,
                x.ResultLength,
                x.ElapsedMs,
                x.CreatedAt
            ))
            .ToListAsync();

        return Ok(new PagedResult<AiAuditDto>(page, pageSize, total, items));
    }

    [HttpGet("type-distribution")]
    public async Task<ActionResult<IReadOnlyList<AiAuditTypeDistributionDto>>> GetTypeDistribution(
        [FromQuery] string? fortuneType,
        [FromQuery] bool? degraded,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc)
    {
        var query = auditStore.Query(fortuneType, degraded, fromUtc, toUtc);

        var result = await query
            .GroupBy(x => string.IsNullOrWhiteSpace(x.FortuneType) ? "Unknown" : x.FortuneType)
            .Select(group => new
            {
                FortuneType = group.Key,
                TotalCalls = group.Count(),
                DegradedCalls = group.Count(x => x.Degraded),
                AvgElapsedMs = group.Average(x => (double)x.ElapsedMs)
            })
            .OrderByDescending(x => x.TotalCalls)
            .Select(x => new AiAuditTypeDistributionDto(
                x.FortuneType,
                x.TotalCalls,
                x.DegradedCalls,
                x.TotalCalls == 0 ? 0 : Math.Round((double)x.DegradedCalls * 100 / x.TotalCalls, 2),
                x.TotalCalls == 0 ? 0 : Math.Round(x.AvgElapsedMs, 2)))
            .ToListAsync();

        return Ok(result);
    }
}
