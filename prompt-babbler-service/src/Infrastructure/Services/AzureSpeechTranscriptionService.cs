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

    public async Task<TranscriptionSession> StartSessionAsync(
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        // Create a fresh SpeechConfig per session with a new AAD token.
        var sessionConfig = await CreateSpeechConfigAsync(cancellationToken);

        sessionConfig.SpeechRecognitionLanguage = !string.IsNullOrEmpty(language)
            ? language
            : "en-US";

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
                channel.Writer.TryWrite(new TranscriptionEvent
                {
                    Text = e.Result.Text,
                    IsFinal = true,
                    Offset = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                    Duration = e.Result.Duration,
                });
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
                channel.Writer.TryComplete();
            }

            sessionStopped.TrySetResult(0);
        };

        recognizer.SessionStopped += (_, _) =>
        {
            channel.Writer.TryComplete();
            sessionStopped.TrySetResult(0);
        };

        // Fire-and-forget continuous recognition start; we await in the session lifecycle.
        _ = recognizer.StartContinuousRecognitionAsync();

        var session = new TranscriptionSession(
            results: channel.Reader,
            writeAudio: (pcmData, _) =>
            {
                pushStream.Write(pcmData.ToArray());
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
        var speechToken = await ExchangeForSpeechTokenAsync(accessToken.Token, cancellationToken);

        var speechConfig = SpeechConfig.FromAuthorizationToken(speechToken, region);
        return speechConfig;
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
