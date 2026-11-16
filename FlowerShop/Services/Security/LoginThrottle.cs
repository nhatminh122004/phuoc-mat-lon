using System;
using System.Runtime.Caching;

namespace FlowerShop.Services.Security
{
    /// <summary>
    /// Chống dò mật khẩu (credential stuffing / brute force) theo cặp IP + tài khoản.
    /// Dùng MemoryCache nên hoạt động cả khi app chạy single-node (IIS Express/Laragon).
    /// Logic tách khỏi controller để unit test được bằng đồng hồ giả.
    /// </summary>
    public static class LoginThrottle
    {
        public const int MaxAttempts = 5;
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

        private static readonly MemoryCache Cache = MemoryCache.Default;
        private const string Prefix = "login-throttle:";

        public class State
        {
            public int Failures { get; set; }
            public DateTime FirstFailureUtc { get; set; }
            public DateTime BlockedUntilUtc { get; set; }
        }

        public static string BuildKey(string ipAddress, string identifier)
        {
            string ip = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress.Trim();
            string id = string.IsNullOrWhiteSpace(identifier) ? "anonymous" : identifier.Trim().ToLowerInvariant();
            return Prefix + ip + "|" + id;
        }

        public static State GetState(string key, DateTime nowUtc)
        {
            var state = Cache.Get(key) as State;
            if (state == null)
            {
                return new State { Failures = 0, FirstFailureUtc = nowUtc, BlockedUntilUtc = DateTime.MinValue };
            }

            if (state.BlockedUntilUtc != DateTime.MinValue && nowUtc >= state.BlockedUntilUtc)
            {
                // Hết thời gian khoá -> reset
                Cache.Remove(key);
                return new State { Failures = 0, FirstFailureUtc = nowUtc, BlockedUntilUtc = DateTime.MinValue };
            }

            if (state.Failures > 0 && nowUtc - state.FirstFailureUtc > Window)
            {
                Cache.Remove(key);
                return new State { Failures = 0, FirstFailureUtc = nowUtc, BlockedUntilUtc = DateTime.MinValue };
            }

            return state;
        }

        public static bool IsBlocked(string key, DateTime nowUtc)
        {
            return GetState(key, nowUtc).BlockedUntilUtc > nowUtc;
        }

        /// <summary>Ghi nhận 1 lần đăng nhập sai; trả về true nếu vừa bị khoá.</summary>
        public static bool RegisterFailure(string key, DateTime nowUtc)
        {
            var state = GetState(key, nowUtc);

            if (state.Failures == 0)
            {
                state.FirstFailureUtc = nowUtc;
            }

            state.Failures++;

            bool justBlocked = false;
            if (state.Failures >= MaxAttempts)
            {
                state.BlockedUntilUtc = nowUtc.Add(Window);
                justBlocked = true;
            }

            Cache.Set(key, state, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(Window.Add(Window)) });
            return justBlocked;
        }

        public static void Reset(string key)
        {
            Cache.Remove(key);
        }

        public static int GetRemainingAttempts(string key, DateTime nowUtc)
        {
            var state = GetState(key, nowUtc);
            int remaining = MaxAttempts - state.Failures;
            return remaining < 0 ? 0 : remaining;
        }
    }
}
