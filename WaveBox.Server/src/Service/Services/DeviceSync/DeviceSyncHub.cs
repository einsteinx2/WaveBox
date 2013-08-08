using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Static;
using WaveBox.Core.Model.Repository;
using Ninject;
using WaveBox.Core;

namespace WaveBox.Service.Services.DeviceSync
{
	public class DeviceSyncHub : Hub
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static IDictionary<string, Group> groups = new Dictionary<string, Group>();

		/*
		 * Private Methods
		 */

		private Group AddConnection(Connection conn)
		{
			lock (groups)
			{
				Group group = null;
				string key = conn.SessionId;
				if (groups.ContainsKey(key))
				{
					// Add the connection to the group
					group = groups[key];
					conn.MyGroup = group;
					group.Connections.Add(conn);
				}
				else
				{
					// Create a new group and insert it
					group = new Group(key);
					conn.MyGroup = group;
					group.Connections.Add(conn);
					groups[key] = group;
				}

				return group;
			}
		}

		private Group RemoveConnection(Connection conn)
		{
			lock (groups)
			{
				string key = conn.SessionId;
				if (groups.ContainsKey(key))
				{
					Group group = groups[key];
					group.Connections.Remove(conn);

					if (group.Connections.Count == 0)
					{
						groups.Remove(key);
					}

					return group;
				}

				return null;
			}
		}

		private Connection ConnectionForId(string connectionId)
		{
			lock (groups)
			{
				IList<Connection> matchedConnection = (from grp in groups.Values 
													from conn in grp.Connections
													where conn.ConnectionId == connectionId
													select conn).ToList();

				if (matchedConnection.Count > 0)
				{
					return matchedConnection[0];
				}

				return null;
			}
		}

		/*
		 * Connection callbacks
		 */

		public override Task OnConnected()
		{
			logger.IfInfo("OnConnected called, Context.ConnectionId: " + Context.ConnectionId);

			// Ask the client to identify itself, so we can create the session id - connection id association
			return Clients.Caller.identify();
		}

		public override Task OnReconnected()
		{
			logger.IfInfo("OnReconnected called, Context.ConnectionId: " + Context.ConnectionId);

			// Ask the client to identify itself, so we can recreate the session id - connection id association
			return Clients.Caller.identify();
		}

