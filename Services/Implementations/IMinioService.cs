namespace VideoProcessingAPI.Services.Interfaces
{
    public interface IMinioService
    {
        Task UploadFileAsync(string bucketName, string objectName, string filePath);
        Task EnsureBucketExistsAsync(string bucketName);
        string GetPublicUrl(string bucketName, string objectName);
        Task<bool> ObjectExistsAsync(string bucketName, string objectName);
    }
}