using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AdminApi.Host.Services;

public class FortuneAiService(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAiCompatibleOptions> options,
    AiAuditStore auditStore,
    ILogger<FortuneAiService> logger)
{
    private readonly OpenAiCompatibleOptions _options = options.Value;

    public async Task<string> GenerateFortuneAsync(
        string fortuneType,
        string userQuestion,
        string? templatePrompt,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            await auditStore.AddAsync(new AiAuditRecord(
                Guid.NewGuid(),
                fortuneType,
                string.Empty,
                true,
                "NotConfigured",
                "None",
                userQuestion.Length,
                0,
                stopwatch.ElapsedMilliseconds,
                DateTime.UtcNow),
                cancellationToken);

            logger.LogInformation(
                "AI审计: fortuneType={FortuneType}, provider=OpenAICompatible, enabled={Enabled}, degraded={Degraded}, reason={Reason}, elapsedMs={ElapsedMs}",
                fortuneType,
                _options.Enabled,
                true,
                "NotConfigured",
                stopwatch.ElapsedMilliseconds);
            return BuildFallbackResult(fortuneType);
        }

        try
        {
            var resolvedModel = _options.Model;
            var resolvedTemperature = _options.Temperature;
            var resolvedMaxTokens = _options.MaxTokens;
            var resolvedSystemPrompt = _options.DefaultSystemPrompt;

            if (_options.FortuneTypeOverrides.TryGetValue(fortuneType, out var typeOverride))
            {
                if (!string.IsNullOrWhiteSpace(typeOverride.SystemPrompt))
                {
                    resolvedSystemPrompt = typeOverride.SystemPrompt;
                }

                if (!string.IsNullOrWhiteSpace(typeOverride.Model))
                {
                    resolvedModel = typeOverride.Model;
                }

                if (typeOverride.Temperature.HasValue)
                {
                    resolvedTemperature = typeOverride.Temperature.Value;
                }

                if (typeOverride.MaxTokens.HasValue)
                {
                    resolvedMaxTokens = typeOverride.MaxTokens.Value;
                }
            }

            var client = httpClientFactory.CreateClient(nameof(FortuneAiService));
            client.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds));

            var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var systemPrompt = string.IsNullOrWhiteSpace(templatePrompt)
                ? resolvedSystemPrompt
                : templatePrompt;

            var payload = new
            {
                model = resolvedModel,
                temperature = resolvedTemperature,
                max_tokens = resolvedMaxTokens,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = $"算命类型：{fortuneType}\n用户问题：{userQuestion}\n请用中文回答，100-300字，分段清晰。"
                    }
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("AI 接口调用失败，StatusCode={StatusCode}, Body={Body}", response.StatusCode, content);
                await auditStore.AddAsync(new AiAuditRecord(
                    Guid.NewGuid(),
                    fortuneType,
                    resolvedModel,
                    true,
                    "HttpError",
                    string.IsNullOrWhiteSpace(templatePrompt) ? "Config" : "DatabaseTemplate",
                    userQuestion.Length,
                    0,
                    stopwatch.ElapsedMilliseconds,
                    DateTime.UtcNow),
                    cancellationToken);

                logger.LogInformation(
                    "AI审计: fortuneType={FortuneType}, provider=OpenAICompatible, model={Model}, degraded={Degraded}, reason={Reason}, statusCode={StatusCode}, questionLength={QuestionLength}, elapsedMs={ElapsedMs}",
                    fortuneType,
                    resolvedModel,
                    true,
                    "HttpError",
                    (int)response.StatusCode,
                    userQuestion.Length,
                    stopwatch.ElapsedMilliseconds);
                return BuildFallbackResult(fortuneType);
            }

            using var doc = JsonDocument.Parse(content);
            var aiResult = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(aiResult))
            {
                await auditStore.AddAsync(new AiAuditRecord(
                    Guid.NewGuid(),
                    fortuneType,
                    resolvedModel,
                    true,
                    "EmptyContent",
                    string.IsNullOrWhiteSpace(templatePrompt) ? "Config" : "DatabaseTemplate",
                    userQuestion.Length,
                    0,
                    stopwatch.ElapsedMilliseconds,
                    DateTime.UtcNow),
                    cancellationToken);

                logger.LogInformation(
                    "AI审计: fortuneType={FortuneType}, provider=OpenAICompatible, model={Model}, degraded={Degraded}, reason={Reason}, questionLength={QuestionLength}, elapsedMs={ElapsedMs}",
                    fortuneType,
                    resolvedModel,
                    true,
                    "EmptyContent",
                    userQuestion.Length,
                    stopwatch.ElapsedMilliseconds);
                return BuildFallbackResult(fortuneType);
            }

            var resultText = aiResult.Trim();
            var promptSource = string.IsNullOrWhiteSpace(templatePrompt) ? "Config" : "DatabaseTemplate";
            await auditStore.AddAsync(new AiAuditRecord(
                Guid.NewGuid(),
                fortuneType,
                resolvedModel,
                false,
                "Success",
                promptSource,
                userQuestion.Length,
                resultText.Length,
                stopwatch.ElapsedMilliseconds,
                DateTime.UtcNow),
                cancellationToken);

            logger.LogInformation(
                "AI审计: fortuneType={FortuneType}, provider=OpenAICompatible, model={Model}, degraded={Degraded}, promptSource={PromptSource}, questionLength={QuestionLength}, resultLength={ResultLength}, elapsedMs={ElapsedMs}",
                fortuneType,
                resolvedModel,
                false,
                promptSource,
                userQuestion.Length,
                resultText.Length,
                stopwatch.ElapsedMilliseconds);

            return resultText;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI 算命生成失败，使用降级结果");
            await auditStore.AddAsync(new AiAuditRecord(
                Guid.NewGuid(),
                fortuneType,
                string.Empty,
                true,
                ex.GetType().Name,
                string.IsNullOrWhiteSpace(templatePrompt) ? "Config" : "DatabaseTemplate",
                userQuestion.Length,
                0,
                stopwatch.ElapsedMilliseconds,
                DateTime.UtcNow),
                cancellationToken);

            logger.LogInformation(
                "AI审计: fortuneType={FortuneType}, provider=OpenAICompatible, degraded={Degraded}, reason={Reason}, questionLength={QuestionLength}, elapsedMs={ElapsedMs}",
                fortuneType,
                true,
                ex.GetType().Name,
                userQuestion.Length,
                stopwatch.ElapsedMilliseconds);
            return BuildFallbackResult(fortuneType);
        }
    }

    private static string BuildFallbackResult(string fortuneType)
    {
        return $"{fortuneType} 解读已生成：近期适合稳中求进，先聚焦一件最重要的事，避免冲动决策；保持规律作息与复盘习惯，会更容易迎来积极变化。";
    }
}
