using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class TranscriptionWebSocketControllerTests
{
    private readonly IRealtimeTranscriptionService _transcriptionService = Substitute.For<IRealtimeTranscriptionService>();
    private readonly ILogger<TranscriptionWebSocketController> _logger = Substitute.For<ILogger<TranscriptionWebSocketController>>();
    private readonly TranscriptionWebSocketController _controller;

    public TranscriptionWebSocketControllerTests()
    {
        _controller = new TranscriptionWebSocketController(_transcriptionService, _logger);
    }

    private static ClaimsPrincipal CreateTestUser() => new(new ClaimsIdentity(
    [
        new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "00000000-0000-0000-0000-000000000000"),
        new Claim("preferred_username", "test@contoso.com"),
    ], "TestAuth"));

    [TestMethod]
    public async Task StreamTranscription_NonWebSocket_Returns400()
    {
        // Arrange: simulate a normal HTTP request (not a WebSocket upgrade)
        var httpContext = new DefaultHttpContext();
        httpContext.User = CreateTestUser();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };

        // Act
        await _controller.StreamTranscription(null, CancellationToken.None);

        // Assert
        httpContext.Response.StatusCode.Should().Be(400);
    }

    [TestMethod]
    public async Task StreamTranscription_WebSocket_StartsSessionAndForwardsEvents()
    {
        // Arrange: create a channel that the mock session will emit events on
        var channel = Channel.CreateUnbounded<TranscriptionEvent>();
        var writtenAudio = new List<byte[]>();
        var completeCalled = false;
        var disposeCalled = false;

        var session = new TranscriptionSession(
            channel.Reader,
            (data, _) =>
            {
                writtenAudio.Add(data.ToArray());
                return Task.CompletedTask;
            },
            () =>
            {
                completeCalled = true;
                channel.Writer.TryComplete();
                return Task.CompletedTask;
            },
            () =>
            {
                disposeCalled = true;
                return ValueTask.CompletedTask;
            });

        _transcriptionService.StartSessionAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(session);

        // Create a fake WebSocket that sends one binary frame then a close frame
        var audioData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var fakeSocket = new FakeWebSocket(audioData);

        // Write a transcription event before the controller starts reading
        await channel.Writer.WriteAsync(new TranscriptionEvent
        {
            Text = "hello",
            IsFinal = true,
        });

        var webSocketManager = Substitute.For<WebSocketManager>();
        webSocketManager.IsWebSocketRequest.Returns(true);
        webSocketManager.AcceptWebSocketAsync().Returns(fakeSocket);

        var httpContext = Substitute.For<HttpContext>();
        httpContext.WebSockets.Returns(webSocketManager);
        httpContext.Response.Returns(Substitute.For<HttpResponse>());
        httpContext.RequestAborted.Returns(CancellationToken.None);
        httpContext.User.Returns(CreateTestUser());

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };

        // Act
        await _controller.StreamTranscription("en-US", CancellationToken.None);

        // Assert — audio was forwarded to the session
        writtenAudio.Should().ContainSingle();
        writtenAudio[0].Should().BeEquivalentTo(audioData);

        // Transcription was completed
        completeCalled.Should().BeTrue();
        disposeCalled.Should().BeTrue();

        // The session sent a text message to the WebSocket
        fakeSocket.SentMessages.Should().ContainSingle();
        var sentJson = Encoding.UTF8.GetString(fakeSocket.SentMessages[0]);
        var msg = JsonSerializer.Deserialize<JsonElement>(sentJson);
        msg.GetProperty("text").GetString().Should().Be("hello");
        msg.GetProperty("isFinal").GetBoolean().Should().BeTrue();
    }

    /// <summary>
    /// Minimal in-memory WebSocket for unit testing.
    /// Yields one binary frame (the audio data) on the first receive, then
    /// returns a close message on every subsequent receive. Thread-safe so
    /// pre-buffer and main reader tasks can call concurrently.
    /// Records text messages sent by the controller.
    /// </summary>
    private sealed class FakeWebSocket : WebSocket
    {
        private readonly byte[] _audioData;
        private int _receiveCall;
        private WebSocketState _state = WebSocketState.Open;

        public FakeWebSocket(byte[] audioData)
        {
            _audioData = audioData;
        }

        public List<byte[]> SentMessages { get; } = [];

        public override WebSocketCloseStatus? CloseStatus => _state == WebSocketState.Closed
            ? WebSocketCloseStatus.NormalClosure : null;

        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType,
            bool endOfMessage, CancellationToken cancellationToken)
        {
            if (messageType == WebSocketMessageType.Text)
            {
                SentMessages.Add(buffer.Array![buffer.Offset..(buffer.Offset + buffer.Count)]);
            }

            return Task.CompletedTask;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref _receiveCall);
            if (call == 1)
            {
                _audioData.CopyTo(buffer.Array!, buffer.Offset);
                return Task.FromResult(new WebSocketReceiveResult(
                    _audioData.Length, WebSocketMessageType.Binary, true));
            }

            // All subsequent calls: return close message — keep State as Open so the
            // writer task can still send pending events before the graceful close.
            return Task.FromResult(new WebSocketReceiveResult(
                0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "done"));
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription,
            CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
        }

        public override void Dispose()
        {
        }
    }
}
