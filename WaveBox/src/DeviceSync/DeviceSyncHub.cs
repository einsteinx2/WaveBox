using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading.Tasks;
using System.Collections.Generic;
using WaveBox.Model;

namespace WaveBox.DeviceSync
{
	public class DeviceSyncHub : Hub
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/*
		 * Connection callbacks
		 */

		public override Task OnConnected()
		{
			logger.Info("OnConnected called, Context.ConnectionId: " + Context.ConnectionId);

			return base.OnConnected();
		}

		public override Task OnDisconnected()
		{
			logger.Info("OnDisconnected called, Context.ConnectionId: " + Context.ConnectionId);

			// Remove the session id - connection id association

			return base.OnDisconnected();
		}

		public override Task OnReconnected()
		{
			logger.Info("OnReconnected called, Context.ConnectionId: " + Context.ConnectionId);

			// Update the client on the current play queue and playback state

			Clients.Caller.currentState(new List<Song>(), 5, true, false, true);

			return base.OnReconnected();
		}

		/*
		 * State information methods
		 */

		// Client calls this upon connecting to associate the session id with the connection id
		public void Identify(string sessionId)
		{
			logger.Info("sessionId: " + sessionId);

			// Create the session id - connection id association

			// Update the client on the current state
			Clients.Caller.currentState(new List<Song>(), 5, true, false, true);
		}

		// Client calls this to inform the other clients and the server than it's play queue has changed
		public void PlayQueueChanged(List<int> songIds)
		{
			logger.Info("song ids count: " + songIds.Count + " item 0: " + songIds[0]);
		}

		// Client calls this to inform the other clients that it is taking over playback and to pause the other clients
		public void TakeOverPlayback()
		{

		}

		// Client calls this to inform the other clients that it has switched songs
		public void PlayQueueIndexChanged(int currentIndex)
		{

		}

		// Client calls this periodically to ensure that the player progress is properly synced
		public void ProgressUpdate(float progress)
		{

		}

		/*
		 * Remote control methods
		 */

		// Client calls this to toggle playback on the currently playing device
		public void RemoteTogglePlayback()
		{

		}

		// Client calls this to change the play queue index on the currently playing device,
		// this is used in place of next/prev methods
		public void RemoteSkipToIndex(int index)
		{

		}

		// Client calls this to toggle shuffle mode on the currently playing device
		public void RemoteToggleShuffle()
		{

		}

		// Client calls this to toggle repeat mode on the currently playing device
		public void RemoteToggleRepeat()
		{

		}
	}
}

