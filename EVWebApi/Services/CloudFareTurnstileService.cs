using EVWebApi.DTOs.Security;
using EVWebApi.Interfaces.Services;
using System.Text.Json;

namespace EVWebApi.Services
{
    public class CloudFareTurnstileService : ICloudFareTurnstileService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _secret;

        public CloudFareTurnstileService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _secret = config["Turnstile:SecretKey"]; 
        }

        public async Task<bool> ValidateAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;

            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _secret,
                ["response"] = token
            });

            var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
            if (!response.IsSuccessStatusCode) return false;

            using var stream = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<TurnstileResponseDTO>(stream);

            return result?.Success ?? false;
        }
    }
}
