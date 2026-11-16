using System;
using System.IO;

namespace FlowerShop.Tests
{
    /// <summary>
    /// Tìm thư mục gốc repo (nơi có FlowerShop.sln) từ thư mục chạy test,
    /// để test có thể đọc file SQL dump / csproj thật.
    /// </summary>
    public static class TestPaths
    {
        private static readonly Lazy<string> RepoRootLazy = new Lazy<string>(FindRepoRoot);

        public static string RepoRoot
        {
            get { return RepoRootLazy.Value; }
        }

        public static string DatabaseScript
        {
            get
            {
                return Path.Combine(RepoRoot, "FlowerShop", "App_Data", "FlowerShop_Database_Dump.sql");
            }
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "FlowerShop.sln")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("Không tìm thấy FlowerShop.sln từ " + AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
