using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace FlowerShop.Services.Upload
{
    public static class FileUploadService
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB per file

        public static string SaveImageFile(HttpPostedFileBase file, HttpServerUtilityBase server, string subFolder, out string errorMessage)
        {
            errorMessage = null;

            if (file == null || file.ContentLength == 0)
            {
                errorMessage = "Vui lòng chọn file ảnh hợp lệ.";
                return null;
            }

            if (file.ContentLength > MaxFileSizeBytes)
            {
                errorMessage = $"File '{file.FileName}' vượt quá dung lượng tối đa 10MB.";
                return null;
            }

            string ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            {
                errorMessage = $"Định dạng file '{file.FileName}' không hợp lệ. Chỉ chấp nhận JPG, PNG, WEBP, GIF.";
                return null;
            }

            try
            {
                string cleanFolder = (subFolder ?? "General").Trim('/');
                string relativeDir = $"/Uploads/{cleanFolder}";
                string physicalDir = server != null
                    ? server.MapPath("~" + relativeDir)
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads", cleanFolder);

                if (!Directory.Exists(physicalDir))
                {
                    Directory.CreateDirectory(physicalDir);
                }

                string safeFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                string physicalPath = Path.Combine(physicalDir, safeFileName);
                file.SaveAs(physicalPath);

                return $"{relativeDir}/{safeFileName}";
            }
            catch (Exception ex)
            {
                errorMessage = "Không thể lưu file ảnh: " + ex.Message;
                return null;
            }
        }

        public static List<string> SaveMultipleImageFiles(IEnumerable<HttpPostedFileBase> files, HttpServerUtilityBase server, string subFolder, out List<string> errors)
        {
            errors = new List<string>();
            var savedUrls = new List<string>();

            if (files == null)
            {
                return savedUrls;
            }

            foreach (var file in files)
            {
                if (file == null || file.ContentLength == 0)
                {
                    continue;
                }

                string url = SaveImageFile(file, server, subFolder, out string err);
                if (!string.IsNullOrEmpty(url))
                {
                    savedUrls.Add(url);
                }
                else if (!string.IsNullOrEmpty(err))
                {
                    errors.Add(err);
                }
            }

            return savedUrls;
        }

        public static List<string> SplitImageUrls(string rawUrls)
        {
            if (string.IsNullOrWhiteSpace(rawUrls))
            {
                return new List<string>();
            }

            return rawUrls
                .Split(new[] { ';', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(u => u.Trim())
                .Where(u => !string.IsNullOrEmpty(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string JoinImageUrls(IEnumerable<string> urls)
        {
            if (urls == null)
            {
                return null;
            }

            var clean = urls
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return clean.Any() ? string.Join(";", clean) : null;
        }
    }
}
