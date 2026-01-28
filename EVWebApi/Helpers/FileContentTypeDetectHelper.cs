using Microsoft.AspNetCore.StaticFiles;

namespace EVWebApi.Helpers
{
    public class FileContentTypeDetectHelper
    {
        public static string GetContentType(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return contentType;
        }
    }
}
