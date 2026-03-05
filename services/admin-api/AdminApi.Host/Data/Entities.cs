namespace AdminApi.Host.Data;

public class FortuneSessionEntity
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string FortuneType { get; set; } = string.Empty;

    public string InputSummary { get; set; } = string.Empty;

    public string ResultSummary { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class ContentItemEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }

    public ContentCategoryEntity? Category { get; set; }

    public bool IsPublished { get; set; }

    public DateTime PublishedAt { get; set; }
}

public class ContentCategoryEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<ContentItemEntity> Contents { get; set; } = [];
}

public class OrderEntity
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}

public class FortuneTemplateEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string FortuneType { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class AdminUserEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool RequirePasswordChange { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockoutEndTimeUtc { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class LoginAuditEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public bool IsSuccess { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class AiAuditEntity
{
    public Guid Id { get; set; }

    public string FortuneType { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public bool Degraded { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string PromptSource { get; set; } = string.Empty;

    public int QuestionLength { get; set; }

    public int ResultLength { get; set; }

    public long ElapsedMs { get; set; }

    public DateTime CreatedAt { get; set; }
}
