namespace E_Learning.Helpers
{
    public static class FileIconHelper
    {
        public static string GetIcon(string fileName)
        {
            var ext = System.IO.Path.GetExtension(fileName)?.ToLowerInvariant();

            return ext switch
            {
                ".pdf" => "bi-file-earmark-pdf",
                ".doc" or ".docx" => "bi-file-earmark-word",
                ".zip" or ".rar" => "bi-file-earmark-zip",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "bi-file-earmark-image",
                _ => "bi-file-earmark"
            };
        }
    }
}