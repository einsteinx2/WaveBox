using System;
using System.Text;
using Microsoft.AspNetCore.Http;
using WaveBox.Core;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Subsonic;
using Xunit;

namespace WaveBox.Server.Tests {
    [Collection("Integration")]
    public class SubsonicAuthTests : IDisposable {
        private readonly IntegrationHarness harness;
        private readonly SubsonicAuth auth;
        private readonly User ben;

        public SubsonicAuthTests() {
            harness = new IntegrationHarness();
            auth = Injection.Get<SubsonicAuth>();
            ben = Injection.Get<IUserRepository>().CreateUser("ben", "password123", Role.User, null);
        }

        public void Dispose() {
            harness.Dispose();
        }

        private static SubsonicRequest Request(string queryString) {
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString(queryString);
            return new SubsonicRequest(context, null);
        }

        [Fact]
        public void ValidUserAndPasswordAuthenticates() {
            SubsonicError error;
            User user = auth.Authenticate(Request("?u=ben&p=password123&c=DSub"), out error);

            Assert.Null(error);
            Assert.NotNull(user);
            Assert.Equal("ben", user.UserName);
            Assert.Equal(ben.UserId, user.UserId);
            Assert.Equal(Role.User, user.Role);
            Assert.Equal("DSub", user.CurrentSession.ClientName);
        }

        [Fact]
        public void WrongPasswordFailsWithCode40() {
            SubsonicError error;
            Assert.Null(auth.Authenticate(Request("?u=ben&p=wrong"), out error));
            Assert.Equal(SubsonicError.WrongCredentials, error.Code);
        }

        [Fact]
        public void UnknownUserFailsWithCode40() {
            SubsonicError error;
            Assert.Null(auth.Authenticate(Request("?u=ghost&p=whatever"), out error));
            Assert.Equal(SubsonicError.WrongCredentials, error.Code);
        }

        [Fact]
        public void HexEncodedPasswordAuthenticates() {
            string hex = Convert.ToHexString(Encoding.UTF8.GetBytes("password123"));

            SubsonicError error;
            User user = auth.Authenticate(Request("?u=ben&p=enc:" + hex), out error);

            Assert.Null(error);
            Assert.Equal("ben", user.UserName);
        }

        [Fact]
        public void MalformedHexPasswordFailsWithCode40() {
            SubsonicError error;
            Assert.Null(auth.Authenticate(Request("?u=ben&p=enc:zznothex"), out error));
            Assert.Equal(SubsonicError.WrongCredentials, error.Code);
        }

        [Fact]
        public void TokenAuthIsRejectedWithCode42() {
            SubsonicError error;
            Assert.Null(auth.Authenticate(Request("?u=ben&t=abc123&s=salt"), out error));
            Assert.Equal(SubsonicError.MechanismNotSupported, error.Code);
        }

        [Fact]
        public void MissingCredentialsFailWithCode10() {
            SubsonicError error;
            Assert.Null(auth.Authenticate(Request("?u=ben"), out error));
            Assert.Equal(SubsonicError.MissingParameter, error.Code);

            Assert.Null(auth.Authenticate(Request("?c=DSub"), out error));
            Assert.Equal(SubsonicError.MissingParameter, error.Code);
        }

        [Fact]
        public void ApiKeyAuthenticates() {
            Assert.True(ben.UpdateApiKey("my-secret-api-key"));

            SubsonicError error;
            User user = auth.Authenticate(Request("?apiKey=my-secret-api-key&c=player"), out error);

            Assert.Null(error);
            Assert.Equal("ben", user.UserName);
            Assert.Equal("player", user.CurrentSession.ClientName);
        }

        [Fact]
        public void InvalidApiKeyFailsWithCode44() {
            Assert.True(ben.UpdateApiKey("my-secret-api-key"));

            SubsonicError error;
            Assert.Null(auth.Authenticate(Request("?apiKey=wrong-key"), out error));
            Assert.Equal(SubsonicError.InvalidApiKey, error.Code);
        }

        [Fact]
        public void ApiKeyCombinedWithOtherMechanismsFailsWithCode43() {
            Assert.True(ben.UpdateApiKey("my-secret-api-key"));

            SubsonicError error;
            Assert.Null(auth.Authenticate(Request("?apiKey=my-secret-api-key&u=ben"), out error));
            Assert.Equal(SubsonicError.ConflictingMechanisms, error.Code);

            Assert.Null(auth.Authenticate(Request("?apiKey=my-secret-api-key&p=password123"), out error));
            Assert.Equal(SubsonicError.ConflictingMechanisms, error.Code);
        }

        [Fact]
        public void RepeatedAuthUsesCacheAndStillSucceeds() {
            SubsonicError error;
            Assert.NotNull(auth.Authenticate(Request("?u=ben&p=password123"), out error));
            // Second call hits the verified-credential cache
            Assert.NotNull(auth.Authenticate(Request("?u=ben&p=password123"), out error));
            // Cached entry must not let a wrong password through
            Assert.Null(auth.Authenticate(Request("?u=ben&p=nope"), out error));
            Assert.Equal(SubsonicError.WrongCredentials, error.Code);

            // Eviction forces a full PBKDF2 verification, which still succeeds
            auth.Evict("ben");
            Assert.NotNull(auth.Authenticate(Request("?u=ben&p=password123"), out error));
        }

        [Fact]
        public void AuthenticatedUserIsAPerRequestCopy() {
            SubsonicError error;
            User first = auth.Authenticate(Request("?u=ben&p=password123&c=one"), out error);
            User second = auth.Authenticate(Request("?u=ben&p=password123&c=two"), out error);

            Assert.NotSame(first, second);
            Assert.NotSame(Injection.Get<IUserRepository>().UserForName("ben"), first);
            Assert.Equal("one", first.CurrentSession.ClientName);
            Assert.Equal("two", second.CurrentSession.ClientName);
        }
    }
}
