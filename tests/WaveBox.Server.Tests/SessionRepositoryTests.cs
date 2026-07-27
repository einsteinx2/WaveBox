using System;
using WaveBox.Core;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using Xunit;

namespace WaveBox.Server.Tests {
    [Collection("Integration")]
    public class SessionRepositoryTests : IDisposable {
        private readonly IntegrationHarness harness;
        private readonly ISessionRepository sessions;
        private readonly User user;

        public SessionRepositoryTests() {
            harness = new IntegrationHarness();
            sessions = Injection.Get<ISessionRepository>();
            user = Injection.Get<IUserRepository>().CreateUser("ben", "pw", Role.User, null);
        }

        public void Dispose() {
            harness.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void CreateSessionPopulatesAllFields() {
            Session session = sessions.CreateSession((int)user.UserId, "DSub");

            Assert.False(String.IsNullOrEmpty(session.SessionId));
            Assert.Equal(user.UserId, session.UserId);
            Assert.Equal("DSub", session.ClientName);
            Assert.NotNull(session.CreateTime);
            Assert.Equal(session.CreateTime, session.UpdateTime);
            Assert.True(session.RowId > 0);
        }

        [Fact]
        public void CreateSessionDefaultsClientNameToWavebox() {
            Session session = sessions.CreateSession((int)user.UserId, null);

            Assert.Equal("wavebox", session.ClientName);
        }

        [Fact]
        public void SessionLookupsRoundTrip() {
            Session session = sessions.CreateSession((int)user.UserId, "client");

            Assert.Equal(user.UserId, sessions.UserIdForSessionid(session.SessionId));
            Assert.Equal(session.SessionId, sessions.SessionForSessionId(session.SessionId).SessionId);
            Assert.Equal(session.SessionId, sessions.SessionForRowId(session.RowId).SessionId);

            Assert.Null(sessions.UserIdForSessionid("nope"));
            Assert.Null(sessions.UserIdForSessionid(null));
            Assert.Null(sessions.SessionForSessionId("nope"));
        }

        [Fact]
        public void UpdateSessionBumpsUpdateTime() {
            Session session = sessions.CreateSession((int)user.UserId, "client");
            long? originalUpdate = session.UpdateTime;

            // The model's Update() stamps a fresh UpdateTime and persists it
            Assert.True(session.Update());

            Assert.True(session.UpdateTime >= originalUpdate);
            Assert.Equal(session.UpdateTime, sessions.SessionForRowId(session.RowId).UpdateTime);
        }

        [Fact]
        public void DeleteSessionForRowIdRemovesFromCacheAndDb() {
            Session session = sessions.CreateSession((int)user.UserId, "client");
            Assert.Equal(1, sessions.CountSessions());

            Assert.True(sessions.DeleteSessionForRowId(session.RowId));

            Assert.Equal(0, sessions.CountSessions());
            Assert.Null(sessions.SessionForSessionId(session.SessionId));
        }

        [Fact]
        public void DeleteSessionsForUserIdRemovesAllOfThatUsersSessions() {
            sessions.CreateSession((int)user.UserId, "one");
            sessions.CreateSession((int)user.UserId, "two");
            User other = Injection.Get<IUserRepository>().CreateUser("other", "pw", Role.User, null);
            Session kept = sessions.CreateSession((int)other.UserId, "three");
            Assert.Equal(3, sessions.CountSessions());

            Assert.True(sessions.DeleteSessionsForUserId((int)user.UserId));

            Assert.Equal(1, sessions.CountSessions());
            Assert.Equal(kept.SessionId, sessions.AllSessions()[0].SessionId);
        }
    }
}
