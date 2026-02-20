namespace EVWebApi.Helpers
{
    public class BuildCookieOptionHelper
    {
        private readonly IWebHostEnvironment _env;

        public BuildCookieOptionHelper(IWebHostEnvironment env)
        {
            _env = env;
        }

        public CookieOptions Build(DateTime expiry, HttpContext context)
        {
            var isHttps = context.Request.IsHttps;

            return new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps, // true in prod (HTTPS)
                SameSite = isHttps
                    ? SameSiteMode.None   // required for cross-domain in prod
                    : SameSiteMode.Lax,   // safe for HTTP dev
                Expires = expiry
            };
        }
    }
}
