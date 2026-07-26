using System;
using WaveBox.ApiHandler;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using Xunit;

namespace WaveBox.Server.Tests {
    [Collection("Integration")]
    public class ApiAuthenticateTests : IDisposable {
        private readonly IntegrationHarness harness;
        private readonly IApiAuthenticate auth;
        private readonly User ben;

        public ApiAuthenticateTests() {
            harness = new IntegrationHarness();
            auth = Injection.Get<IApiAuthenticate>();
            ben = Injection.Get<IUserRepository>().CreateUser("ben", "password123", Role.User, null);
        }

        public void Dispose() {
            harness.Dispose();
        }

        [Fact]
        public void LoginUriCreatesSessionAndReturnsUser() {
            User user = auth.AuthenticateUri(new UriWrapper("/api/login?u=ben&p=password123&c=testclient"));

            Assert.NotNull(user);
            Assert.Equal(ben.UserId, user.UserId);
            Assert.False(String.IsNullOrEmpty(user.SessionId));

            Session session = Injection.Get<ISessionRepository>().SessionForSessionId(user.SessionId);
            Assert.Equal(ben.UserId, session.UserId);
            Assert.Equal("testclient", session.ClientName);
        }

        [Fact]
        public void LoginWithWrongPasswordReturnsNull() {
            Assert.Null(auth.AuthenticateUri(new UriWrapper("/api/login?u=ben&p=wrong")));
            Assert.Equal(0, Injection.Get<ISessionRepository>().CountSessions());
        }

        [Fact]
        public void SessionParameterAuthenticatesNonLoginRequests() {
            Assert.True(ben.CreateSession("password123", "client"));

            User user = auth.AuthenticateUri(new UriWrapper("/api/songs?s=" + ben.SessionId));

            Assert.NotNull(user);
            Assert.Equal(ben.UserId, user.UserId);
        }

        [Fact]
        public void AuthenticateSessionReturnsUserAndBumpsUpdateTime() {
            Assert.True(ben.CreateSession("password123", "client"));
            Session before = Injection.Get<ISessionRepository>().SessionForSessionId(ben.SessionId);
            long? beforeUpdate = before.UpdateTime;

            User user = auth.AuthenticateSession(ben.SessionId);

            Assert.NotNull(user);
            Assert.Equal(ben.UserId, user.UserId);
            Session after = Injection.Get<ISessionRepository>().SessionForSessionId(ben.SessionId);
            Assert.True(after.UpdateTime >= beforeUpdate);
        }

        [Fact]
        public void UnknownSessionOrMissingParametersReturnNull() {
            Assert.Null(auth.AuthenticateSession("no-such-session"));
            Assert.Null(auth.AuthenticateUri(new UriWrapper("/api/songs?s=no-such-session")));
            Assert.Null(auth.AuthenticateUri(new UriWrapper("/api/songs")));
        }
    }
}
