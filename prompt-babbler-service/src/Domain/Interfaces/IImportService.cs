namespace PromptBabbler.Domain.Interfaces;

public interface IImportService
{
    Task<string> StartImportAsync(string userId, string sourceFilePath, bool overwriteExisting, CancellationToken cancellationToken = default);
}
