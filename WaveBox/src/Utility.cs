using System;
using System.Text;
using System.Security.Cryptography;

namespace WaveBox
{
	public static class Utility
	{
		/// <summary>
		/// Generates a random string, for use in session creation
		/// </summary>
		public static string RandomString(int size)
		{
			Random rng = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()";

			char[] buffer = new char[size];
			for (int i = 0; i < size; i++)
			{
				buffer[i] = chars[rng.Next(chars.Length)];
			}
			return new string(buffer);
		}

		/// <summary>
		/// Generates a SHA1 sum of a given string
		/// </summary>
		public static string SHA1(string sumthis)
		{
			if (sumthis == "" || sumthis == null)
			{
				return "";
			}

			SHA1CryptoServiceProvider provider = new SHA1CryptoServiceProvider();
			return BitConverter.ToString(provider.ComputeHash(Encoding.ASCII.GetBytes(sumthis))).Replace("-", "");
		}

		/// <summary>
		/// Converts DateTime object to its UNIX timestamp equivalent
		/// </summary>
		public static int UnixTime(DateTime dt)
		{
			return Convert.ToInt32(Math.Round((dt - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds));
		}
	}
}

