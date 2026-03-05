using AdminApi.Host.Models;
using AdminApi.Host.Data;
using AdminApi.Host.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Host.Controllers;

[ApiController]
[Route("api/v1/fortune-sessions")]
public class FortuneSessionsController(AdminDbContext dbContext, FortuneAiService fortuneAiService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<FortuneSessionDto>> Create([FromBody] CreateFortuneSessionRequest request)
    {
        var question = request.Parameters.TryGetValue("question", out var questionValue)
            ? questionValue
            : string.Empty;

        var templatePrompt = await dbContext.Templates
            .Where(x => x.IsEnabled && x.FortuneType == request.FortuneType)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => x.Prompt)
            .FirstOrDefaultAsync();

        var resultSummary = await fortuneAiService.GenerateFortuneAsync(
            request.FortuneType,
            question,
            templatePrompt,
            HttpContext.RequestAborted);

        var createdEntity = new FortuneSessionEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FortuneType = request.FortuneType,
            InputSummary = string.Join(";", request.Parameters.Select(x => $"{x.Key}={x.Value}")),
            ResultSummary = resultSummary,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.FortuneSessions.Add(createdEntity);
        await dbContext.SaveChangesAsync();

        var created = new FortuneSessionDto(
            createdEntity.Id,
            createdEntity.UserId,
            createdEntity.FortuneType,
            createdEntity.InputSummary,
            createdEntity.ResultSummary,
            createdEntity.CreatedAt
        );

        return Ok(created);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FortuneSessionDto>> GetById(Guid id)
    {
        var session = await dbContext.FortuneSessions.FirstOrDefaultAsync(x => x.Id == id);
        if (session is null)
        {
            return NotFound();
        }

        return Ok(new FortuneSessionDto(
            session.Id,
            session.UserId,
            session.FortuneType,
            session.InputSummary,
            session.ResultSummary,
            session.CreatedAt
        ));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FortuneSessionDto>>> GetHistory(
        [FromQuery] string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var list = await dbContext.FortuneSessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
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

        return Ok(list);
    }
}
