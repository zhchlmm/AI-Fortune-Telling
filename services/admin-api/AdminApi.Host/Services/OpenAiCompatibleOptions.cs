namespace AdminApi.Host.Services;

public class OpenAiCompatibleOptions
{
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-4o-mini";

    public double Temperature { get; set; } = 0.7;

    public int MaxTokens { get; set; } = 1000;

    public int TimeoutSeconds { get; set; } = 60;

    public string DefaultSystemPrompt { get; set; } = "你是一位专业、理性、友好的命理顾问。请基于用户问题给出结构化建议，包含：整体解读、机会、风险、行动建议。避免绝对化结论。";

    public Dictionary<string, FortuneTypeAiOverride> FortuneTypeOverrides { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public class FortuneTypeAiOverride
{
    public string? SystemPrompt { get; set; }

    public string? Model { get; set; }

    public double? Temperature { get; set; }

    public int? MaxTokens { get; set; }
}
