using System;
using System.Text;
using System.Security.Cryptography;

namespace WaveBox
{
	public static class Utility
	{
		/// <summary>
		/// Determine if a string is meant to indicate true, return false if none detected
		/// </summary>
		public static bool IsTrue(string boolString)
		{
			try
			{
				// Null string -> false
				if (boolString == null)
				{
					return false;
				}

				// Lowercase and trim whitespace
				boolString = boolString.ToLower();
				boolString = boolString.Trim();

				if (boolString.Length > 0)
				{
					// t or 1 -> true
					if (boolString[0] == 't' || boolString[0] == '1')
					{
						return true;
					}
				}

				// Anything else, false
				return false;
			}
			catch
			{
				// Exception, false
				return false;
			}
		}

		/// <summary>
		/// Returns an integer representation of a month string
		/// </summary>
		public static int MonthForAbbreviation(string abb)
		{
			switch (abb.ToLower())
			{
				case "jan": return 1;
				case "feb": return 2;
				case "mar": return 3;
				case "apr": return 4;
				case "may": return 5;
				case "jun": return 6;
				case "jul": return 7;
				case "aug": return 8;
				case "sep": return 9;
				case "oct": return 10;
				case "nov": return 11;
				case "dec": return 12;
				default: return 0;
			}
		}

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

