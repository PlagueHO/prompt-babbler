using System.Threading.Channels;
using Azure.Core;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;

namespace PromptBabbler.Infrastructure.Services;

public sealed class AzureSpeechTranscriptionService(
    string region,
    string aiServicesEndpoint,
    TokenCredential credential,
    ILogger<AzureSpeechTranscriptionService> logger) : IRealtimeTranscriptionService
{
    private static readonly string[] CognitiveServicesScope =
        ["https://cognitiveservices.azure.com/.default"];

    private static readonly HttpClient s_httpClient = new();

    // STS tokens are valid for 10 minutes; cache with a 1-minute safety margin.
    private static readonly TimeSpan StsTokenSafetyMargin = TimeSpan.FromMinutes(1);
    private readonly SemaphoreSlim _stsTokenLock = new(1, 1);
    private string? _cachedStsToken;
    private DateTimeOffset _stsTokenExpiry = DateTimeOffset.MinValue;

    public async Task<TranscriptionSession> StartSessionAsync(
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        // Create a fresh SpeechConfig per session with a new AAD token.
        var sessionConfig = await CreateSpeechConfigAsync(cancellationToken);

        sessionConfig.SpeechRecognitionLanguage = !string.IsNullOrEmpty(language)
            ? language
            : "en-US";

        // For continuous recognition of a live stream, increase the end-of-speech
        // silence timeout so the service doesn't prematurely end the turn when
        // the speaker pauses. Default is ~2-5 seconds which is too short for
        // live dictation. Set to 30 seconds of silence before ending a phrase.
        sessionConfig.SetProperty(
            PropertyId.Speech_SegmentationSilenceTimeoutMs, "3000");
        sessionConfig.SetProperty(
            PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "30000");

        var audioFormat = AudioStreamFormat.GetWaveFormatPCM(
            samplesPerSecond: 16000, bitsPerSample: 16, channels: 1);
        var pushStream = AudioInputStream.CreatePushStream(audioFormat);
        var audioConfig = AudioConfig.FromStreamInput(pushStream);
        var recognizer = new SpeechRecognizer(sessionConfig, audioConfig);

        var channel = Channel.CreateUnbounded<TranscriptionEvent>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        var sessionStopped = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        recognizer.Recognizing += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                logger.LogDebug("Speech recognizing (partial): \"{Text}\"", e.Result.Text.Length > 60 ? e.Result.Text[..60] + "…" : e.Result.Text);
                channel.Writer.TryWrite(new TranscriptionEvent
                {
                    Text = e.Result.Text,
                    IsFinal = false,
                    Offset = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                    Duration = e.Result.Duration,
                });
            }
        };

        recognizer.Recognized += (_, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech &&
                !string.IsNullOrEmpty(e.Result.Text))
            {
                logger.LogInformation("Speech recognized (final): \"{Text}\"", e.Result.Text.Length > 80 ? e.Result.Text[..80] + "…" : e.Result.Text);
                channel.Writer.TryWrite(new TranscriptionEvent
                {
                    Text = e.Result.Text,
                    IsFinal = true,
                    Offset = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                    Duration = e.Result.Duration,
                });
            }
            else if (e.Result.Reason == ResultReason.NoMatch)
            {
                logger.LogDebug("Speech recognition NoMatch — no speech detected in audio segment");
            }
        };

        recognizer.Canceled += (_, e) =>
        {
            if (e.Reason == CancellationReason.Error)
            {
                logger.LogError(
                    "Speech recognition canceled with error {ErrorCode}: {ErrorDetails}",
                    e.ErrorCode, e.ErrorDetails);
                channel.Writer.TryComplete(new InvalidOperationException(
                    $"Speech recognition error {e.ErrorCode}: {e.ErrorDetails}"));
            }
            else
            {
                logger.LogInformation("Speech recognition canceled (reason={Reason})", e.Reason);
                channel.Writer.TryComplete();
            }

            sessionStopped.TrySetResult(0);
        };

        recognizer.SessionStarted += (_, _) =>
        {
            logger.LogInformation("Speech SDK session started");
        };

        recognizer.SessionStopped += (_, _) =>
        {
            logger.LogInformation("Speech SDK session stopped");
            channel.Writer.TryComplete();
            sessionStopped.TrySetResult(0);
        };

        // Fire-and-forget continuous recognition start; we await in the session lifecycle.
        logger.LogInformation(
            "Starting continuous recognition (region={Region}, language={Language})",
            region, sessionConfig.SpeechRecognitionLanguage);
        await recognizer.StartContinuousRecognitionAsync();

        var session = new TranscriptionSession(
            results: channel.Reader,
            writeAudio: (pcmData, _) =>
            {
                var size = pcmData.Length;
                pushStream.Write(pcmData.ToArray());
                logger.LogTrace("PushStream.Write: {Size}B", size);
                return Task.CompletedTask;
            },
            complete: async () =>
            {
                pushStream.Close();
                await sessionStopped.Task;
                await recognizer.StopContinuousRecognitionAsync();
            },
            dispose: async () =>
            {
                pushStream.Close();

                try
                {
                    await recognizer.StopContinuousRecognitionAsync();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed — safe to ignore.
                }

                recognizer.Dispose();
                audioConfig.Dispose();

                channel.Writer.TryComplete();
            });

        return session;
    }

    private async Task<SpeechConfig> CreateSpeechConfigAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(region))
        {
            throw new InvalidOperationException(
                "Speech:Region configuration is required. Ensure the AppHost passes the Azure location " +
                "via the Speech__Region environment variable.");
        }

        if (string.IsNullOrEmpty(aiServicesEndpoint))
        {
            throw new InvalidOperationException(
                "AI Services endpoint is required for Speech Service authentication. " +
                "Ensure the AI Foundry connection string (ConnectionStrings:ai-foundry) is configured.");
        }

        // Get a fresh AAD token from DefaultAzureCredential for the Cognitive Services scope.
        var tokenRequest = new TokenRequestContext(CognitiveServicesScope);
        var accessToken = await credential.GetTokenAsync(tokenRequest, cancellationToken);

        logger.LogDebug(
            "Obtained AAD token for Speech Service in region {Region}, expires {Expiry}",
            region, accessToken.ExpiresOn);

        // Exchange the AAD token for a short-lived Cognitive Services token via the STS endpoint.
        // The Speech SDK's FromAuthorizationToken does not accept raw AAD bearer tokens.
        // STS tokens are valid for 10 minutes — reuse a cached token when possible.
        var speechToken = await GetOrRefreshStsTokenAsync(accessToken.Token, cancellationToken);

        var speechConfig = SpeechConfig.FromAuthorizationToken(speechToken, region);
        return speechConfig;
    }

    /// <summary>
    /// Returns a cached STS token if still valid, otherwise exchanges the AAD token for a new one.
    /// Thread-safe via <see cref="_stsTokenLock"/>.
    /// </summary>
    private async Task<string> GetOrRefreshStsTokenAsync(string aadToken, CancellationToken cancellationToken)
    {
        // Fast path: token is still valid (no lock needed for the read — worst case we refresh slightly early).
        if (_cachedStsToken is not null && DateTimeOffset.UtcNow < _stsTokenExpiry)
        {
            logger.LogDebug("Reusing cached STS token (expires {Expiry})", _stsTokenExpiry);
            return _cachedStsToken;
        }

        await _stsTokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring the lock.
            if (_cachedStsToken is not null && DateTimeOffset.UtcNow < _stsTokenExpiry)
            {
                return _cachedStsToken;
            }

            var token = await ExchangeForSpeechTokenAsync(aadToken, cancellationToken);

            // STS tokens are valid for 10 minutes; subtract a safety margin.
            _cachedStsToken = token;
            _stsTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(10) - StsTokenSafetyMargin;

            logger.LogDebug("Cached new STS token (expires {Expiry})", _stsTokenExpiry);
            return token;
        }
        finally
        {
            _stsTokenLock.Release();
        }
    }

    /// <summary>
    /// Exchanges an AAD bearer token for a short-lived (10 min) Cognitive Services token
    /// via the AI Services resource's STS endpoint.
    /// </summary>
    private async Task<string> ExchangeForSpeechTokenAsync(string aadToken, CancellationToken cancellationToken)
    {
        // Ensure we hit the cognitiveservices.azure.com domain for the STS endpoint,
        // even if the Aspire connection string uses the openai.azure.com domain.
        var stsBase = aiServicesEndpoint.Replace(
            ".openai.azure.com", ".cognitiveservices.azure.com", StringComparison.OrdinalIgnoreCase);
        var stsUri = $"{stsBase.TrimEnd('/')}/sts/v1.0/issueToken";

        using var request = new HttpRequestMessage(HttpMethod.Post, stsUri);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", aadToken);

        logger.LogDebug("Exchanging AAD token via STS endpoint {StsUri}", stsUri);

        var response = await s_httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to exchange AAD token for Speech token at {stsUri}. " +
                $"Status: {response.StatusCode}. Response: {body}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
