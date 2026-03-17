using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Authorize]
[RequiredScope("access_as_user")]
[Route("api/transcribe")]
public sealed class TranscriptionWebSocketController(
    IRealtimeTranscriptionService transcriptionService,
    ILogger<TranscriptionWebSocketController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [HttpGet("stream")]
    public async Task StreamTranscription(
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "WebSocket Required",
                Status = 400,
                Detail = "This endpoint requires a WebSocket connection.",
            }, cancellationToken);
            return;
        }

        logger.LogInformation("WebSocket transcription request received (language={Language})", language ?? "(default)");

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        logger.LogInformation("WebSocket accepted, starting transcription session");

        TranscriptionSession session;
        try
        {
            session = await transcriptionService.StartSessionAsync(language, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start transcription session");

            if (webSocket.State == WebSocketState.Open)
            {
                var errorJson = JsonSerializer.Serialize(new { error = "Failed to start transcription session." }, JsonOptions);
                var errorBytes = Encoding.UTF8.GetBytes(errorJson);
                await webSocket.SendAsync(errorBytes, WebSocketMessageType.Text, true, CancellationToken.None);
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.InternalServerError, "Transcription session failed", CancellationToken.None);
            }

            return;
        }

        logger.LogInformation("Transcription session started successfully");
        await using var _ = session;

        // Writer task: read transcription events from the channel and send them to the WebSocket client.
        var writerTask = Task.Run(async () =>
        {
            var eventCount = 0;
            try
            {
                await foreach (var evt in session.Results.ReadAllAsync(cancellationToken))
                {
                    if (webSocket.State != WebSocketState.Open)
                    {
                        logger.LogWarning("WebSocket no longer open, stopping writer (sent {Count} events)", eventCount);
                        break;
                    }

                    eventCount++;
                    logger.LogDebug(
                        "Sending transcription event #{Count}: isFinal={IsFinal}, text=\"{Text}\"",
                        eventCount, evt.IsFinal, evt.Text.Length > 60 ? evt.Text[..60] + "…" : evt.Text);

                    var json = JsonSerializer.Serialize(new TranscriptionMessage
                    {
                        Text = evt.Text,
                        IsFinal = evt.IsFinal,
                    }, JsonOptions);

                    var bytes = Encoding.UTF8.GetBytes(json);
                    await webSocket.SendAsync(
                        bytes, WebSocketMessageType.Text, true, cancellationToken);
                }

                logger.LogInformation("Transcription writer finished — sent {Count} events total", eventCount);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Transcription writer cancelled (sent {Count} events)", eventCount);
            }
            catch (WebSocketException ex)
            {
                logger.LogWarning(ex, "WebSocket send error during transcription (after {Count} events)", eventCount);
            }
        }, cancellationToken);

        // Reader loop: read binary audio frames from the WebSocket client and feed them to the session.
        var buffer = new byte[8192];
        var frameCount = 0;
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("WebSocket client sent Close after {FrameCount} audio frames", frameCount);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                {
                    frameCount++;
                    if (frameCount == 1 || frameCount % 100 == 0)
                    {
                        logger.LogDebug(
                            "Received audio frame #{Count}, size={Size}B",
                            frameCount, result.Count);
                    }

                    await session.WriteAudioAsync(
                        new ReadOnlyMemory<byte>(buffer, 0, result.Count), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("WebSocket reader cancelled after {FrameCount} audio frames", frameCount);
        }
        catch (WebSocketException ex)
        {
            logger.LogWarning(ex, "WebSocket receive error during transcription (after {FrameCount} frames)", frameCount);
        }

        logger.LogInformation("Completing transcription session (received {FrameCount} audio frames)", frameCount);

        // Signal end-of-audio and wait for the writer to finish.
        await session.CompleteAsync();
        await writerTask;

        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure, "Transcription complete", CancellationToken.None);
        }
    }

    private sealed record TranscriptionMessage
    {
        public required string Text { get; init; }
        public required bool IsFinal { get; init; }
    }
}
