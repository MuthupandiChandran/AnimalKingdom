using AnimalKingdom.Utils.ConfigOptions;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace AnimalKingdom.Services
{
    public interface ICloudStorageService
    {
        Task<string> GetSignedUrlAsync(string fileNameToRead, int timeOutInMinutes = 30);
        Task<string> UploadFileAsync(IFormFile fileToUpload, string fileNameToSave);
        Task DeleteFileAsync(string fileNameToDelete);
    
     }



    public class CloudStorageService : ICloudStorageService
    {
        private readonly GCSConfigOptions _options;
        private readonly ILogger _logger;
        private readonly GoogleCredential _googleCredential;

        public CloudStorageService(IOptions<GCSConfigOptions> options, ILogger<CloudStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;

            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (environment == "Development")
                {
                    // Read the contents of the file and pass it to FromJson
                    var jsonCredentials = System.IO.File.ReadAllText(_options.GCPStorageAuthFile);
                    _googleCredential = GoogleCredential.FromJson(jsonCredentials);
                }
                else
                {
                    _googleCredential = GoogleCredential.FromFile(_options.GCPStorageAuthFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                throw;
            }
        }


        public async Task DeleteFileAsync(string fileNameToDelete)
          {
            try
            {
                using (var storageClient = StorageClient.Create(_googleCredential))
                { 
                   await storageClient.DeleteObjectAsync(_options.GoogleCloudStorageBucketName, fileNameToDelete);
                }
                _logger.LogInformation($"File{fileNameToDelete}deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Occured While Uploading File {fileNameToDelete}:{ex.Message}");
                 throw;
            }
         }

        public async Task<string> GetSignedUrlAsync(string fileNameToRead, int timeOutInMinutes)
        {
            try
            {
                var sac = _googleCredential.UnderlyingCredential as ServiceAccountCredential;
                var urlSigner = UrlSigner.FromCredential(sac);
                //provides limited permission and time to make request: time here is mentioned for 30 minutes.
                var signedUrl = await urlSigner.SignAsync(_options.GoogleCloudStorageBucketName, fileNameToRead,TimeSpan.FromMinutes(timeOutInMinutes));
                _logger.LogInformation($"Signed Url obtained for file {fileNameToRead}");
                return signedUrl.ToString();
            }
            catch (Exception ex) 
            {
                _logger.LogError($"Error Occured While Obtaining Signed Url for File {fileNameToRead}:{ex.Message}");
                throw;

            }
        }

       public async Task<string> UploadFileAsync(IFormFile fileToUpload, string fileNameToSave)
        {
            try
            {
                _logger.LogInformation($"Uploading: file {fileNameToSave} to storage {_options.GoogleCloudStorageBucketName}");
                //Create Storage Client from Google Credential
                using (var memorystream = new MemoryStream())
                { 
                   await fileToUpload.CopyToAsync(memorystream);
                   using (var storageClient = StorageClient.Create(_googleCredential))
                    {
                        //upload file stream
                        var uploadedFile = await storageClient.UploadObjectAsync(_options.GoogleCloudStorageBucketName, fileNameToSave,fileToUpload.ContentType,memorystream);
                        _logger.LogInformation($"Uploaded: file {fileNameToSave} to storage{_options.GoogleCloudStorageBucketName}");
                        return uploadedFile.MediaLink;
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error Occured While Uploading File {fileNameToSave}:{ex.Message}");
                throw;
            }
        }
    }
}
