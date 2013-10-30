using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace WaveBox.Core.Derived
{
	public class LinuxWebClient : TimedWebClient
	{
		// Set timeout using base constructor
		public LinuxWebClient(int timeout) : base(timeout)
		{
		}

		// Download a string using curl
		public string DownloadString(string uri)
		{
			string response = null;
			using (Process curl = new Process())
			{
				// Invoke curl, set timeout in seconds, set URI
				curl.StartInfo.FileName = "curl";
				curl.StartInfo.Arguments = "-m " + (this.timeout / 1000) + " '" + uri + "'";
				curl.StartInfo.UseShellExecute = false;
				curl.StartInfo.RedirectStandardOutput = true;
				curl.StartInfo.RedirectStandardError = true;

				// Run curl
				curl.Start();
				response = curl.StandardOutput.ReadToEnd();
				curl.WaitForExit();

				// Exit code 28 indicates timeout, so throw exception
				if (curl.ExitCode == 28)
				{
					throw new WebException("LinuxWebClient timed out");
				}
				// All other exit codes
				else if (curl.ExitCode != 0)
				{
					throw new Exception("LinuxWebClient: curl exited with: " + curl.ExitCode);
				}
			}

			return response;
		}

		// Download a file to a specified path using curl
		public void DownloadFile(string address, string path)
		{
			using (Process curl = new Process())
			{
				// Invoke curl, set timeout in seconds, set destination file, set URI
				curl.StartInfo.FileName = "curl";
				curl.StartInfo.Arguments = "-m " + (this.timeout / 1000) + " -o " + path + " '" + address + "'";
				curl.StartInfo.UseShellExecute = false;
				curl.StartInfo.RedirectStandardOutput = true;
				curl.StartInfo.RedirectStandardError = true;

				// Run curl
				curl.Start();
				curl.WaitForExit();

				// Exit code 28 indicates timeout, so throw exception
				if (curl.ExitCode == 28)
				{
					throw new WebException("LinuxWebClient timed out");
				}
				// All other exit codes
				else if (curl.ExitCode != 0)
				{
					throw new Exception("LinuxWebClient: curl exited with: " + curl.ExitCode);
				}
			}
		}
	}
}
