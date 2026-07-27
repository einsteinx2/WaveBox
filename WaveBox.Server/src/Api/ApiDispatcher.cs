using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using WaveBox.ApiHandler;
using WaveBox.ApiHandler.Handlers;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Server;

namespace WaveBox.Api {
    // Terminal Kestrel middleware reproducing the legacy HttpProcessor.ApiProcess dispatch flow:
    // raw (undecoded) parameter parsing, POST-body-as-query-string, session cookie auth with a
    // sliding refresh, and HTTP 200 + {"error": ...} JSON for all failure modes.
    public class ApiDispatcher {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger(typeof(ApiDispatcher));

        private static readonly char[] cookieSplitChars = new char[] { ';', ',', '=' };

        public async Task ProcessAsync(HttpContext context) {
            string method = context.Request.Method.ToUpperInvariant();

            if (method != "GET" && method != "POST" && method != "PUT" && method != "DELETE") {
                // HTTP 405: Unsupported method (legacy behavior, including the Allow header value)
                context.Response.StatusCode = 405;
                context.Response.Headers["Allow"] = "GET, POST";
                return;
            }

            // The legacy server never URL-decoded anything, so hand the raw request target to UriWrapper
            string rawUrl = context.Request.GetEncodedPathAndQuery();

            // POST appends the raw request body to the query string, regardless of Content-Type (legacy behavior)
            if (method == "POST") {
                using (StreamReader reader = new StreamReader(context.Request.Body)) {
                    string body = await reader.ReadToEndAsync(context.RequestAborted);
                    rawUrl = rawUrl + "?" + body;
                }
            }

            // Handlers are synchronous and may block (e.g. tailing a running transcode), so run the
            // legacy dispatch on a worker thread rather than tying up the request loop synchronously.
            await Task.Run(() => Dispatch(context, rawUrl, method), context.RequestAborted);
        }

        private static void Dispatch(HttpContext context, string rawUrl, string method) {
            UriWrapper uri = new UriWrapper(rawUrl, method);
            HttpContextProcessor processor = new HttpContextProcessor(context);

            // The user who is accessing the API
            User apiUser = null;

            // No API request found?  Serve web UI
            if (!uri.IsApiCall) {
                IApiHandler webApi = Injection.Get<IApiHandlerFactory>().CreateApiHandler("web");
                webApi.Process(uri, processor, apiUser);
                return;
            }

            // Get client IP address
            string ip = context.Connection.RemoteIpAddress != null ? context.Connection.RemoteIpAddress.ToString() : "unknown";

            // Check for valid API action ("web" and "error" are technically valid, but can't be used in this way)
            if (uri.ApiAction == null || uri.ApiAction == "web" || uri.ApiAction == "error") {
                WriteError(uri, processor, apiUser, "Invalid API call");
                logger.IfInfo(String.Format("[{0}] API: {1}", ip, rawUrl));
                return;
            }

            // Check for session cookie authentication, unless this is a login request
            string sessionId = null;
            if (uri.ApiAction != "login") {
                sessionId = GetSessionCookie(processor);
                apiUser = Injection.Get<IApiAuthenticate>().AuthenticateSession(sessionId);
            }

            // If no cookie, try parameter authentication
            if (apiUser == null) {
                apiUser = Injection.Get<IApiAuthenticate>().AuthenticateUri(uri);

                // If user still null, failed authentication, so serve error
                if (apiUser == null) {
                    WriteError(uri, processor, apiUser, "Authentication failed");
                    logger.IfInfo(String.Format("[{0}] API: {1}", ip, rawUrl));
                    return;
                }
            }

            // apiUser.SessionId will be generated on new login, so that takes precedence for new session cookie
            apiUser.SessionId = apiUser.SessionId ?? sessionId;
            SetSessionCookie(processor, apiUser.SessionId);

            // Store user's current session object
            apiUser.CurrentSession = Injection.Get<ISessionRepository>().SessionForSessionId(apiUser.SessionId);

            // Retrieve the requested API handler by its action
            IApiHandler apiHandler = Injection.Get<IApiHandlerFactory>().CreateApiHandler(uri.ApiAction);

            // Check for valid API action
            if (apiHandler == null) {
                WriteError(uri, processor, apiUser, "Invalid API call");
                logger.IfInfo(String.Format("[{0}] API: {1}", ip, rawUrl));
                return;
            }

            // Log API call
            logger.IfInfo(String.Format("[{0}/{1}@{2}] API: {3} {4}", apiUser.UserName, apiUser.CurrentSession != null ? apiUser.CurrentSession.ClientName : null, ip, method, rawUrl));

            // Check if user has appropriate permissions for this action on this API handler
            if (!apiHandler.CheckPermission(apiUser, uri.Action)) {
                WriteError(uri, processor, apiUser, "Permission denied");
                return;
            }

            // Finally, process and return results
            apiHandler.Process(uri, processor, apiUser);
        }

        private static void WriteError(UriWrapper uri, HttpContextProcessor processor, User user, string message) {
            ErrorApiHandler.Process(uri, processor, user, message);
        }

        // If a cookie is found, grab it and use it for authentication (legacy naive parsing, bug-compatible)
        private static string GetSessionCookie(HttpContextProcessor processor) {
            if (processor.HttpHeaders.ContainsKey("Cookie")) {
                // Split each cookie into pairs
                string[] cookies = processor.HttpHeaders["Cookie"].ToString().Split(cookieSplitChars, StringSplitOptions.RemoveEmptyEntries);

                // Iterate all cookies
                for (int i = 0; i < cookies.Length - 1; i += 2) {
                    // Look for wavebox_session cookie
                    if (cookies[i].Trim() == "wavebox_session") {
                        return cookies[i + 1];
                    }
                }
            }

            return null;
        }

        // Set a new session cookie to be set when the HTTP response is sent
        private static void SetSessionCookie(HttpContextProcessor processor, string sessionId) {
            if (sessionId != null) {
                // Calculate session timeout time (DateTime.UtcNow UTC + SessionTimeout minutes)
                DateTime expire = DateTime.UtcNow.ToUniversalTime().AddMinutes(Injection.Get<IServerSettings>().SessionTimeout);

                // Add a delayed header so cookie will be reset on each API call (to prevent timeout)
                processor.DelayedHeaders["Set-Cookie"] = String.Format("wavebox_session={0}; Path=/api; Expires={1};", sessionId, expire.ToRFC1123());
            }
        }
    }
}
