using System;

namespace WaveBox.Core.Extensions
{
	public static class DateTimeExtensions
	{
		public static string ToRFC1123(this DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss") + " GMT";
		}

		/// <summary>
		/// Creates universal DateTime object from an input UNIX timestamp
		/// </summary>
		public static DateTime ToDateTimeFromUnixTimestamp(this long unixTime)
		{
			return new DateTime(1970, 1, 1).AddSeconds(unixTime).ToUniversalTime();
		}

		/// <summary>
		/// Creates a GMT UNIX timestamp from a DateTime object
		/// </summary>
		public static long ToUniversalUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToLocalTime() - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
		}

		/// <summary>
		/// Creates a local UNIX timestamp from a DateTime object
		/// </summary>
		public static long ToLocalUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1).ToUniversalTime()).TotalSeconds;
		}
	}
}

