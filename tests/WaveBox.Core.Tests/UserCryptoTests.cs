using System;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using Xunit;

namespace WaveBox.Core.Tests {
    // Only User's static/pure members are tested here; everything else on User goes through
    // Injection and repositories, which are process-global and off limits for unit tests.
    // bcrypt is deliberately slow even at the reduced test work factor, so hash computations
    // are kept to a minimum.
    public class UserCryptoTests {
        [Fact]
        public void Hash_IsSelfSaltingSoRepeatsDiffer() {
            // bcrypt generates its own salt per call, so unlike the old PBKDF2 scheme the same
            // password never produces the same hash twice
            string hash1 = PasswordHasher.Hash("password");
            string hash2 = PasswordHasher.Hash("password");

            Assert.NotEqual(hash1, hash2);
            Assert.True(PasswordHasher.Verify("password", hash1));
            Assert.True(PasswordHasher.Verify("password", hash2));
        }

        [Fact]
        public void Hash_IsAModularCryptFormatBcryptString() {
            string hash = PasswordHasher.Hash("password");

            // "$2<rev>$<cost>$<22 char salt><31 char hash>"
            Assert.StartsWith("$2", hash);
            Assert.Equal(60, hash.Length);
        }

        [Fact]
        public void Verify_RejectsWrongPassword() {
            string hash = PasswordHasher.Hash("password1");

            Assert.False(PasswordHasher.Verify("password2", hash));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not a bcrypt hash")]
        [InlineData("$2a$truncated")]
        public void Verify_ReturnsFalseForUnusableHashesInsteadOfThrowing(string storedHash) {
            Assert.False(PasswordHasher.Verify("password", storedHash));
        }

        [Fact]
        public void Verify_ReturnsFalseForEmptyPassword() {
            string hash = PasswordHasher.Hash("password");

            Assert.False(PasswordHasher.Verify(null, hash));
            Assert.False(PasswordHasher.Verify("", hash));
        }

        [Fact]
        public void Hash_RejectsPasswordsBeyondBcryptsInputLimit() {
            // bcrypt reads only the first 72 bytes and ignores the rest, so an over-long password
            // must be refused rather than silently truncated to something weaker than it looks
            Assert.Null(PasswordHasher.Hash(new string('a', PasswordHasher.MaxPasswordBytes + 1)));
            Assert.NotNull(PasswordHasher.Hash(new string('a', PasswordHasher.MaxPasswordBytes)));

            // The limit is bytes, not characters: this is 73 bytes of UTF-8 in 25 characters
            Assert.Null(PasswordHasher.Hash(new string('é', 36) + "a"));
        }

        [Fact]
        public void Hash_RejectsNullOrEmptyPassword() {
            Assert.Null(PasswordHasher.Hash(null));
            Assert.Null(PasswordHasher.Hash(""));
        }

        [Fact]
        public void Authenticate_AcceptsCorrectPasswordAndRejectsWrongOne() {
            User user = new User {
                PasswordHash = PasswordHasher.Hash("correct horse")
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
