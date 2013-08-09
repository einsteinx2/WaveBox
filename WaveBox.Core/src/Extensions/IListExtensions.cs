using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using WaveBox.Core.Static;

namespace WaveBox.Core.Extensions
{
	public static class IListExtensions
	{
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
		/// InsertRange() for IList<T>, to insert a collection to another collection
		/// </summary>
		public static void InsertRange<T>(this IList<T> list, int index, IEnumerable<T> input)
		{
			foreach (T item in input)
			{
				list.Insert(index, item);
				index++;
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

		/// <summary>
		/// Convert a IList<string> to a single, non-quoted or quoted, CSV string
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
	}
}
