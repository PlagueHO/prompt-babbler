using System.Text.Json;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class FileSettingsService : ISettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".prompt-babbler");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<LlmSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(SettingsFilePath))
        {
            return null;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var json = await File.ReadAllTextAsync(SettingsFilePath, cancellationToken);
            return JsonSerializer.Deserialize<LlmSettings>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveSettingsAsync(LlmSettings settings, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }
}
