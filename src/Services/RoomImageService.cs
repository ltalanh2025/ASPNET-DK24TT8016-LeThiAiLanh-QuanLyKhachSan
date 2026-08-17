using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;

namespace QLKS.Services
{
    public sealed class RoomImageService
    {
        public const string DefaultImagePath = "~/Content/Images/room-default.svg";
        private static readonly string[] AllowedLocalExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private readonly QLKSEntities db;

        public RoomImageService(QLKSEntities context)
        {
            db = context ?? throw new ArgumentNullException(nameof(context));
        }

        public string GetRoomPrimaryImage(int roomId)
        {
            var image = db.HinhAnhPhongs.AsNoTracking()
                .Where(x => x.MaPhong == roomId && x.TrangThai)
                .OrderByDescending(x => x.LaAnhDaiDien)
                .ThenBy(x => x.ThuTuHienThi)
                .ThenBy(x => x.MaHinhAnh)
                .Select(x => x.DuongDanAnh)
                .FirstOrDefault();
            if (IsValidImagePath(image)) return NormalizeImagePath(image);

            var legacy = db.Phongs.AsNoTracking().Where(x => x.MaPhong == roomId).Select(x => x.AnhDaiDien).FirstOrDefault();
            return NormalizeLegacyImage(legacy) ?? DefaultImagePath;
        }

        public IEnumerable<string> GetRoomImages(int roomId)
        {
            var images = db.HinhAnhPhongs.AsNoTracking()
                .Where(x => x.MaPhong == roomId && x.TrangThai)
                .OrderByDescending(x => x.LaAnhDaiDien)
                .ThenBy(x => x.ThuTuHienThi)
                .ThenBy(x => x.MaHinhAnh)
                .Select(x => x.DuongDanAnh)
                .ToList()
                .Where(IsValidImagePath)
                .Select(NormalizeImagePath)
                .ToList();
            return images.Count == 0 ? new List<string> { GetRoomPrimaryImage(roomId) } : images;
        }

        public IDictionary<int, RoomImageGalleryViewModel> GetGalleriesForRooms(IEnumerable<int> roomIds, IUrlHelper url)
        {
            var ids = (roomIds ?? Enumerable.Empty<int>()).Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, RoomImageGalleryViewModel>();

            var rooms = db.Phongs.AsNoTracking()
                .Where(x => ids.Contains(x.MaPhong))
                .Select(x => new { x.MaPhong, x.SoPhong, x.AnhDaiDien })
                .ToList()
                .ToDictionary(x => x.MaPhong);

            var imageRows = db.HinhAnhPhongs.AsNoTracking()
                .Where(x => ids.Contains(x.MaPhong) && x.TrangThai)
                .OrderBy(x => x.MaPhong)
                .ThenByDescending(x => x.LaAnhDaiDien)
                .ThenBy(x => x.ThuTuHienThi)
                .ThenBy(x => x.MaHinhAnh)
                .ToList()
                .Where(x => IsValidImagePath(x.DuongDanAnh))
                .GroupBy(x => x.MaPhong)
                .ToDictionary(x => x.Key, x => x.ToList());

            var result = new Dictionary<int, RoomImageGalleryViewModel>();
            foreach (var roomId in ids)
            {
                var room = rooms.ContainsKey(roomId) ? rooms[roomId] : null;
                var roomNumber = room == null ? roomId.ToString() : room.SoPhong;
                var alt = "Ảnh phòng " + (string.IsNullOrWhiteSpace(roomNumber) ? roomId.ToString() : roomNumber);
                var rows = imageRows.ContainsKey(roomId) ? imageRows[roomId] : new List<HinhAnhPhong>();
                var images = rows.Select(x => new RoomImageViewModel
                {
                    MaHinhAnh = x.MaHinhAnh,
                    MaPhong = x.MaPhong,
                    DuongDanAnh = NormalizeImagePath(x.DuongDanAnh),
                    ImageUrl = GetImageUrl(x.DuongDanAnh, url),
                    MoTa = x.MoTa,
                    LaAnhDaiDien = x.LaAnhDaiDien,
                    ThuTuHienThi = x.ThuTuHienThi,
                    TrangThai = x.TrangThai,
                    NgayTao = x.NgayTao,
                    AltText = string.IsNullOrWhiteSpace(x.MoTa) ? alt : x.MoTa
                }).ToList();

                if (images.Count == 0)
                {
                    var legacy = room == null ? null : NormalizeLegacyImage(room.AnhDaiDien);
                    var fallbackPath = legacy ?? DefaultImagePath;
                    images.Add(new RoomImageViewModel
                    {
                        MaPhong = roomId,
                        DuongDanAnh = fallbackPath,
                        ImageUrl = GetImageUrl(fallbackPath, url),
                        MoTa = legacy == null ? "Ảnh mặc định" : "Ảnh phòng tương thích dữ liệu cũ",
                        LaAnhDaiDien = true,
                        TrangThai = true,
                        AltText = alt
                    });
                }

                result[roomId] = new RoomImageGalleryViewModel
                {
                    MaPhong = roomId,
                    SoPhong = roomNumber,
                    PrimaryImageUrl = images[0].ImageUrl,
                    ImageUrls = images.Select(x => x.ImageUrl).ToList(),
                    ImageAltText = alt,
                    Images = images
                };
            }
            return result;
        }