		public override Task OnDisconnected()
		{
			logger.IfInfo("OnDisconnected called, Context.ConnectionId: " + Context.ConnectionId);

			// Remove the session id - connection id association
			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (!ReferenceEquals(conn, null))
				{
					RemoveConnection(conn);
				}
			});
		}

		/*
		 * State information methods
		 */

		// Client calls this upon connecting to associate the session id with the connection id
		public Task Identify(string sessionId, string clientName)
		{
			logger.IfInfo("Identify: " + sessionId + ", " + clientName);

			return Task.Factory.StartNew(() => 
			{
				// Add to the SignalR group
				Groups.Add(Context.ConnectionId, sessionId);

				// Save in our dictionary for tracking
				Group group = AddConnection(new Connection(Context.ConnectionId, sessionId, clientName));

				// Tell the client the current group state
				var songIds = group.SongIds.Count > 0 ? Injection.Kernel.Get<ISongRepository>().SongsForIds(group.SongIds) : new List<Song>();
				var progress = group.Progress + (float)(DateTime.Now.ToUniversalUnixTimestamp() - group.ProgressTimestamp);
				Clients.Caller.currentState(songIds, group.CurrentIndex, progress, group.IsShuffle, group.IsRepeat);
			});
		}

		// Client calls this to inform the other clients that it is taking over playback and to pause the other clients
		public Task TakeOverPlayback()
		{
			logger.IfInfo("TakeOverPlayback");

			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (!ReferenceEquals(conn, null))
				{
					conn.MyGroup.ActiveConnection = conn;

					Clients.OthersInGroup(conn.SessionId).takeOverPlayback(conn.ClientName);
				}
			});
		}

		// Client calls this to inform the other clients and the server than it's play queue has changed
		public Task PlayQueueChanged(List<int> songIds)
		{
			logger.IfInfo("PlayQueueChanged: " + string.Join(",", songIds.Select(i => i.ToString()).ToArray()));

			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (!ReferenceEquals(conn, null))
				{
					var songs = songIds.Count > 0 ? Injection.Kernel.Get<ISongRepository>().SongsForIds(songIds) : new List<Song>();
					conn.MyGroup.SongIds = songIds;
					Clients.OthersInGroup(conn.SessionId).playQueueChanged(songs);
				}
			});
		}

		// Client calls this to inform the other clients that it has switched songs
		public Task PlayQueueIndexChanged(int currentIndex)
		{
			logger.IfInfo("PlayQueueIndexChanged: " + currentIndex);

			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (!ReferenceEquals(conn, null))
				{
					Clients.OthersInGroup(conn.SessionId).playQueueChanged(currentIndex);
				}
			});
		}

		// Client calls this periodically to ensure that the player progress is properly synced
		public Task ProgressUpdate(float progress)
		{
			logger.IfInfo("ProgressUpdate: " + progress);

			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (!ReferenceEquals(conn, null))
				{
					conn.MyGroup.Progress = progress;
					conn.MyGroup.ProgressTimestamp = DateTime.Now.ToUniversalUnixTimestamp();

					Clients.OthersInGroup(conn.SessionId).progressUpdate(progress);
				}
			});
		}

		/*
		 * Remote control methods
		 */

		// Client calls this to toggle playback on the currently playing device
		public Task RemoteTogglePlayback()
		{
			logger.IfInfo("RemoteTogglePlayback");

			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (!ReferenceEquals(conn, null))
				{
					Connection activeConnection = conn.MyGroup.ActiveConnection;
					if (!ReferenceEquals(activeConnection, null))
					{
						Clients.Client(activeConnection.ConnectionId).togglePlayback();
					}
				}
			});
		}

		// Client calls this to change the play queue index on the currently playing device,
		// this is used in place of next/prev methods
		public Task RemoteSkipToIndex(int index)
		{
			logger.IfInfo("RemoteSkipToIndex: " + index);

			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (conn != null)
				{
					Connection activeConnection = conn.MyGroup.ActiveConnection;
					if (!ReferenceEquals(activeConnection, null))
					{
						Clients.Client(activeConnection.ConnectionId).skipToIndex(index);
					}
				}
			});
		}

		// Client calls this to toggle shuffle mode on the currently playing device
		public Task RemoteToggleShuffle()
		{
			logger.IfInfo("RemoteToggleShuffle");

			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (conn != null)
				{
					Connection activeConnection = conn.MyGroup.ActiveConnection;
					if (!ReferenceEquals(activeConnection, null))
					{
						Clients.Client(activeConnection.ConnectionId).toggleShuffle();
					}
				}
			});
		}

		// Client calls this to toggle repeat mode on the currently playing device
		public Task RemoteToggleRepeat()
		{
			logger.IfInfo("RemoteToggleRepeat");

			return Task.Factory.StartNew(() => 
			{
				Connection conn = ConnectionForId(Context.ConnectionId);
				if (conn != null)
				{
					Connection activeConnection = conn.MyGroup.ActiveConnection;
					if (!ReferenceEquals(activeConnection, null))
					{
						Clients.Client(activeConnection.ConnectionId).toggleRepeat();
					}
				}
			});
		}

		private class Connection
		{
			public string ConnectionId { get; set; }

			public string SessionId { get; set; }

			public string ClientName { get; set; }

			public Group MyGroup { get; set; }

			public Connection(string connectionId, string sessionId, string clientName)
			{
				ConnectionId = connectionId;
				SessionId = sessionId;
				ClientName = clientName;
			}
		}

		private class Group
		{
			public string SessionId { get; set; }

			public Connection ActiveConnection { get; set; }

			public IList<Connection> Connections { get; set; }

			public IList<int> SongIds { get; set; }

			public int CurrentIndex { get; set; }

			public bool IsPlaying { get; set; }

			public bool IsShuffle { get; set; }

			public bool IsRepeat { get; set; }

			public float Progress { get; set; }

			public long ProgressTimestamp { get; set; }

			public Group()
			{
				Connections = new List<Connection>();
				SongIds = new List<int>();
			}

			public Group(string sessionId) : this()
			{
				SessionId = sessionId;
			}
		}
	}
}
