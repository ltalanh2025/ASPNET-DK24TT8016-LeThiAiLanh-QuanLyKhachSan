using System;
using System.IO;

namespace QLKS.Infrastructure
{
    public sealed class ServerPathResolver
    {
        public string MapPath(string virtualPath)
        {
            var relativePath = (virtualPath ?? string.Empty)
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart('~', Path.DirectorySeparatorChar);
            return Path.Combine(AppConfig.ContentRootPath, relativePath);
        }
    }
}
