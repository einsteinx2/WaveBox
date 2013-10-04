using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using WaveBox.Core.Model;

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

		public static PairList<string, int> SectionPositionsFromSortedList(IList<IGroupingItem> sortedList)
		{
			if (sortedList == null)
				return new PairList<string, int>();

			PairList<string, int> positions = new PairList<string, int>();

			string lastGroupingLetter = null;
			int lastIndex = 0;
			for (int i = 0; i < sortedList.Count; i++)
			{
				string groupingName = sortedList[i].GroupingName;
				if (groupingName == null || groupingName.Length == 0)
					continue;

				char firstLetter = Char.ToUpper(groupingName[0]);
				if (lastGroupingLetter == null || firstLetter != lastGroupingLetter[0])
				{
					lastGroupingLetter = firstLetter.ToString();
					lastIndex = i;
					positions.Add(lastGroupingLetter, lastIndex);
				}
			}

			return positions;
		}
	}
}
