using System;
using Newtonsoft.Json;
using Ninject;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers {
    public class LogoutApiHandler : IApiHandler {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get { return "logout"; } }

        // API handler is read-only, so no permissions checks needed
        public bool CheckPermission(User user, string action) {
            return true;
        }

        /// <summary>
        /// Process logs this user out and destroys their current session
        /// <summary>
        public void Process(UriWrapper uri, IHttpProcessor processor, User user) {
            // Destroy session
            if (user.DeleteSession(user.SessionId)) {
                logger.IfInfo(String.Format("Logged out user, destroyed session: [user: {0}, key: {1}]", user.UserName, user.SessionId));
                processor.WriteJson(new LogoutResponse(null, user.SessionId));
                return;
            }

            processor.WriteJson(new LogoutResponse("Failed to destroy user session", user.SessionId));
            return;
        }
    }
}
