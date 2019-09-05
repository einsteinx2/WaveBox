using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Collections.Generic;
using WaveBox.Core.Extensions;
using WaveBox.Core.Static;
using System.Linq;

namespace WaveBox.Core.Model.Repository {
    public class SessionRepository : ISessionRepository {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDatabase database;

        private IDictionary<string, Session> Sessions { get; set; }

        public SessionRepository(IDatabase database) {
            if (database == null) {
                throw new ArgumentNullException("database");
            }

            this.database = database;

            this.Sessions = new Dictionary<string, Session>();
            this.ReloadSessions();
        }

        private void ReloadSessions() {
            lock (this.Sessions) {
                this.Sessions.Clear();

                // Reload all sessions into cache
                var reload = this.database.GetList<Session>("SELECT RowId AS RowId,* FROM Session");
                foreach (Session s in reload) {
                    this.Sessions[s.SessionId] = s;
                }
            }
        }

        public Session SessionForRowId(int rowId) {
            return this.database.GetSingle<Session>("SELECT RowId AS RowId,* FROM Session WHERE RowId = ?", rowId);
        }

        public Session SessionForSessionId(string sessionId) {
            lock (this.Sessions) {
                if (this.Sessions.ContainsKey(sessionId)) {
                    return this.Sessions[sessionId];
                }

                return null;
            }
        }

        public Session CreateSession(int userId, string clientName) {
            var session = new Session();
            session.SessionId = Utility.RandomString(100).SHA1();
            session.UserId = userId;

            // Set a default client name if not provided
            if (clientName == null) {
                clientName = "wavebox";
            }

            session.ClientName = clientName;
            long unixTime = DateTime.UtcNow.ToUnixTime();
            session.CreateTime = unixTime;
            session.UpdateTime = unixTime;

            int affected = this.database.InsertObject<Session>(session);

            // Fetch the row ID just created
            session.RowId = this.database.GetScalar<int>("SELECT RowId AS RowId FROM Session WHERE SessionId = ?", session.SessionId);

            if (affected > 0) {
                lock (this.Sessions) {
                    this.Sessions[session.SessionId] = session;
                }

                return session;
            }

            return new Session();
        }

        public bool UpdateSession(Session session) {
            // Get current UNIX time
            long unixTime = DateTime.UtcNow.ToUnixTime();

            // Update session non-logged, because sessions aren't in backup anyway
            int affected = this.database.ExecuteQueryNonLogged("UPDATE Session SET UpdateTime = ? WHERE SessionId = ?", unixTime, session.SessionId);

            if (affected > 0) {
                return this.UpdateSessionCache(session);
            }

            return false;
        }


        public IList<Session> AllSessions() {
            lock (this.Sessions) {
                return new List<Session>(this.Sessions.Select(x => x.Value).ToList());
            }
        }

        public int CountSessions() {
            lock (this.Sessions) {
                return this.Sessions.Count;
            }
        }

        public bool UpdateSessionCache(Session session) {
            lock (this.Sessions) {
                this.Sessions[session.SessionId] = session;
            }

            return true;
        }

        // Remove a session by row ID, reload the cached sessions list
        public bool DeleteSessionForRowId(int rowId) {
            int affected = this.database.ExecuteQuery("DELETE FROM Session WHERE RowId = ?", rowId);

            if (affected > 0) {
                this.ReloadSessions();

                return true;
            }

            return false;
        }

        public bool DeleteSessionsForUserId(int userId) {
            int affected = this.database.ExecuteQuery("DELETE FROM Session WHERE UserId = ?", userId);

            if (affected > 0) {
                this.ReloadSessions();

                return true;
            }

            return false;
        }

        public int? UserIdForSessionid(string sessionId) {
            if (sessionId == null) {
                return null;
            }

            lock (this.Sessions) {
                if (this.Sessions.ContainsKey(sessionId)) {
                    return this.Sessions[sessionId].UserId;
                }

                return null;
            }
        }
    }
}
