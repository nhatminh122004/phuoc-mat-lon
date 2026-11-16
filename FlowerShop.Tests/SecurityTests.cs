using System;
using FlowerShop.Services.Security;
using Xunit;

namespace FlowerShop.Tests
{
    public class PasswordServiceTests
    {
        [Fact]
        public void Hash_ProducesBCryptAndVerifies()
        {
            string hash = PasswordService.Hash("Admin@123");

            Assert.True(PasswordService.IsBCryptHash(hash));
            Assert.NotEqual("Admin@123", hash);
            Assert.True(PasswordService.Verify("Admin@123", hash));
            Assert.False(PasswordService.Verify("Admin@124", hash));
        }

        [Fact]
        public void Verify_AcceptsLegacySha256Hex()
        {
            // Dữ liệu seed cũ trong file SQL lưu SHA-256 hex
            string sha = PasswordService.Sha256Hex("Admin@123");

            Assert.True(PasswordService.Verify("Admin@123", sha));
            Assert.False(PasswordService.Verify("sai-mat-khau", sha));
        }

        [Fact]
        public void Verify_RejectsEmptyAndNull()
        {
            Assert.False(PasswordService.Verify(null, "abc"));
            Assert.False(PasswordService.Verify("abc", null));
            Assert.False(PasswordService.Verify("", ""));
        }

        [Fact]
        public void NeedsRehash_TrueForLegacyFalseForCurrentBcrypt()
        {
            Assert.True(PasswordService.NeedsRehash(PasswordService.Sha256Hex("x")));
            Assert.True(PasswordService.NeedsRehash("plaintext"));
            Assert.True(PasswordService.NeedsRehash(null));

            string current = PasswordService.Hash("Customer@123");
            Assert.False(PasswordService.NeedsRehash(current));
        }

        [Fact]
        public void Hash_ThrowsOnEmptyPassword()
        {
            Assert.Throws<ArgumentException>(() => PasswordService.Hash(""));
        }

        [Theory]
        [InlineData("Abc12345", true)]      // 8 ký tự, có chữ + số
        [InlineData("abc123", false)]       // quá ngắn
        [InlineData("abcdefgh", false)]     // không có số
        [InlineData("12345678", false)]     // không có chữ
        [InlineData("", false)]
        public void Validate_EnforcesPolicy(string password, bool expected)
        {
            var result = PasswordService.Validate(password, null);

            Assert.Equal(expected, result.IsValid);
        }

        [Fact]
        public void Validate_DetectsConfirmMismatch()
        {
            var result = PasswordService.Validate("Abc12345", "Abc123456");

            Assert.False(result.IsValid);
            Assert.Contains("không trùng khớp", result.Message);
        }
    }

    public class PasswordResetTokenFactoryTests
    {
        [Fact]
        public void CreateToken_IsUrlSafeAndUnique()
        {
            var seen = new System.Collections.Generic.HashSet<string>();

            for (int i = 0; i < 200; i++)
            {
                string token = PasswordResetTokenFactory.CreateToken();

                Assert.True(PasswordResetTokenFactory.IsWellFormed(token), "Token sai định dạng: " + token);
                Assert.DoesNotContain("+", token);
                Assert.DoesNotContain("/", token);
                Assert.DoesNotContain("=", token);
                Assert.True(seen.Add(token), "Token bị trùng - nguồn ngẫu nhiên có vấn đề");
            }
        }

        [Fact]
        public void HashToken_IsStableSha256Hex()
        {
            string token = PasswordResetTokenFactory.CreateToken();

            string first = PasswordResetTokenFactory.HashToken(token);
            string second = PasswordResetTokenFactory.HashToken(token);

            Assert.Equal(first, second);
            Assert.Equal(64, first.Length);
            Assert.NotEqual(token, first);
        }

        [Fact]
        public void HashToken_DifferentTokensDifferentHashes()
        {
            Assert.NotEqual(
                PasswordResetTokenFactory.HashToken(PasswordResetTokenFactory.CreateToken()),
                PasswordResetTokenFactory.HashToken(PasswordResetTokenFactory.CreateToken()));
        }

        [Fact]
        public void GetExpiry_IsThirtyMinutes()
        {
            var issued = new DateTime(2026, 11, 16, 10, 0, 0);

            Assert.Equal(issued.AddMinutes(30), PasswordResetTokenFactory.GetExpiry(issued));
        }

        [Fact]
        public void IsExpired_ComparesWithNow()
        {
            var expiresAt = new DateTime(2026, 11, 16, 10, 30, 0);

            Assert.False(PasswordResetTokenFactory.IsExpired(expiresAt, expiresAt.AddSeconds(-1)));
            Assert.True(PasswordResetTokenFactory.IsExpired(expiresAt, expiresAt.AddSeconds(1)));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("abc")]
        [InlineData("^^^^")]
        public void IsWellFormed_RejectsGarbage(string token)
        {
            Assert.False(PasswordResetTokenFactory.IsWellFormed(token));
        }
    }

    public class LoginThrottleTests
    {
        private static string UniqueKey()
        {
            return LoginThrottle.BuildKey("127.0.0.1", "user-" + Guid.NewGuid().ToString("N"));
        }

        [Fact]
        public void BuildKey_NormalizesCaseAndNulls()
        {
            Assert.Equal(
                LoginThrottle.BuildKey("127.0.0.1", "ABC@Mail.com"),
                LoginThrottle.BuildKey("127.0.0.1", "abc@mail.com"));

            Assert.Equal(
                LoginThrottle.BuildKey(null, null),
                LoginThrottle.BuildKey("", ""));
        }

        [Fact]
        public void BlocksAfterFiveFailures()
        {
            string key = UniqueKey();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 4; i++)
            {
                Assert.False(LoginThrottle.RegisterFailure(key, now));
                Assert.False(LoginThrottle.IsBlocked(key, now));
            }

            Assert.True(LoginThrottle.RegisterFailure(key, now));
            Assert.True(LoginThrottle.IsBlocked(key, now));
        }

        [Fact]
        public void RemainingAttemptsDecreases()
        {
            string key = UniqueKey();
            var now = DateTime.UtcNow;

            Assert.Equal(5, LoginThrottle.GetRemainingAttempts(key, now));

            LoginThrottle.RegisterFailure(key, now);
            Assert.Equal(4, LoginThrottle.GetRemainingAttempts(key, now));

            LoginThrottle.RegisterFailure(key, now);
            Assert.Equal(3, LoginThrottle.GetRemainingAttempts(key, now));
        }

        [Fact]
        public void ResetClearsBlock()
        {
            string key = UniqueKey();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 5; i++)
            {
                LoginThrottle.RegisterFailure(key, now);
            }

            Assert.True(LoginThrottle.IsBlocked(key, now));

            LoginThrottle.Reset(key);
            Assert.False(LoginThrottle.IsBlocked(key, now));
            Assert.Equal(5, LoginThrottle.GetRemainingAttempts(key, now));
        }

        [Fact]
        public void BlockExpiresAfterWindow()
        {
            string key = UniqueKey();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 5; i++)
            {
                LoginThrottle.RegisterFailure(key, now);
            }

            Assert.True(LoginThrottle.IsBlocked(key, now));
            Assert.False(LoginThrottle.IsBlocked(key, now.Add(LoginThrottle.Window).AddSeconds(1)));
        }
    }
}
