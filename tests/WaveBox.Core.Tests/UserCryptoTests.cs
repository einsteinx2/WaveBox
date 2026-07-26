using System;
using WaveBox.Core.Model;
using Xunit;

namespace WaveBox.Core.Tests {
    // Only User's static/pure members are tested here; everything else on User goes through
    // Injection and repositories, which are process-global and off limits for unit tests.
    // PBKDF2 at 2500 iterations is deliberately slow, so hash computations are kept to a minimum.
    public class UserCryptoTests {
        [Fact]
        public void ComputePasswordHash_IsDeterministicForSameSaltAndDiffersAcrossSalts() {
            string hash1 = User.ComputePasswordHash("password", "saltA");
            string hash2 = User.ComputePasswordHash("password", "saltA");
            string hash3 = User.ComputePasswordHash("password", "saltB");

            Assert.Equal(hash1, hash2);
            Assert.NotEqual(hash1, hash3);

            // 64-byte PBKDF2 output as base64: 88 chars
            Assert.Equal(88, hash1.Length);
            byte[] decoded = Convert.FromBase64String(hash1);
            Assert.Equal(64, decoded.Length);
        }

        [Fact]
        public void ComputePasswordHash_DiffersAcrossPasswords() {
            Assert.NotEqual(
                User.ComputePasswordHash("password1", "saltA"),
                User.ComputePasswordHash("password2", "saltA"));
        }

        [Fact]
        public void GeneratePasswordSalt_Is32RandomBytesBase64AndUnique() {
            string salt1 = User.GeneratePasswordSalt();
            string salt2 = User.GeneratePasswordSalt();

            Assert.Equal(32, Convert.FromBase64String(salt1).Length);
            Assert.Equal(32, Convert.FromBase64String(salt2).Length);
            Assert.NotEqual(salt1, salt2);
        }

        [Fact]
        public void Authenticate_AcceptsCorrectPasswordAndRejectsWrongOne() {
            string salt = "fixed-salt";
            User user = new User {
                PasswordSalt = salt,
                PasswordHash = User.ComputePasswordHash("correct horse", salt)
            };

            Assert.True(user.Authenticate("correct horse"));
            Assert.False(user.Authenticate("battery staple"));
        }

        [Theory]
        // Role ordering: Test(1) < Guest(2) < User(3) < Admin(4); permission is Role >= required
        [InlineData(Role.Test, Role.Test, true)]
        [InlineData(Role.Test, Role.Guest, false)]
        [InlineData(Role.Test, Role.User, false)]
        [InlineData(Role.Test, Role.Admin, false)]
        [InlineData(Role.Guest, Role.Test, true)]
        [InlineData(Role.Guest, Role.Guest, true)]
        [InlineData(Role.Guest, Role.User, false)]
        [InlineData(Role.Guest, Role.Admin, false)]
        [InlineData(Role.User, Role.Guest, true)]
        [InlineData(Role.User, Role.User, true)]
        [InlineData(Role.User, Role.Admin, false)]
        [InlineData(Role.Admin, Role.Test, true)]
        [InlineData(Role.Admin, Role.User, true)]
        [InlineData(Role.Admin, Role.Admin, true)]
        public void HasPermission_ComparesRoles(Role userRole, Role requiredRole, bool expected) {
            User user = new User { Role = userRole };
            Assert.Equal(expected, user.HasPermission(requiredRole));
        }

        [Fact]
        public void CompareUsersByName_IsCaseInsensitiveOrdinal() {
            User alice = new User { UserName = "alice" };
            User bob = new User { UserName = "bob" };
            User bobUpper = new User { UserName = "BOB" };

            Assert.True(User.CompareUsersByName(alice, bob) < 0);
            Assert.True(User.CompareUsersByName(bob, alice) > 0);
            Assert.Equal(0, User.CompareUsersByName(bob, bobUpper));
        }
    }
}
