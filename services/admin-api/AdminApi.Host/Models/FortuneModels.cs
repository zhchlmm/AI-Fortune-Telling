namespace AdminApi.Host.Models;

public record CreateFortuneSessionRequest(
    string UserId,
    string FortuneType,
    Dictionary<string, string> Parameters
);

public record FortuneSessionDto(
    Guid Id,
    string UserId,
    string FortuneType,
    string InputSummary,
    string ResultSummary,
    DateTime CreatedAt
);

public record ContentItemDto(
    Guid Id,
    string Title,
    string Summary,
    string Content,
    Guid? CategoryId,
    string CategoryName,
    bool IsPublished,
    DateTime PublishedAt
);

public record CreateContentRequest(
    string Title,
    string Summary,
    string Content,
    Guid CategoryId,
    bool IsPublished
);

public record UpdateContentRequest(
    string Title,
    string Summary,
    string Content,
    Guid CategoryId,
    bool IsPublished
);

public record ContentCategoryDto(
    Guid Id,
    string Name,
    int SortOrder,
    bool IsEnabled,
    DateTime UpdatedAt
);

public record CreateContentCategoryRequest(
    string Name,
    int SortOrder,
    bool IsEnabled
);

public record UpdateContentCategoryRequest(
    string Name,
    int SortOrder,
    bool IsEnabled
);

public record CreateOrderRequest(
    string UserId,
    string ProductCode,
    decimal Amount
);

public record OrderDto(
    Guid Id,
    string UserId,
    string ProductCode,
    decimal Amount,
    string Status,
    DateTime UpdatedAt
);

public record LoginRequest(
    string Username,
    string Password
);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    bool RequirePasswordChange
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record LoginAuditDto(
    Guid Id,
    string Username,
    string IpAddress,
    bool IsSuccess,
    string Reason,
    DateTime CreatedAt
);

public record AiAuditDto(
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

public record AiAuditSummaryDto(
    int WindowMinutes,
    int TotalCalls,
    int DegradedCalls,
    double DegradedRate,
    double AvgElapsedMs,
    DateTime WindowStartUtc,
    DateTime WindowEndUtc
);

public record AiAuditTypeDistributionDto(
    string FortuneType,
    int TotalCalls,
    int DegradedCalls,
    double DegradedRate,
    double AvgElapsedMs
);

public record PagedResult<T>(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<T> Items
);

public record UploadImageResponse(
    string Url,
    string FileName,
    long Size,
    string ContentType
);

public record FortuneTemplateDto(
    Guid Id,
    string Name,
    string FortuneType,
    string Prompt,
    bool IsEnabled,
    DateTime UpdatedAt
);

public record CreateFortuneTemplateRequest(
    string Name,
    string FortuneType,
    string Prompt,
    bool IsEnabled
);

public record UpdateFortuneTemplateRequest(
    string Name,
    string FortuneType,
    string Prompt,
    bool IsEnabled
);

public record MiniappLoginByCodeRequest(
    string Code
);

public record MiniappLoginByCodeResponse(
    string OpenId
);

public record MiniappUserProfileResponse(
    string OpenId,
    string? Nickname,
    string? Avatar,
    string? Email,
    string? PhoneNumber,
    DateTime UpdatedAt
);

public record UpdateMiniappUserProfileRequest(
    string OpenId,
    string? Nickname,
    string? Email,
    string? PhoneNumber,
    string? Avatar
);

public record UpdateMiniappUserPhoneRequest(
    string OpenId,
    string EncryptedData,
    string Iv
);
