using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace WaveBox.Core.Static
{
	public static class Utility
	{
		/// <summary>
		/// Generates a random string, for use in session creation
		/// </summary>
		static private Random rng = new Random();
		public static string RandomString(int size)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%^&*()";

			char[] buffer = new char[size];
			for (int i = 0; i < size; i++)
			{
				buffer[i] = chars[rng.Next(chars.Length)];
			}
			return new string(buffer);
		}

		/*
		 * Class Extensions
		 */

		/// <summary>
		/// Determine if a string is meant to indicate true, return false if none detected
		/// </summary>
		public static bool IsTrue(this string boolString)
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
		/// Generates a MD5 sum of a given string
		/// <summary>
		public static string MD5(this string sumthis)
		{
			if (sumthis == "" || sumthis == null)
			{
				return "";
			}

			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
			return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(sumthis)), 0);
		}

		/// <summary>
		/// Returns an integer representation of a month string
		/// </summary>
		public static int MonthForAbbreviation(this string abb)
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
		/// Generates a SHA1 sum of a given string
		/// </summary>
		public static string SHA1(this string sumthis)
		{
			if (sumthis == "" || sumthis == null)
			{
				return "";
			}

			SHA1CryptoServiceProvider provider = new SHA1CryptoServiceProvider();
			return BitConverter.ToString(provider.ComputeHash(Encoding.ASCII.GetBytes(sumthis))).Replace("-", "");
		}

		/// <summary>
		/// Convert a List to a quoted CSV string
		/// </summary>
		public static string ToCSV(this IList<string> list, bool quoted = false)
		{
			string buffer = "";

			// If list is empty, return empty list
			if (list.Count == 0)
			{
				if (quoted)
				{
					return "\"\"";
				}

				return "";
			}

			foreach (string s in list)
			{
				if (quoted)
				{
					buffer += "\"" + s + "\", ";
				}
				else
				{
					buffer += s + ", ";
				}
			}

			return buffer.Trim(new char[] {' ', ','});
		}

		/// <summary>
		/// Remove UTF8 byte order mark from a string
		/// </summary>
		public static string RemoveByteOrderMark(this string s)
		{
			string byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

			if (s.StartsWith(byteOrderMarkUtf8))
			{
				return s.Remove(0, byteOrderMarkUtf8.Length);
			}

			return s;
		}

		/// <summary>
		/// AddRange() for IList<T>, to add a collection to another collection
		/// </summary>
		public static void AddRange<T>(this IList<T> list, IEnumerable<T> input)
		{
			foreach (T item in input)
			{
				list.Add(item);
			}
		}

		/// <summary>
		/// Randomly shuffle an IList<T> list
		/// </summary>
		public static void Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}
}

