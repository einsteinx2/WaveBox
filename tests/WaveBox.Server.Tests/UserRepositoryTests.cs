using System;
using System.Linq;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using Xunit;

namespace WaveBox.Server.Tests {
    [Collection("Integration")]
    public class UserRepositoryTests : IDisposable {
        private readonly IntegrationHarness harness;
        private readonly IUserRepository users;

        public UserRepositoryTests() {
            harness = new IntegrationHarness();
            users = Injection.Get<IUserRepository>();
        }

        public void Dispose() {
            harness.Dispose();
        }

        [Fact]
        public void CreateUserRoundTripsThroughRepositoryAndAuthenticates() {
            User created = users.CreateUser("alice", "secret", Role.User, null);

            Assert.NotNull(created.UserId);
            Assert.Equal("alice", created.UserName);
            Assert.NotNull(created.PasswordHash);
            Assert.NotNull(created.PasswordSalt);
            Assert.NotNull(created.CreateTime);

            User fetched = users.UserForName("alice");
            Assert.Equal(created.UserId, fetched.UserId);
            Assert.Equal(Role.User, fetched.Role);
            Assert.True(fetched.Authenticate("secret"));
            Assert.False(fetched.Authenticate("wrong"));
        }

        [Fact]
        public void CreateDuplicateUserNameReturnsEmptyUser() {
            users.CreateUser("alice", "secret", Role.User, null);

            // Pins actual behavior: a duplicate returns a blank User (null UserId), not null
            User duplicate = users.CreateUser("alice", "other", Role.Admin, null);
            Assert.NotNull(duplicate);
            Assert.Null(duplicate.UserId);
        }

        [Fact]
        public void MissingUsersComeBackAsStubObjects() {
            // Pins actual behavior: lookups never return null, they return stubs
            User byName = users.UserForName("ghost");
            Assert.Null(byName.UserId);
            Assert.Equal("ghost", byName.UserName);

            User byId = users.UserForId(99999);
            Assert.Equal(99999, byId.UserId);
            Assert.Null(byId.UserName);
        }

        [Fact]
        public void CachesAreLoadedAtConstructionTime() {
            // The singleton repository built its cache from an empty User table.  Creating a user
            // through a second, independently constructed repository updates the database but
            // only the second repository's cache.
            UserRepository other = new UserRepository(Injection.Get<IDatabase>(), Injection.Get<IItemRepository>());
            User created = other.CreateUser("bob", "pw", Role.User, null);
            Assert.NotNull(created.UserId);

            // Pins the stale-cache behavior of the ctor-time cache
            Assert.Null(users.UserForName("bob").UserId);
            Assert.NotNull(other.UserForName("bob").UserId);

            // A third repository constructed after the write sees the database state
            UserRepository fresh = new UserRepository(Injection.Get<IDatabase>(), Injection.Get<IItemRepository>());
            Assert.Equal(created.UserId, fresh.UserForName("bob").UserId);
        }

        [Fact]
        public void UpdateRoleAndPasswordPersist() {
            User user = users.CreateUser("carol", "old", Role.User, null);

            Assert.True(user.UpdateRole(Role.Admin));
            Assert.Equal(Role.Admin, users.UserForName("carol").Role);

            Assert.True(user.UpdatePassword("new"));
            User fetched = users.UserForName("carol");
            Assert.True(fetched.Authenticate("new"));
            Assert.False(fetched.Authenticate("old"));
        }

        [Fact]
        public void UpdateApiKeyPersistsAndClears() {
            User user = users.CreateUser("dave", "pw", Role.User, null);

            Assert.True(user.UpdateApiKey("my-api-key"));
            Assert.Equal("my-api-key", users.UserForName("dave").ApiKey);

            Assert.True(user.UpdateApiKey(null));
            Assert.Null(users.UserForName("dave").ApiKey);
        }

        [Fact]
        public void DeleteRemovesUserAndSessions() {
            User user = users.CreateUser("eve", "pw", Role.User, null);
            Assert.True(user.CreateSession("pw", "client"));
            Assert.Equal(1, Injection.Get<ISessionRepository>().CountSessions());

            Assert.True(user.Delete());

            Assert.Null(users.UserForName("eve").UserId);
            Assert.Equal(0, Injection.Get<ISessionRepository>().CountSessions());
        }

        [Fact]
        public void ExpiredUsersReturnsOnlyPastDeleteTimes() {
            long now = DateTime.UtcNow.ToUnixTime();
            users.CreateUser("expired", "pw", Role.Test, now - 3600);
            users.CreateUser("current", "pw", Role.User, now + 3600);
            users.CreateUser("forever", "pw", Role.User, null);

            var expired = users.ExpiredUsers();

            Assert.Single(expired);
            Assert.Equal("expired", expired[0].UserName);
        }

        [Fact]
        public void CreateTestUserGeneratesRandomTemporaryAccount() {
            User test = users.CreateTestUser(60);

            Assert.NotNull(test.UserId);
            Assert.Equal(Role.Test, test.Role);
            Assert.NotNull(test.DeleteTime);
            Assert.True(test.DeleteTime > DateTime.UtcNow.ToUnixTime());
        }
    }
}
