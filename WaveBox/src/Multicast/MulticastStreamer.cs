using System;
using RTP;
using NLog;

namespace WaveBox.Multicast
{
	public class MulticastStreamer
	{
		private static Logger logger = LogManager.GetCurrentClassLogger();

		// Private instance variables
		private MulticastSender sender;
		private RTPPacket rtpHeader = new RTPPacket();
		private string ip;
		private int port;
		private int ttl;

		/// <summary>
		/// Default constructor for MulticastStreamer
		/// </summary>
		public MulticastStreamer()
		{
			// Set all RTP parameters
			this.ConfigRTPHeader();

			// Configure server as default
			this.ip = MulticastConfiguration.Address;
			this.port = MulticastConfiguration.Port;
			this.ttl = MulticastConfiguration.TTL;
		}

		/// <summary>
		/// Configurable constructor for MulticastStreamer
		/// </summary>
		public MulticastStreamer(string ip, int port, int ttl)
		{
			// Set all RTP parameters
			this.ConfigRTPHeader();

			// Configure server
			this.ip = ip;
			this.port = port;
			this.ttl = ttl;
		}

		/// <summary>
		/// Begin a RTP multicast stream
		/// </summary>
		public void Start()
		{
			logger.Info("[MULTICASTSTREAMER] Starting RTP multicast: " + this.ip + ':' + this.port);

			// Initialize multicast sender
			this.sender = new MulticastSender(this.ip, this.port, this.ttl);
		}

		/// <summary>
		/// Stop a RTP multicast stream
		/// </summary>
		public void Stop()
		{
			logger.Info("[MULTICASTSTREAMER] Stopping RTP multicast");

			// Stop multicast sender
			this.sender.Close();
			this.sender = null;
		}

		/// <summary>
		/// Configure the RTP header packet
		/// </summary>
		private void ConfigRTPHeader()
		{
			this.rtpHeader.Version = 2;
			this.rtpHeader.Padding = false;
			this.rtpHeader.Extension = false;
			this.rtpHeader.CSRCCount = 0;
			this.rtpHeader.Marker = false;
			this.rtpHeader.PayloadType = 0;
			this.rtpHeader.SequenceNumber = Convert.ToUInt16(new Random(0).Next(System.UInt16.MaxValue));
			this.rtpHeader.Timestamp = Convert.ToUInt32(new Random(0).Next());
			this.rtpHeader.SourceId = 0;
		}

		/// <summary>
		/// Generate RTP stream and send from source byte array
		/// </summary>
		private void SendDataStream(byte[] data)
		{
			try
			{
				lock (this)
				{
					// Ensure multicast sender is initialized
					if (this.sender != null)
					{
						// Convert linear data to mu-law for transmission
						byte[] mulaw = Utils.LinearToMulaw(data, MulticastConfiguration.BitsPerSample, MulticastConfiguration.Channels);

						// Set sequence number and timestamp
						try
						{
							this.rtpHeader.SequenceNumber = Convert.ToUInt16(this.rtpHeader.SequenceNumber + 1);
						}
						catch (Exception)
						{
							this.rtpHeader.SequenceNumber = 0;
						}
						try
						{
							this.rtpHeader.Timestamp = Convert.ToUInt32(this.rtpHeader.Timestamp + mulaw.Length);
						}
						catch (Exception)
						{
							this.rtpHeader.Timestamp = 0;
						}

						// Get bytes from RTP header
						byte[] rtpBytes = this.rtpHeader.ToBytes();

						// Copy header and data into destination array
						byte[] bytes = new byte[mulaw.Length + this.rtpHeader.HeaderLength];
						Array.Copy(rtpBytes, 0, bytes, 0, this.rtpHeader.HeaderLength);
						Array.Copy(mulaw, 0, bytes, this.rtpHeader.HeaderLength, mulaw.Length);

						// Send byte package
						this.sender.SendBytes(bytes);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("[MULTICASTSTREAMER] EXCEPTION: " + e.Message);
			}
		}
	}

	// Configuration for RTP multicast
	public static class MulticastConfiguration
	{
		public static string Address = "239.0.0.0";
		public static int Port = 5004;
		public static int TTL = 10;
		public static int SamplesPerSecond = 8000;
		public static short BitsPerSample = 16;
		public static short Channels = 2;
		public static int PacketSize = 1024;
		public static int BufferCount = 8;
	}
}
