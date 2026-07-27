using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Service.Services.Http;
using WaveBox.Static;

namespace WaveBox.ApiHandler.Handlers {
    class ErrorApiHandler : IApiHandler {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        public string Name { get { return "error"; } }

        // API handler is read-only, so no permissions checks needed
        public bool CheckPermission(User user, string action) {
            return true;
        }

        /// <summary>
        /// Overload for IApiHandler interface
        /// </summary>
        public void Process(UriWrapper uri, IHttpProcessor processor, User user) {
            Process(uri, processor, user, "Invalid API call");
        }

        /// <summary>
        /// Process logs the error, creates a JSON response, and send it back to the user on bad API call
        /// </summary>
        public static void Process(UriWrapper uri, IHttpProcessor processor, User user, string error) {
            logger.Error(error);

            ErrorResponse response = new ErrorResponse(error);
            processor.WriteJson(response);
        }
    }
}
