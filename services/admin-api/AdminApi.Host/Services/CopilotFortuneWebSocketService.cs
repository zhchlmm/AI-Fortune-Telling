using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AdminApi.Host.Data;
using GitHub.Copilot.SDK;

namespace AdminApi.Host.Services;

public class CopilotFortuneWebSocketService(
    IConfiguration configuration,
    AdminDbContext dbContext,
    AiAuditStore aiAuditStore,
    ILogger<CopilotFortuneWebSocketService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task HandleAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        await SendAsync(socket, new { type = "connected", message = "copilot websocket connected" }, cancellationToken);

        var request = await ReceiveStartAsync(socket, cancellationToken);
        if (request is null)
        {
            await SendAsync(socket, new { type = "error", message = "invalid_request" }, cancellationToken);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var model = configuration["CopilotSdk:Model"] ?? "gpt-5-mini";
        var timeoutSeconds = int.TryParse(configuration["CopilotSdk:RequestTimeoutSeconds"], out var parsedTimeout)
            ? Math.Clamp(parsedTimeout, 10, 600)
            : 120;

        try
        {
            var token = configuration["CopilotSdk:GitHubToken"] ?? string.Empty;
            var cliUrl = configuration["CopilotSdk:CliUrl"];
            var enabled = bool.TryParse(configuration["CopilotSdk:Enabled"], out var parsedEnabled) && parsedEnabled;
            if (!enabled)
            {
                throw new InvalidOperationException("CopilotSdkDisabled");
            }

            if (string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(cliUrl))
            {
                throw new InvalidOperationException("CopilotSdkMissingGitHubTokenOrCliUrl");
            }

            using var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            runCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var runToken = runCts.Token;

            var clientOptions = new CopilotClientOptions();

            if (!string.IsNullOrWhiteSpace(cliUrl))
            {
                clientOptions.CliUrl = cliUrl;
            }
            else if (!string.IsNullOrWhiteSpace(token))
            {
                clientOptions.GitHubToken = token;
                clientOptions.UseLoggedInUser = false;
            }

            await using var client = new CopilotClient(clientOptions);
            await client.StartAsync(runToken);

            await using var session = await client.CreateSessionAsync(new SessionConfig
            {
                Model = model,
                Streaming = true,
                OnPermissionRequest = PermissionHandler.ApproveAll
            }, runToken);

            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var resultBuilder = new StringBuilder();
            string? finalMessage = null;

            using var subscription = session.On(evt =>
            {
                switch (evt)
                {
                    case AssistantMessageDeltaEvent delta when !string.IsNullOrWhiteSpace(delta.Data.DeltaContent):
                        resultBuilder.Append(delta.Data.DeltaContent);
                        _ = SendAsync(socket, new { type = "delta", text = delta.Data.DeltaContent }, CancellationToken.None);
                        break;
                    case AssistantMessageEvent message:
                        finalMessage = message.Data.Content;
                        break;
                    case SessionErrorEvent errorEvent:
                        done.TrySetException(new InvalidOperationException(errorEvent.Data.Message));
                        break;
                    case SessionIdleEvent:
                        done.TrySetResult();
                        break;
                }
            });

            var prompt = BuildPrompt(request.FortuneType, request.Question);
            await session.SendAsync(new MessageOptions
            {
                Prompt = prompt
            }, runToken);

            await done.Task.WaitAsync(TimeSpan.FromSeconds(timeoutSeconds), runToken);

            var result = string.IsNullOrWhiteSpace(finalMessage)
                ? resultBuilder.ToString().Trim()
                : finalMessage.Trim();

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("EmptyCopilotResponse");
            }

            var sessionEntity = new FortuneSessionEntity
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                FortuneType = request.FortuneType,
                InputSummary = request.Question,
                ResultSummary = result,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.FortuneSessions.Add(sessionEntity);
            await dbContext.SaveChangesAsync(runToken);

            await aiAuditStore.AddAsync(new AiAuditRecord(
                Guid.NewGuid(),
                request.FortuneType,
                model,
                false,
                "Success",
                "CopilotSdk",
                request.Question.Length,
                result.Length,
                stopwatch.ElapsedMilliseconds,
                DateTime.UtcNow), CancellationToken.None);

            await SendAsync(socket, new
            {
                type = "done",
                id = sessionEntity.Id,
                fortuneType = sessionEntity.FortuneType,
                result = sessionEntity.ResultSummary,
                model
            }, CancellationToken.None);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Copilot WebSocket 请求被客户端取消");

            await aiAuditStore.AddAsync(new AiAuditRecord(
                Guid.NewGuid(),
                request.FortuneType,
                model,
                true,
                "ClientCanceled",
                "CopilotSdk",
                request.Question.Length,
                0,
                stopwatch.ElapsedMilliseconds,
                DateTime.UtcNow), CancellationToken.None);

            await SendAsync(socket, new
            {
                type = "error",
                message = "RequestCanceled"
            }, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Copilot WebSocket 请求超时，timeoutSeconds={TimeoutSeconds}", timeoutSeconds);

            await aiAuditStore.AddAsync(new AiAuditRecord(
                Guid.NewGuid(),
                request.FortuneType,
                model,
                true,
                "Timeout",
                "CopilotSdk",
                request.Question.Length,
                0,
                stopwatch.ElapsedMilliseconds,
                DateTime.UtcNow), CancellationToken.None);

            await SendAsync(socket, new
            {
                type = "error",
                message = "CopilotRequestTimeout"
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Copilot WebSocket 算命失败");

            var normalizedError = NormalizeError(ex);

            await aiAuditStore.AddAsync(new AiAuditRecord(
                Guid.NewGuid(),
                request.FortuneType,
                model,
                true,
                normalizedError,
                "CopilotSdk",
                request.Question.Length,
                0,
                stopwatch.ElapsedMilliseconds,
                DateTime.UtcNow), CancellationToken.None);

            await SendAsync(socket, new
            {
                type = "error",
                message = normalizedError
            }, CancellationToken.None);
        }
    }

    private static string NormalizeError(Exception ex)
    {
        if (ex.Message.Contains("Failed to list models", StringComparison.OrdinalIgnoreCase))
        {
            return "CopilotCliNotAuthenticated";
        }

        return ex.Message;
    }

    private static string BuildPrompt(string fortuneType, string question)
    {
        return $"算命类型：{fortuneType}\n用户问题：{question}\n请用中文回答，100-300字，分段清晰，包含：整体解读、机会、风险、行动建议。";
    }

    private static async Task<CopilotStartMessage?> ReceiveStartAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8 * 1024];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        var text = Encoding.UTF8.GetString(stream.ToArray());
        var message = JsonSerializer.Deserialize<CopilotStartMessage>(text, JsonOptions);
        if (message?.Type != "fortune.start")
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(message.UserId) ||
            string.IsNullOrWhiteSpace(message.FortuneType) ||
            string.IsNullOrWhiteSpace(message.Question))
        {
            return null;
        }

        return message;
    }

    private static async Task SendAsync(WebSocket socket, object payload, CancellationToken cancellationToken)
    {
        if (socket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var data = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken);
    }

    private sealed record CopilotStartMessage(string Type, string UserId, string FortuneType, string Question);
}