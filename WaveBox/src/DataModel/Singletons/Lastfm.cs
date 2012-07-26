using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Web;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Net.Sockets;

namespace WaveBox.ApiHandler.Handlers
{
	public class Lastfm
	{
		private readonly string apiKey = "6aec36725ab20cff28e8525cdf5fbd4a";
		private readonly string secret = "cd596009d199d51405a2477d4e65c5d7";
		private string sessionKey = null;
		private User user;

		private string authUrl;
        public string AuthUrl
        {
            get
            {
                if(authUrl == null)
                {
                    CreateAuthUrl();
                    return authUrl;
                }
                else 
				{
					return authUrl;
				}
            }
        }

        private bool sessionAuthenticated;
        public bool SessionAuthenticated { get { return sessionAuthenticated; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveBox.ApiHandler.Handlers.Lastfm"/> class.
        /// </summary>
        /// <param name='userId'>
        /// User identifier.
        /// </param>
		public Lastfm(int userId)
		{
			user = new User(userId);
			sessionKey = user.LastfmSession;

			// If the session key is prepended by 'token:', then the user has already generated a request token.
			// we should now try to get a session using that token.  If this fails, that simply means the user
			// has not granted us access yet.  If there is no session key at all, then we should request a token
			// and do nothing else.

			if (sessionKey == null)
			{
				CreateAuthUrl();
				Console.WriteLine(this.AuthUrl);
			} 

			else if(sessionKey.Substring(0, 6) == "token:")
			{
				string token = sessionKey.Substring(6);
				GetSessionKeyAndUpdateUser(token);
			}
            else sessionAuthenticated = true;
		}

        /// <summary>
        /// Scrobble the specified songId and recordScrobble.
        /// </summary>
        /// <param name='songId'>
        /// If set to <c>true</c> song identifier.
        /// </param>
        /// <param name='recordScrobble'>
        /// If set to <c>true</c> record scrobble.
        /// </param>
		public string Scrobble(int songId, bool recordScrobble)
		{
			var song = new Song(songId);
			if (song.ItemId == 0)
			{
				return null;
			}

			long timestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
			string p, apiSig;


			if (recordScrobble)
			{
				apiSig = md5("api_key" + apiKey + "artist[0]" + song.ArtistName + "method" + "track.scrobble" + "sk" + sessionKey + "timestamp[0]" + timestamp + "track[0]" + song.SongName + secret);
				p = String.Format("method=track.scrobble&api_key={0}&sk={1}&artist[0]={2}&track[0]={3}&timestamp[0]={4}&api_sig={5}", apiKey, sessionKey, 
			                         UrlEncode(song.ArtistName, Encoding.UTF8), UrlEncode(song.SongName, Encoding.UTF8), timestamp, apiSig);
			} 

			else
			{
				apiSig = md5("api_key" + apiKey + "artist" + song.ArtistName + "method" + "track.updateNowPlaying" + "sk" + sessionKey + "timestamp" + timestamp + "track" + song.SongName + secret);
				p = String.Format("method=track.updateNowPlaying&format=json&api_key={0}&sk={1}&artist={2}&track={3}&timestamp={4}&api_sig={5}", apiKey, sessionKey, 
			                         UrlEncode(song.ArtistName, Encoding.UTF8), UrlEncode(song.SongName, Encoding.UTF8), timestamp, apiSig);
			}

			string resp = DoPostRestRequest(p);

			return RemoveHttpHeaders(resp);
		}

        public string RemoveHttpHeaders(string resp)
        {
            return resp.Substring(resp.IndexOf("\r\n\r\n") + 4);
        }

        /// <summary>
        /// Gets the session key and updates the user's lastfm session in the database.
        /// </summary>
        /// <param name='token'>
        /// The session passed to the last.fm API when requesting a session key.
        /// </param>
		private void GetSessionKeyAndUpdateUser(string token)
		{
			string apiSigSource = "api_key" + apiKey + "method" + "auth.getSession" + "token" + token + secret;
			string apiSig = md5 (apiSigSource);
			dynamic jsonResponse;
			string requestUrl = "http://ws.audioscrobbler.com/2.0/?method=auth.getSession&format=json" + 
				string.Format("&api_key={0}", apiKey) + 
				string.Format("&token={0}", token) + 
				string.Format("&api_sig={0}", apiSig);

			var req = (HttpWebRequest)WebRequest.Create(requestUrl);

			using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
			{  
				StreamReader reader = new StreamReader(response.GetResponseStream());  
				jsonResponse = JsonConvert.DeserializeObject(reader.ReadToEnd());
			}

			if (jsonResponse.session != null)
			{
				sessionKey = jsonResponse.session.key.ToString();
                sessionAuthenticated = true;
				user.UpdateLastfmSession(sessionKey);
				Console.WriteLine ("[SCROBBLE] ({0}) Obtain last.fm session key: success", user.UserName);
			}
            else sessionAuthenticated = false;
		}

        /// <summary>
        /// Md5 the specified input.
        /// </summary>
        /// <param name='input'>
        /// Input.
        /// </param>
		private string md5(string input)
		{
			var m = new MD5CryptoServiceProvider();
			return BitConverter.ToString(m.ComputeHash(Encoding.ASCII.GetBytes(input))).Replace("-", string.Empty);
		}

        /// <summary>
        /// Creates the auth URL.
        /// </summary>
		private void CreateAuthUrl()
		{
			string requestToken = null;
			dynamic jsonResponse;

			// Get a last.fm request token
			string requestUrl = "http://ws.audioscrobbler.com/2.0/?method=auth.gettoken&format=json" + 
				string.Format("&api_key={0}", apiKey);

			var req = (HttpWebRequest)WebRequest.Create(requestUrl);

			using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
			{  
				StreamReader reader = new StreamReader(response.GetResponseStream());  
				jsonResponse = JsonConvert.DeserializeObject(reader.ReadToEnd());
			}

			requestToken = jsonResponse.token.ToString();

			if (requestToken != null)
			{
				user.UpdateLastfmSession("token:" + requestToken);
				Console.WriteLine ("[SCROBBLE] ({0}) Obtain last.fm authentication request token: success", user.UserName);
			}

			string url = "http://www.last.fm/api/auth/?" + 
				string.Format ("api_key={0}", apiKey) + 
				string.Format ("&token={0}", requestToken);

			authUrl = url;
		}

		private string DoGetRestRequest(string url)
		{
			return string.Empty;
		}

        /// <summary>
        /// Executes rest requests using the POST method
        /// </summary>
        /// <returns>
        /// The rest response.
        /// </returns>
        /// <param name='parameters'>
        /// Parameters.
        /// </param>
        private string DoPostRestRequest(string parameters)
        {
            string resp = "";

            try
            {
                var s = new System.Net.Sockets.TcpClient("ws.audioscrobbler.com", 80);
                var req = new StringBuilder();
                req.Append(string.Format("POST /2.0/?{0} HTTP/1.1\r\n", parameters));
                req.Append("Accept: application/json; charset=utf-8\r\n");
                req.Append("Host: ws.audioscrobbler.com\r\n");
                req.Append("Content-Type: application/x-www-form-urlencoded; charset=utf-8;\r\n");
                req.Append("Content-Length: 0\r\n");
                req.Append("User-Agent: WaveBox/1.0\r\n");
                req.Append("Connection: close\r\n\r\n");

                var headerBytes = Encoding.ASCII.GetBytes(req.ToString());

                var stream = s.GetStream();
                stream.Write(headerBytes, 0, headerBytes.Length);

                //Console.WriteLine(req.ToString());

                byte[] receive = new byte[256];
                var m = new MemoryStream();
                int numRead = 0;

                while ((numRead = stream.Read(receive, 0, receive.Length)) > 0)
                {
                    m.Write(receive, 0, numRead);
                }

                var finalByteArray = m.ToArray();
                resp = Encoding.UTF8.GetString(finalByteArray, 0, finalByteArray.Length);

                //Console.WriteLine(resp);
                stream.Close();
                s.Close();
            } 

            catch (Exception e)
            {
                Console.WriteLine("[LASTFM(1)] " + e.ToString());
            }

            return resp;
    }

        // Thanks to: http://software.1713.n2.nabble.com/System-Web-HttpUtility-UrlEncode-equivalent-in-OS-X-td721075.html
        // For some reason, Mono doesn't implement System.Web.HttpServerUtility.UrlEncode().  Weird.
        public static string UrlEncode(string s, Encoding e) 
		{ 
                StringBuilder sb = new StringBuilder(); 

                foreach (byte i in e.GetBytes(s)) 
				{ 
                        if ((i >= 'A' && i <= 'Z') || 
                            (i >= 'a' && i <= 'z') || 
                            (i >= '0' && i <= '9') || 
                             i == '-' || i == '_') 
						{ 
                        	sb.Append((char) i); 
                        } 
						else if (i == ' ') 
						{ 
							sb.Append('+'); 
                        } 
						else 
						{
                        	sb.Append('%'); 
                        	sb.Append(i.ToString("X2")); 
                        } 
                } 

                return sb.ToString(); 
        } 
	}
}

