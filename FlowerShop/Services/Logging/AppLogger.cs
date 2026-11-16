using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FlowerShop.Services.Logging
{
    /// <summary>
    /// Logger tối giản ghi file (không kéo thêm dependency). Đủ để truy vết lỗi gửi mail,
    /// lỗi cổng thanh toán - những chỗ mà im lặng thất bại là nguy hiểm nhất.
    /// </summary>
    public static class AppLogger
    {
        private static readonly object SyncRoot = new object();
        private static string _directory;

        public static void Configure(string directory)
        {
            _directory = directory;
        }

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Warn(string message)
        {
            Write("WARN", message, null);
        }

        public static void Error(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        public static string ResolveDirectory(string applicationRoot)
        {
            if (!string.IsNullOrWhiteSpace(_directory))
            {
                return _directory;
            }

            return Path.Combine(applicationRoot ?? AppDomain.CurrentDomain.BaseDirectory, "App_Data", "logs");
        }

        /// <summary>Đọc N dòng log gần nhất (dùng cho trang diagnostics).</summary>
        public static IList<string> ReadRecent(string applicationRoot, int maxLines)
        {
            string dir = ResolveDirectory(applicationRoot);
            var result = new List<string>();

            if (!Directory.Exists(dir))
            {
                return result;
            }

            var files = new DirectoryInfo(dir).GetFiles("app-*.log");
            Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));

            if (files.Length == 0)
            {
                return result;
            }

            string[] lines = File.ReadAllLines(files[0].FullName, Encoding.UTF8);
            int start = Math.Max(0, lines.Length - maxLines);

            for (int i = start; i < lines.Length; i++)
            {
                result.Add(lines[i]);
            }

            return result;
        }

        private static void Write(string level, string message, Exception exception)
        {
            try
            {
                string dir = ResolveDirectory(AppDomain.CurrentDomain.BaseDirectory);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string line = string.Format("{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}{3}",
                    DateTime.Now,
                    level,
                    message,
                    exception == null
                        ? string.Empty
                        : " | " + exception.GetType().Name + ": " + exception.Message);

                string path = Path.Combine(dir, string.Format("app-{0:yyyyMMdd}.log", DateTime.Now));

                lock (SyncRoot)
                {
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Logging không bao giờ được phép làm hỏng request.
            }
        }
    }
}
