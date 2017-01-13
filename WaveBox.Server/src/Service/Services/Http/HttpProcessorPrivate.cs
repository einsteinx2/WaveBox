using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using Ninject;
using WaveBox;
using WaveBox.ApiHandler;
using WaveBox.ApiHandler.Handlers;
using WaveBox.Core;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Static;
using WaveBox.Server;
using WaveBox.Static;
using WaveBox.Transcoding;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske.

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/

namespace WaveBox.Service.Services.Http {
    // This class contains all private methods for the HttpProcessor class, to better abstract their functionality
    public partial class HttpProcessor : IHttpProcessor {
        // Parse tokens from a HTTP request
        private void ParseRequest() {
            try {
                string[] tokens = this.InputStream.ReadLine().Split(' ');

                // Expects: GET /url HTTP/1.1 or similar
                if (tokens.Length != 3) {
                    logger.Error("Failed reading HTTP request");
                    throw new Exception("Failed reading HTTP request");
                }

                // Store necessary information about this request
                this.HttpMethod = tokens[0].ToUpper();
                this.HttpUrl = tokens[1];
                this.HttpProtocolVersionString = tokens[2];
            }
            // If client disconnects, ignore and continue
            catch (IOException) {
            } catch (NullReferenceException) {
            }
        }

        // Read in all HTTP headers into hashtable
        private void ReadHeaders() {
            try {
                string line = null;
                while ((line = this.InputStream.ReadLine()) != null) {
                    // Done reading, empty content
                    if (line == "" || line.Length == 0) {
                        return;
                    }

                    // Check for valid HTTP header format
                    int separator = line.IndexOf(':');
                    if (separator == -1) {
                        logger.Error("Failed reading HTTP headers");
                        throw new Exception("Failed reading HTTP headers: " + line);
                    }

                    // Get header name
                    string name = line.Substring(0, separator);

                    // Find header position
                    int pos = separator + 1;

                    // Strip any spaces
                    while ((pos < line.Length) && (line[pos] == ' ')) {
                        pos++;
                    }

                    // Store header's value in name key
                    this.HttpHeaders[name] = line.Substring(pos, line.Length - pos);
                }
            }
            // If client disconnects, ignore and continue
            catch (IOException) {
            }
        }

        // GET requests are ready to go for processing
        private void HandleGETRequest() {
            this.ApiProcess();
        }

        // POST requests must have their parameters parsed in order to do API processing
        private const int BUF_SIZE = 4096;
        private void HandlePOSTRequest() {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream
            // we hand him needs to let him see the "end of the stream" at this content
            // length, because otherwise he won't know when he's seen it all!

            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (this.HttpHeaders.ContainsKey("Content-Length")) {
                content_len = Convert.ToInt32(this.HttpHeaders["Content-Length"]);
                if (content_len > MAX_POST_SIZE) {
                    throw new Exception(String.Format("POST Content-Length({0}) too big for this simple server", content_len));
                }

                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;

                while (to_read > 0) {
                    int numread = this.InputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                    if (numread == 0) {
                        if (to_read == 0) {
                            break;
                        } else {
                            throw new Exception("Client disconnected during HTTP POST");
                        }
                    }

                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }

                ms.Seek(0, SeekOrigin.Begin);
            }

            // Add all POST parameters to URL to allow for easy API processing
            this.HttpUrl = this.HttpUrl + "?" + new StreamReader(ms).ReadToEnd();

            // Process the API request
            this.ApiProcess();
        }

