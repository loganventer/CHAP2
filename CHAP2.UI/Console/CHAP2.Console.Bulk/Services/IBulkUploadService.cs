namespace CHAP2.Console.Bulk.Services;

public interface IBulkUploadService
{
    Task<int> UploadFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<UploadResult> UploadFileAsync(string filePath, CancellationToken cancellationToken = default);
}
