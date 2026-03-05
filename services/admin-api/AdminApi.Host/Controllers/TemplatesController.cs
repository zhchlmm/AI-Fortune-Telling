using AdminApi.Host.Models;
using AdminApi.Host.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Host.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/templates")]
public class TemplatesController(AdminDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FortuneTemplateDto>>> GetList()
    {
        var list = await dbContext.Templates
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new FortuneTemplateDto(
                x.Id,
                x.Name,
                x.FortuneType,
                x.Prompt,
                x.IsEnabled,
                x.UpdatedAt
            ))
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<FortuneTemplateDto>> Create([FromBody] CreateFortuneTemplateRequest request)
    {
        var item = new FortuneTemplateEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            FortuneType = request.FortuneType,
            Prompt = request.Prompt,
            IsEnabled = request.IsEnabled,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Templates.Add(item);
        await dbContext.SaveChangesAsync();

        return Ok(new FortuneTemplateDto(
            item.Id,
            item.Name,
            item.FortuneType,
            item.Prompt,
            item.IsEnabled,
            item.UpdatedAt
        ));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FortuneTemplateDto>> Update(Guid id, [FromBody] UpdateFortuneTemplateRequest request)
    {
        var old = await dbContext.Templates.FirstOrDefaultAsync(x => x.Id == id);
        if (old == null)
        {
            return NotFound();
        }

        old.Name = request.Name;
        old.FortuneType = request.FortuneType;
        old.Prompt = request.Prompt;
        old.IsEnabled = request.IsEnabled;
        old.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(new FortuneTemplateDto(
            old.Id,
            old.Name,
            old.FortuneType,
            old.Prompt,
            old.IsEnabled,
            old.UpdatedAt
        ));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await dbContext.Templates.FirstOrDefaultAsync(x => x.Id == id);
        if (deleted is null)
        {
            return NotFound();
        }

        dbContext.Templates.Remove(deleted);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
