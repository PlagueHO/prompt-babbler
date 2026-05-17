using System.CommandLine;
using System.Text.Json;
using PromptBabbler.ApiClient;
using PromptBabbler.ApiClient.Models;

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

var apiUrlOption = new Option<string?>("--api-url")
{
    Description = "Prompt Babbler API base URL. Defaults to PROMPT_BABBLER_API_URL.",
};
var accessCodeOption = new Option<string?>("--access-code")
{
    Description = "Access code sent as X-Access-Code. Defaults to PROMPT_BABBLER_ACCESS_CODE.",
};

var importCommand = new Command("import", "Import data into Prompt Babbler.");

var importBabblesCommand = new Command("babbles", "Import babbles from a JSON file using POST /api/babbles.");
var babblesFileOption = new Option<string>("--file")
{
    Description = "Path to a babbles JSON file.",
    Required = true,
};
importBabblesCommand.Options.Add(babblesFileOption);
importBabblesCommand.Options.Add(apiUrlOption);
importBabblesCommand.Options.Add(accessCodeOption);
importBabblesCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var file = parseResult.GetValue(babblesFileOption)!;
    var apiUrl = parseResult.GetValue(apiUrlOption);
    var accessCode = parseResult.GetValue(accessCodeOption);
    return await ImportBabblesAsync(file, apiUrl, accessCode, cancellationToken);
});

var importPackageCommand = new Command("package", "Import a ZIP package using POST /api/imports.");
var importPackageFileOption = new Option<string>("--file")
{
    Description = "Path to an export ZIP file.",
    Required = true,
};
var importOverwriteOption = new Option<bool>("--overwrite")
{
    Description = "Overwrite existing records when importing.",
};
importPackageCommand.Options.Add(importPackageFileOption);
importPackageCommand.Options.Add(importOverwriteOption);
importPackageCommand.Options.Add(apiUrlOption);
importPackageCommand.Options.Add(accessCodeOption);
importPackageCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var file = parseResult.GetValue(importPackageFileOption)!;
    var overwrite = parseResult.GetValue(importOverwriteOption);
    var apiUrl = parseResult.GetValue(apiUrlOption);
    var accessCode = parseResult.GetValue(accessCodeOption);
    return await ImportPackageAsync(file, overwrite, apiUrl, accessCode, cancellationToken);
});

importCommand.Subcommands.Add(importBabblesCommand);
importCommand.Subcommands.Add(importPackageCommand);

var exportCommand = new Command("export", "Export data from Prompt Babbler.");
var exportPackageCommand = new Command("package", "Start and download an export package via /api/exports.");
var exportOutputOption = new Option<string>("--output")
{
    Description = "Destination ZIP file path.",
    Required = true,
};
var includeBabblesOption = new Option<bool?>("--include-babbles")
{
    Description = "Include babbles (default: true).",
};
var includeGeneratedPromptsOption = new Option<bool?>("--include-generated-prompts")
{
    Description = "Include generated prompts (default: true).",
};
var includeUserTemplatesOption = new Option<bool?>("--include-user-templates")
{
    Description = "Include user templates (default: true).",
};
var includeSemanticVectorsOption = new Option<bool?>("--include-semantic-vectors")
{
    Description = "Include semantic vectors in exported babbles (default: false).",
};

exportPackageCommand.Options.Add(exportOutputOption);
exportPackageCommand.Options.Add(includeBabblesOption);
exportPackageCommand.Options.Add(includeGeneratedPromptsOption);
exportPackageCommand.Options.Add(includeUserTemplatesOption);
exportPackageCommand.Options.Add(includeSemanticVectorsOption);
exportPackageCommand.Options.Add(apiUrlOption);
exportPackageCommand.Options.Add(accessCodeOption);
exportPackageCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var output = parseResult.GetValue(exportOutputOption)!;
    var includeBabbles = parseResult.GetValue(includeBabblesOption) ?? true;
    var includeGeneratedPrompts = parseResult.GetValue(includeGeneratedPromptsOption) ?? true;
    var includeUserTemplates = parseResult.GetValue(includeUserTemplatesOption) ?? true;
    var includeSemanticVectors = parseResult.GetValue(includeSemanticVectorsOption) ?? false;
    var apiUrl = parseResult.GetValue(apiUrlOption);
    var accessCode = parseResult.GetValue(accessCodeOption);

    var request = new ExportRequest
    {
        IncludeBabbles = includeBabbles,
        IncludeGeneratedPrompts = includeGeneratedPrompts,
        IncludeUserTemplates = includeUserTemplates,
        IncludeSemanticVectors = includeSemanticVectors,
    };

    return await ExportPackageAsync(output, request, apiUrl, accessCode, cancellationToken);
});

exportCommand.Subcommands.Add(exportPackageCommand);

var rootCommand = new RootCommand("Prompt Babbler tools CLI")
{
    importCommand,
    exportCommand,
};

if (args.Length == 0)
{
    var seedPath = Environment.GetEnvironmentVariable("PROMPT_BABBLER_SEED_DATA_PATH");
    if (!string.IsNullOrWhiteSpace(seedPath))
    {
        Environment.ExitCode = await ImportBabblesAsync(seedPath, null, null, cancellationTokenSource.Token);
    }
    else
    {
        Environment.ExitCode = await rootCommand.Parse(args).InvokeAsync();
    }
}
else
{
    Environment.ExitCode = await rootCommand.Parse(args).InvokeAsync();
}

