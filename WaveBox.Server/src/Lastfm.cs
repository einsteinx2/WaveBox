using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
using System.Web;
using WaveBox.Model;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Web.Services;
using System.Net.Sockets;

namespace WaveBox
{
	public class Lastfm
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static string apiKey = "6aec36725ab20cff28e8525cdf5fbd4a";
		private static string secret = "cd596009d199d51405a2477d4e65c5d7";
		private string sessionKey = null;
		private User user;

		private string authUrl;
		public string AuthUrl
		{
			get
			{
				if (authUrl == null)
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
		public Lastfm(User theUser)
		{
			user = theUser;
			sessionKey = user.LastfmSession;

			// If the session key is prepended by 'token:', then the user has already generated a request token.
			// we should now try to get a session using that token.  If this fails, that simply means the user
			// has not granted us access yet.  If there is no session key at all, then we should request a token
			// and do nothing else.

			if (sessionKey == null)
			{
				CreateAuthUrl();
				if (logger.IsInfoEnabled) logger.Info(this.AuthUrl);
			}
			else if (sessionKey.Substring(0, 6) == "token:")
			{
				string token = sessionKey.Substring(6);
				GetSessionKeyAndUpdateUser(token);
			}
			else
			{
				sessionAuthenticated = true;
			}
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
		public string Scrobble(List<LfmScrobbleData> scrobbles, LfmScrobbleType scrobbleType)
		{
			if (scrobbles.Count == 0)
			{
				return null;
			}

			Song song = null;

			SortedDictionary<string, string> parameters = new SortedDictionary<string, string>();

			// add the scrobble data to the parameter list
			int limit = scrobbleType == LfmScrobbleType.NOWPLAYING ? 1 : scrobbles.Count > 100 ? 100 : scrobbles.Count;
			long timestamp = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);

			for (int i = 0; i < limit; i++)
			{
				song = new Song(scrobbles[i].SongId);
				parameters.Add("artist" + (scrobbleType == LfmScrobbleType.NOWPLAYING ? "" : string.Format("[{0}]", i)), HttpUtility.UrlEncode(song.ArtistName, Encoding.UTF8));
				parameters.Add("timestamp" + (scrobbleType == LfmScrobbleType.NOWPLAYING ? "" : string.Format("[{0}]", i)), HttpUtility.UrlEncode(timestamp.ToString(), Encoding.UTF8));
				parameters.Add("track" + (scrobbleType == LfmScrobbleType.NOWPLAYING ? "" : string.Format("[{0}]", i)), HttpUtility.UrlEncode(song.SongName, Encoding.UTF8));
			}

			// add the api and session keys to the parameter list
			parameters.Add("api_key", apiKey);
			parameters.Add("sk", sessionKey);

			// choose the appropriate method and add it to the parameter list
			if (scrobbleType == LfmScrobbleType.SUBMIT)
			{
				parameters.Add("method", "track.scrobble");
			}
			else if (scrobbleType == LfmScrobbleType.NOWPLAYING)
			{
				parameters.Add("method", "track.updateNowPlaying");
				parameters.Add("duration", song.Duration.ToString());
			}

			// or if it's invalid, return without doing anything.
			else 
			{
				return null;
			}
			
			// then compile the request and do it
			string p = CompileApiCall(parameters);
			string resp = DoPostRestRequest(p);

			return RemoveHttpHeaders(resp);
		}

		public static string GetArtistInfo(Artist artist)
		{
			var p = new SortedDictionary<string, string>();

			p.Add("artist", artist.ArtistName);
			p.Add("api_key", apiKey);
			p.Add("method", "artist.getInfo");

			var url = CompileApiCall(p);
			string result = DoPostRestRequest(url);

			return RemoveHttpHeaders(result);
		}

		public static string GetArtistInfo(int artistId)
		{
			return GetArtistInfo(new Artist(artistId));
		}

		private static string CompileApiCall(SortedDictionary<string, string> parameters)
		{
			string sig = "";
			string cmd = "";

			// create the API signature from the given parameters
			SortedDictionary<string, string>.Enumerator enumerator = parameters.GetEnumerator();
			while (enumerator.MoveNext())
			{
				sig += enumerator.Current.Key + HttpUtility.UrlDecode(enumerator.Current.Value);
			}

			sig += secret;
			sig = md5(sig);

			parameters.Add("api_sig", sig);
			parameters.Add("format", "json");


			// using the API signature that was just added to the parameter dictionary, compile the command.
			enumerator = parameters.GetEnumerator();

			bool firstKey = true;
			while (enumerator.MoveNext())
			{
				if (!firstKey)
				{
					cmd += "&";
				}
				else firstKey = false;

				cmd += string.Format("{0}={1}", enumerator.Current.Key, enumerator.Current.Value);
			}

			return cmd;
		}

		public static string RemoveHttpHeaders(string resp)
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

			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUrl);

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
				logger.Info ("[SCROBBLE] (" + user.UserName + ") Obtain last.fm session key: success");
			}
			else sessionAuthenticated = false;
		}

		/// <summary>
		/// Md5 the specified input.
		/// </summary>
		/// <param name='input'>
		/// Input.
		/// </param>
		private static string md5(string input)
		{
			MD5CryptoServiceProvider m = new MD5CryptoServiceProvider();
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

			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(requestUrl);

			using (HttpWebResponse response = req.GetResponse() as HttpWebResponse)
			{  
				StreamReader reader = new StreamReader(response.GetResponseStream());  
				jsonResponse = JsonConvert.DeserializeObject(reader.ReadToEnd());
			}

			requestToken = jsonResponse.token.ToString();

			if (requestToken != null)
			{
				user.UpdateLastfmSession("token:" + requestToken);
				logger.Info ("[SCROBBLE] (" + user.UserName + ") Obtain last.fm authentication request token: success");
			}

			string url = "http://www.last.fm/api/auth/?" + 
				string.Format ("api_key={0}", apiKey) + 
				string.Format ("&token={0}", requestToken);

			authUrl = url;
		}

		private static string DoGetRestRequest(string url)
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
		private static string DoPostRestRequest(string parameters)
		{
			string resp = "";

			try
			{
				TcpClient s = new TcpClient("ws.audioscrobbler.com", 80);
				StringBuilder req = new StringBuilder();
				req.Append(string.Format("POST /2.0/?{0} HTTP/1.1\r\n", parameters));
				req.Append("Accept: application/json; charset=utf-8\r\n");
				req.Append("Host: ws.audioscrobbler.com\r\n");
				req.Append("Content-Type: application/x-www-form-urlencoded; charset=utf-8;\r\n");
				req.Append("Content-Length: 0\r\n");
				req.Append("User-Agent: WaveBox/1.0\r\n");
				req.Append("Connection: close\r\n\r\n");

				byte[] headerBytes = Encoding.ASCII.GetBytes(req.ToString());

				NetworkStream stream = s.GetStream();
				stream.Write(headerBytes, 0, headerBytes.Length);

				//if (logger.IsInfoEnabled) logger.Info(req.ToString());

				byte[] receive = new byte[256];
				MemoryStream m = new MemoryStream();
				int numRead = 0;

				while ((numRead = stream.Read(receive, 0, receive.Length)) > 0)
				{
					m.Write(receive, 0, numRead);
				}

				byte[] finalByteArray = m.ToArray();
				resp = Encoding.UTF8.GetString(finalByteArray, 0, finalByteArray.Length);

				//if (logger.IsInfoEnabled) logger.Info(resp);
				stream.Close();
				s.Close();
			} 

			catch (Exception e)
			{
				logger.Error(e);
			}

			return resp;
	}

		public static LfmScrobbleType ScrobbleTypeForString(string input)
		{
			var theInput = input.ToLower();
			if (theInput == "nowplaying")
			{
				return LfmScrobbleType.NOWPLAYING;
			}
			else if (theInput == "submit")
			{
				return LfmScrobbleType.SUBMIT;
			}
			else if (theInput == "auth")
			{
				return LfmScrobbleType.AUTH;
			}
			else
			{
				return LfmScrobbleType.INVALID;
			}
		}
	}

	public class LfmScrobbleData
	{
		public int SongId { get; set; }
		public long? Timestamp { get; set; }

		public LfmScrobbleData(int songId, long? timestamp)
		{
			SongId = songId;
			Timestamp = timestamp;
		}
	}

	public enum LfmScrobbleType
	{
		AUTH = 0,
		NOWPLAYING = 1,
		SUBMIT = 2,
		INVALID = 3
	}
}