        // Process API calls based on the class's HTTP URL
        private void ApiProcess() {
            // The API request wrapper
            UriWrapper uri = new UriWrapper(this.HttpUrl, this.HttpMethod);

            // The user who is accessing the API
            User apiUser = null;

            // The handler being accessed
            IApiHandler api = null;

            // No API request found?  Serve web UI
            if (!uri.IsApiCall) {
                api = Injection.Kernel.Get<IApiHandlerFactory>().CreateApiHandler("web");
                api.Process(uri, this, apiUser);
                return;
            }

            // Get client IP address
            string ip = ((IPEndPoint)this.Socket.Client.RemoteEndPoint).Address.ToString();

            // Check for valid API action ("web" and "error" are technically valid, but can't be used in this way)
            if (uri.ApiAction == null || uri.ApiAction == "web" || uri.ApiAction == "error") {
                ErrorApiHandler errorApi = (ErrorApiHandler)Injection.Kernel.Get<IApiHandlerFactory>().CreateApiHandler("error");
                errorApi.Process(uri, this, apiUser, "Invalid API call");
                logger.IfInfo(String.Format("[{0}] API: {1}", ip, this.HttpUrl));
                return;
            }

            // Check for session cookie authentication, unless this is a login request
            string sessionId = null;
            if (uri.ApiAction != "login") {
                sessionId = this.GetSessionCookie();
                apiUser = Injection.Kernel.Get<IApiAuthenticate>().AuthenticateSession(sessionId);
            }

            // If no cookie, try parameter authentication
            if (apiUser == null) {
                apiUser = Injection.Kernel.Get<IApiAuthenticate>().AuthenticateUri(uri);

                // If user still null, failed authentication, so serve error
                if (apiUser == null) {
                    ErrorApiHandler errorApi = (ErrorApiHandler)Injection.Kernel.Get<IApiHandlerFactory>().CreateApiHandler("error");
                    errorApi.Process(uri, this, apiUser, "Authentication failed");
                    logger.IfInfo(String.Format("[{0}] API: {1}", ip, this.HttpUrl));
                    return;
                }
            }

            // apiUser.SessionId will be generated on new login, so that takes precedence for new session cookie
            apiUser.SessionId  = apiUser.SessionId ?? sessionId;
            this.SetSessionCookie(apiUser.SessionId);

            // Store user's current session object
            apiUser.CurrentSession = Injection.Kernel.Get<ISessionRepository>().SessionForSessionId(apiUser.SessionId);

            // Retrieve the requested API handler by its action
            IApiHandler apiHandler = Injection.Kernel.Get<IApiHandlerFactory>().CreateApiHandler(uri.ApiAction);

            // Check for valid API action
            if (apiHandler == null) {
                ErrorApiHandler errorApi = (ErrorApiHandler)Injection.Kernel.Get<IApiHandlerFactory>().CreateApiHandler("error");
                errorApi.Process(uri, this, apiUser, "Invalid API call");
                logger.IfInfo(String.Format("[{0}] API: {1}", ip, this.HttpUrl));
                return;
            }

            // Log API call
            logger.IfInfo(String.Format("[{0}/{1}@{2}] API: {3} {4}", apiUser.UserName, apiUser.CurrentSession.ClientName, ip, this.HttpMethod, this.HttpUrl));

            // Check if user has appropriate permissions for this action on this API handler
            if (!apiHandler.CheckPermission(apiUser, uri.Action)) {
                ErrorApiHandler errorApi = (ErrorApiHandler)Injection.Kernel.Get<IApiHandlerFactory>().CreateApiHandler("error");
                errorApi.Process(uri, this, apiUser, "Permission denied");
                return;
            }

            // Finally, process and return results
            apiHandler.Process(uri, this, apiUser);
        }

        // If a cookie is found, grab it and use it for authentication
        private string GetSessionCookie() {
            if (this.HttpHeaders.ContainsKey("Cookie")) {
                // Split each cookie into pairs
                string[] cookies = this.HttpHeaders["Cookie"].ToString().Split(new [] {';', ',', '='}, StringSplitOptions.RemoveEmptyEntries);

                // Iterate all cookies
                for (int i = 0; i < cookies.Length; i += 2) {
                    // Look for wavebox_session cookie
                    if (cookies[i] == "wavebox_session") {
                        string cookie = cookies[i + 1];
                        return cookie;
                    }
                }
            }

            return null;
        }

        // Set a new session cookie to be set when the HTTP response is sent
        private void SetSessionCookie(string sessionId) {
            if (sessionId != null) {
                // Calculate session timeout time (DateTime.UtcNow UTC + SessionTimeout minutes)
                DateTime expire = DateTime.UtcNow.ToUniversalTime().AddMinutes(Injection.Kernel.Get<IServerSettings>().SessionTimeout);

                // Add a delayed header so cookie will be reset on each API call (to prevent timeout)
                this.DelayedHeaders["Set-Cookie"] = String.Format("wavebox_session={0}; Path=/api; Expires={1};", sessionId, expire.ToRFC1123());
            }
        }

        private DateTime CleanLastModified(DateTime? lastModified) {
            // If null, use current time
            if (ReferenceEquals(lastModified, null)) {
                return DateTime.UtcNow;
            }

            // Make sure we're using UTC
            DateTime lastMod = ((DateTime)lastModified).ToUniversalTime();

            // If the time is later than now, use now
            if (DateTime.Compare(DateTime.UtcNow, lastMod) < 0) {
                lastMod = DateTime.UtcNow;
            }

            return lastMod;
        }
    }
}