static async Task<int> ImportBabblesAsync(string file, string? apiUrlOverride, string? accessCodeOverride, CancellationToken cancellationToken)
{
    if (!File.Exists(file))
    {
        Console.Error.WriteLine($"Babbles file not found: {file}");
        return 1;
    }

    var payload = await File.ReadAllTextAsync(file, cancellationToken);
    var babbles = JsonSerializer.Deserialize<List<BabbleImportItem>>(payload, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    });

    if (babbles is null || babbles.Count == 0)
    {
        Console.Error.WriteLine("Babbles file contained no items.");
        return 1;
    }

    var apiUrl = ResolveApiUrl(apiUrlOverride);
    var accessCode = ResolveAccessCode(accessCodeOverride);

    using var httpClient = CreateApiHttpClient(apiUrl, accessCode);
    var client = new PromptBabblerApiClient(httpClient);

    var successCount = 0;
    var failureCount = 0;

    foreach (var babble in babbles)
    {
        try
        {
            using var response = await client.UpsertBabbleAsync(babble, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                successCount++;
                Console.WriteLine($"Imported babble: {babble.Title}");
            }
            else
            {
                failureCount++;
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.Error.WriteLine($"Failed to import '{babble.Title}': {(int)response.StatusCode} {response.StatusCode}. {errorText}");
            }
        }
        catch (Exception ex)
        {
            failureCount++;
            Console.Error.WriteLine($"Failed to import '{babble.Title}': {ex.Message}");
        }
    }

    Console.WriteLine($"Import completed. Success={successCount}, Failed={failureCount}");
    return failureCount == 0 ? 0 : 1;
}

static async Task<int> ImportPackageAsync(string file, bool overwrite, string? apiUrlOverride, string? accessCodeOverride, CancellationToken cancellationToken)
{
    if (!File.Exists(file))
    {
        Console.Error.WriteLine($"Import package not found: {file}");
        return 1;
    }

    var apiUrl = ResolveApiUrl(apiUrlOverride);
    var accessCode = ResolveAccessCode(accessCodeOverride);

    try
    {
        using var httpClient = CreateApiHttpClient(apiUrl, accessCode);
        var client = new PromptBabblerApiClient(httpClient);
        var jobId = await client.StartImportAsync(file, overwrite, cancellationToken);
        Console.WriteLine($"Started import job: {jobId}");
        return await WaitForJobAsync(
            () => client.GetImportJobAsync(jobId, cancellationToken),
            jobId,
            "import",
            cancellationToken);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Import package failed: {ex.Message}");
        return 1;
    }
}

static async Task<int> ExportPackageAsync(string outputPath, ExportRequest request, string? apiUrlOverride, string? accessCodeOverride, CancellationToken cancellationToken)
{
    var apiUrl = ResolveApiUrl(apiUrlOverride);
    var accessCode = ResolveAccessCode(accessCodeOverride);

    try
    {
        using var httpClient = CreateApiHttpClient(apiUrl, accessCode);
        var client = new PromptBabblerApiClient(httpClient);
        var jobId = await client.StartExportAsync(request, cancellationToken);
        Console.WriteLine($"Started export job: {jobId}");

        var waitCode = await WaitForJobAsync(
            () => client.GetExportJobAsync(jobId, cancellationToken),
            jobId,
            "export",
            cancellationToken);

        if (waitCode != 0)
        {
            return waitCode;
        }

        await client.DownloadExportAsync(jobId, outputPath, cancellationToken);
        Console.WriteLine($"Export downloaded to: {outputPath}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Export package failed: {ex.Message}");
        return 1;
    }
}

static async Task<int> WaitForJobAsync(
    Func<Task<ImportExportJobResponse>> getJob,
    string jobId,
    string jobType,
    CancellationToken cancellationToken)
{
    string? lastStatus = null;

    while (!cancellationToken.IsCancellationRequested)
    {
        var job = await getJob();
        if (!string.Equals(lastStatus, job.Status, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{jobType} job {jobId}: {job.Status} ({job.ProgressPercentage}%) {job.CurrentStage}");
            lastStatus = job.Status;
        }

        if (string.Equals(job.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(job.Status, "Failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(job.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"{jobType} job {jobId} ended with status {job.Status}. {job.ErrorMessage}");
            return 1;
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    }

    Console.Error.WriteLine($"{jobType} job {jobId} cancelled by user.");
    return 1;
}

static string ResolveApiUrl(string? apiUrlOverride)
{
    var apiUrl = string.IsNullOrWhiteSpace(apiUrlOverride)
        ? Environment.GetEnvironmentVariable("PROMPT_BABBLER_API_URL")
        : apiUrlOverride;

    if (string.IsNullOrWhiteSpace(apiUrl))
    {
        throw new InvalidOperationException("API URL is required. Set --api-url or PROMPT_BABBLER_API_URL.");
    }

    return apiUrl;
}

static string? ResolveAccessCode(string? accessCodeOverride)
{
    return string.IsNullOrWhiteSpace(accessCodeOverride)
        ? Environment.GetEnvironmentVariable("PROMPT_BABBLER_ACCESS_CODE")
        : accessCodeOverride;
}

static HttpClient CreateApiHttpClient(string apiUrl, string? accessCode)
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/", UriKind.Absolute),
        Timeout = TimeSpan.FromMinutes(5),
    };

    if (!string.IsNullOrWhiteSpace(accessCode))
    {
        httpClient.DefaultRequestHeaders.Add("X-Access-Code", accessCode);
    }

    return httpClient;
}
