using AdminApi.Host.Data;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Host.Services;

public class AiAuditStore(AdminDbContext dbContext, ILogger<AiAuditStore> logger)
{
    public async Task AddAsync(AiAuditRecord record, CancellationToken cancellationToken)
    {
        try
        {
            dbContext.AiAudits.Add(new AiAuditEntity
            {
                Id = record.Id,
                FortuneType = record.FortuneType,
                Model = record.Model,
                Degraded = record.Degraded,
                Reason = record.Reason,
                PromptSource = record.PromptSource,
                QuestionLength = record.QuestionLength,
                ResultLength = record.ResultLength,
                ElapsedMs = record.ElapsedMs,
                CreatedAt = record.CreatedAt
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "写入AI审计记录失败");
        }
    }

    public IQueryable<AiAuditEntity> Query(
        string? fortuneType,
        bool? degraded,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var query = dbContext.AiAudits.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(fortuneType))
        {
            query = query.Where(x => x.FortuneType == fortuneType);
        }

        if (degraded.HasValue)
        {
            query = query.Where(x => x.Degraded == degraded.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= toUtc.Value);
        }

        return query.OrderByDescending(x => x.CreatedAt);
    }
}

public record AiAuditRecord(
    Guid Id,
    string FortuneType,
    string Model,
    bool Degraded,
    string Reason,
    string PromptSource,
    int QuestionLength,
    int ResultLength,
    long ElapsedMs,
    DateTime CreatedAt
);
