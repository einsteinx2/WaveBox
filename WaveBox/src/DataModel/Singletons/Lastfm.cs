using System;
using System.Net;
using System.IO;
using System.Text;
using System.Web;
using WaveBox.DataModel.Model;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace WaveBox.ApiHandler.Handlers
{
	public class Lastfm
	{
		private readonly string apiKey = "6aec36725ab20cff28e8525cdf5fbd4a";
		private readonly string secret = "cd596009d199d51405a2477d4e65c5d7";
		private string sessionKey = null;
		private User user;

		private string authUrl;
		public string AuthUrl { 
			get 
			{
				return authUrl;
			}
		}

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
		}

		public bool Scrobble(int songId, bool recordScrobble)
		{
			var song = new Song(songId);
			if (song.ItemId == 0)
			{
				return false;
			}

			var timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
			string requestUrl = "http://ws.audioscrobbler.com/2.0/";
			string p, apiSig;


			if (recordScrobble)
			{
				apiSig = md5("api_key" + apiKey + "artist[0]" + song.ArtistName + "method" + "track.scrobble" + "timestamp[0]" + timestamp + "track[0]" + song.SongName + secret);
				p = String.Format("method=track.scrobble&api_key={0}&sk={1}&artist[0]={2}&track[0]={3}&timestamp[0]={4}&api_sig={5}", apiKey, sessionKey, 
			                         song.ArtistName, song.SongName, timestamp, apiSig);
			} 

			else
			{
				apiSig = md5("api_key" + apiKey + "artist[0]" + song.ArtistName + "method" + "track.updateNowPlaying" + "timestamp[0]" + timestamp + "track[0]" + song.SongName + secret);
				p = String.Format("method=track.updateNowPlaying&api_key={0}&sk={1}&artist[0]={2}&track[0]={3}&timestamp[0]={4}&api_sig={5}", apiKey, sessionKey, 
			                         song.ArtistName, song.SongName, timestamp, apiSig);
			}

			string a = DoPostRestRequest(requestUrl, p);



			return true;
		}

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

			string sk = jsonResponse.session.key.ToString();
			if (sk != null)
			{
				sessionKey = sk;
				user.UpdateLastfmSession(sk);
				Console.WriteLine ("[SCROBBLE] ({0}) Obtain session key: success");
			}
		}

		private string md5(string input)
		{
			var m = new MD5CryptoServiceProvider();
			return BitConverter.ToString(m.ComputeHash(Encoding.ASCII.GetBytes(input))).Replace("-", string.Empty);
		}

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
				Console.WriteLine ("[SCROBBLE] ({0}) Obtain request token: success");
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

		private string DoPostRestRequest(string url, string parameters)
		{
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";

			var byteParams = UTF8Encoding.UTF8.GetBytes(parameters);
			request.ContentLength = byteParams.Length;

			using (Stream pStream = request.GetRequestStream())
			{
				pStream.Write(byteParams, 0, byteParams.Length);
                pStream.Close();
			}

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			var reader = new StreamReader(response.GetResponseStream());
			return reader.ReadToEnd();
		}
	}
}

