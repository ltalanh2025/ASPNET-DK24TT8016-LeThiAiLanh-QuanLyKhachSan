using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace QLKS.Services
{
    public static class ImageUploadService
    {
        public const int MaximumBytes = 5 * 1024 * 1024;
        public const string DefaultImage = "room-default.svg";

        private static readonly IDictionary<string, string[]> AllowedTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png", new[] { "image/png" } },
            { ".webp", new[] { "image/webp" } }
        };

        public static string Save(IFormFile file, string physicalDirectory, ModelStateDictionary modelState, string fieldName)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > MaximumBytes)
            {
                modelState.AddModelError(fieldName, "Ảnh không được vượt quá 5 MB.");
                return null;
            }

            var extension = Path.GetExtension(file.FileName) ?? string.Empty;
            string[] mimeTypes;
            if (!AllowedTypes.TryGetValue(extension, out mimeTypes) || !mimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                modelState.AddModelError(fieldName, "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP.");
                return null;
            }
            if (!HasValidSignature(file, extension))
            {
                modelState.AddModelError(fieldName, "Nội dung tệp không đúng định dạng ảnh.");
                return null;
            }

            Directory.CreateDirectory(physicalDirectory);
            var safeName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
            using (var output = File.Create(Path.Combine(physicalDirectory, safeName)))
            {
                file.CopyTo(output);
            }
            return safeName;
        }

        private static bool HasValidSignature(IFormFile file, string extension)
        {
            using var stream = file.OpenReadStream();
            var originalPosition = stream.CanSeek ? stream.Position : 0;
            var header = new byte[12];
            var read = stream.Read(header, 0, header.Length);
            if (stream.CanSeek) stream.Position = originalPosition;

            if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                return read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                return read >= 8 && header[0] == 0x89 && header[1] == 0x4E && header[2] == 0x47 && header[3] == 0x0D;
            if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
                return read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
            return false;
        }
    }
}
