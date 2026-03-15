using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Api.Controllers;

[ApiController]
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

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await using var session = await transcriptionService.StartSessionAsync(language, cancellationToken);

        // Writer task: read transcription events from the channel and send them to the WebSocket client.
        var writerTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var evt in session.Results.ReadAllAsync(cancellationToken))
                {
                    if (webSocket.State != WebSocketState.Open)
                    {
                        break;
                    }

                    var json = JsonSerializer.Serialize(new TranscriptionMessage
                    {
                        Text = evt.Text,
                        IsFinal = evt.IsFinal,
                    }, JsonOptions);

                    var bytes = Encoding.UTF8.GetBytes(json);
                    await webSocket.SendAsync(
                        bytes, WebSocketMessageType.Text, true, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected — expected.
            }
            catch (WebSocketException ex)
            {
                logger.LogWarning(ex, "WebSocket send error during transcription");
            }
        }, cancellationToken);

        // Reader loop: read binary audio frames from the WebSocket client and feed them to the session.
        var buffer = new byte[8192];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                {
                    await session.WriteAudioAsync(
                        new ReadOnlyMemory<byte>(buffer, 0, result.Count), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on client disconnect.
        }
        catch (WebSocketException ex)
        {
            logger.LogWarning(ex, "WebSocket receive error during transcription");
        }

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
