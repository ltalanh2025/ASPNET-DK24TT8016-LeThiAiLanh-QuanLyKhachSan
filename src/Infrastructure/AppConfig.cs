using System;
using Microsoft.Extensions.Configuration;

namespace QLKS.Infrastructure
{
    public static class AppConfig
    {
        private static IConfiguration configuration;

        public static string ContentRootPath { get; private set; } = AppContext.BaseDirectory;

        public static void Initialize(IConfiguration appConfiguration, string contentRootPath)
        {
            configuration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            ContentRootPath = string.IsNullOrWhiteSpace(contentRootPath) ? AppContext.BaseDirectory : contentRootPath;
        }

        public static string Get(string key, string fallback = null)
        {
            var value = configuration?[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        public static string GetConnectionString(string name = "QLKS")
        {
            return configuration?.GetConnectionString(name)
                ?? configuration?.GetConnectionString("QLKS")
                ?? configuration?.GetConnectionString("QL_KhachSan")
                ?? configuration?.GetConnectionString("QLKSEntities")
                ?? configuration?.GetConnectionString("QL_KhachSanEntities");
        }

        public static string GetEntityConnectionString()
        {
            var value = GetConnectionString("QLKS");

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình ConnectionStrings:QLKS trong appsettings.json.");
            }

            return value;
        }
    }
}

