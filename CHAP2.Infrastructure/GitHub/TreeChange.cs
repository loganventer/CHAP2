namespace CHAP2.Infrastructure.GitHub;

/// <summary>
/// One entry in a tree-update payload. <see cref="BlobSha"/> identifies
/// the new blob for upserts; null indicates a deletion.
/// </summary>
internal sealed record TreeChange(string Path, string? BlobSha)
{
    public bool IsDelete => BlobSha is null;
    public static TreeChange Upsert(string path, string blobSha) => new(path, blobSha);
    public static TreeChange Delete(string path) => new(path, null);
}
