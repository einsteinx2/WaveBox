using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;

namespace WaveBox.TcpServer.Mpd
{
	public class MpdProcessor
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public TcpClient Socket { get; set; }
		public MpdServer Srv { get; set; }

		private NetworkStream Net;
		private StreamReader Reader;
		private StreamWriter Writer;

		public MpdProcessor(TcpClient s, MpdServer srv) 
		{
			Socket = s;
			Srv = srv;
		}

		public void Process()
		{
			// Grab socket stream, open I/O streams
			Net = Socket.GetStream();
			Reader = new StreamReader(Net, Encoding.UTF8);
			Writer = new StreamWriter(new BufferedStream(Net), new UTF8Encoding(false));
			Writer.AutoFlush = true;

			// Send greeting to client
			Writer.WriteLine(MpdResponse.Greeting);

			// Loop until client wishes to quit
			string input = null;
			string output = null;
			while (input != MpdCommand.Close)
			{
				// Read input from socket
				try
				{
					input = Reader.ReadLine();
				}
				catch (Exception e)
				{
					logger.Error(e.ToString());
					break;
				}

				// Process input
				output = Command(input);

				// Send reply to client
				Writer.WriteLine(output);
			}

			Halt();
		}

		public void Halt()
		{
			// Close all streams, close socket
			Reader.Close();
			Writer.Close();
			Net.Close();
			Socket.Close();
		}

		public string Command(string cmd)
		{
			logger.Info("mpd: " + cmd);

			// Check for errors
			MpdError error = MpdError.None;

			// Number in command list, for error output
			int listNum = 0;

			// Output details
			string outputDetails = null;

			// Error details
			string errorDetails = null;

			// Break command into array split by spaces
			string[] cmdArray = cmd.Split(' ');

			switch (cmdArray[0])
			{
				case MpdCommand.Close:
					Halt();
					break;
				case MpdCommand.LsInfo:
					// Check if argument exists
					if (cmdArray.Length < 2)
					{
						outputDetails = MpdOperation.LsInfo(null);
						break;
					}

					outputDetails = MpdOperation.LsInfo(cmdArray[1]);
					break;
				case MpdCommand.Next:
					Jukebox.Instance.Next();
					break;
				case MpdCommand.Pause:
					// Check for boolean, toggle pause (deprecated in mpd, but still implemented)
					if (cmdArray.Length < 2)
					{
						Jukebox.Instance.Pause(true);
						break;
					}

					// 0 to play, 1 to pause, else error
					int v = Convert.ToInt32(cmdArray[1]);
					if (v == 0)
					{
						Jukebox.Instance.Play();
					}
					else if (v == 1)
					{
						Jukebox.Instance.Pause();
					}
					else
					{
						error = MpdError.Arg;
						errorDetails = "Boolean (0/1) expected: " + v;
					}
					break;
				case MpdCommand.Ping:
					break;
				case MpdCommand.Play:
					// Check for index, or start at 0
					if (cmdArray.Length < 2)
					{
						logger.Info("mpd start play");
						Jukebox.Instance.PlaySongAtIndex(0);
						logger.Info("mpd stop play");
						break;
					}

					// If index, play song at it
					Jukebox.Instance.PlaySongAtIndex(Convert.ToInt32(cmdArray[1]));
					break;
				case MpdCommand.Previous:
					Jukebox.Instance.Prev();
					break;
				case MpdCommand.Status:
					outputDetails = MpdOperation.Status();
					break;
				case MpdCommand.Stop:
					Jukebox.Instance.Stop();
					break;
				default:
					error = MpdError.Unknown;
					errorDetails = "unknown command \"" + cmd + "\"";
					break;
			}

			if (error == MpdError.None && outputDetails == null)
			{
				return MpdResponse.Ok;
			}
			else if (error == MpdError.None && outputDetails != null)
			{
				return outputDetails + MpdResponse.Ok;
			}

			// For errors, return command name if bad arguments, else just braces
			if (error == MpdError.Arg)
			{
				cmd = cmdArray[0];
			}
			else
			{
				cmd = "{}";
			}

			// Program randomly halts unless I parameterize the braces, and I can't escape them.  wtf.
			return String.Format("{0} [{1}@{2}] {3} {4}", MpdResponse.Error, (int)error, listNum, cmd, errorDetails);
		}
	}
}
