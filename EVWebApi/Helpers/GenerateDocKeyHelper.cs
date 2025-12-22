namespace EVWebApi.Helpers
{
    public class GenerateDocKeyHelper
    {
        public static string GenerateDocKey(string label)
        {
            return label
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_");
        }
    }
}