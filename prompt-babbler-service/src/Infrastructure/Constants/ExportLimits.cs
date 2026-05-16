namespace PromptBabbler.Infrastructure.Constants;

public static class ExportLimits
{
    public const int MaxBabbles = 10_000;
    public const int MaxGeneratedPrompts = 10_000;
    public const int MaxUserTemplates = 1_000;
    public const long MaxZipBytes = 200L * 1024L * 1024L;
}