        public static string GetImageUrl(string imagePath, IUrlHelper url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            var normalized = IsValidImagePath(imagePath) ? NormalizeImagePath(imagePath) : DefaultImagePath;
            if (IsValidHttpsImageUrl(normalized)) return normalized;
            return url.Content(normalized);
        }

        public static bool IsValidImagePath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || imagePath.Length > 1000) return false;
            var value = imagePath.Trim();
            if (ContainsUnsafeCharacters(value)) return false;
            if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return IsValidHttpsImageUrl(value);
            if (!value.StartsWith("~/Content/", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("/Content/", StringComparison.OrdinalIgnoreCase)) return false;
            if (value.Contains("..") || value.Contains("\\") || value.Contains(":") || value.Contains("?") || value.Contains("#")) return false;
            var extension = System.IO.Path.GetExtension(value);
            return AllowedLocalExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
                   string.Equals(value, DefaultImagePath, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidHttpsImageUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.Length > 1000 || ContainsUnsafeCharacters(imageUrl)) return false;
            Uri uri;
            if (!Uri.TryCreate(imageUrl.Trim(), UriKind.Absolute, out uri)) return false;
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return false;
            if (string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)) return false;
            if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) || uri.IsLoopback) return false;
            IPAddress address;
            return !IPAddress.TryParse(uri.Host, out address) || !IsPrivateAddress(address);
        }

        public static string NormalizeImagePath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return DefaultImagePath;
            var value = imagePath.Trim();
            if (value.StartsWith("/Content/", StringComparison.OrdinalIgnoreCase)) return "~" + value;
            return value;
        }

        public static string NormalizeLegacyImage(string legacyImage)
        {
            if (string.IsNullOrWhiteSpace(legacyImage) || string.Equals(legacyImage.Trim(), ImageUploadService.DefaultImage, StringComparison.OrdinalIgnoreCase)) return null;
            var value = legacyImage.Trim();
            if (IsValidImagePath(value)) return NormalizeImagePath(value);
            if (value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0 || value.Contains("..")) return null;
            var candidate = "~/Content/Images/" + value;
            return IsValidImagePath(candidate) ? candidate : null;
        }

        private static bool ContainsUnsafeCharacters(string value)
        {
            return value.IndexOfAny(new[] { '<', '>', '"', '\'', '\r', '\n', '\0' }) >= 0;
        }

        private static bool IsPrivateAddress(IPAddress address)
        {
            if (IPAddress.IsLoopback(address)) return true;
            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.Equals(IPAddress.IPv6Loopback);
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   bytes[0] == 127;
        }
    }
}
