using System;

namespace WaveBox.Core.Extensions
{
	public static class DateTimeExtensions
	{
		/// <summary>
		/// Creates universal DateTime object from an input UNIX timestamp
		/// </summary>
		public static DateTime ToDateTime(this long unixTime)
		{
			return new DateTime(1970, 1, 1).AddSeconds(unixTime).ToUniversalTime();
		}

		/// <summary>
		/// Convert a DateTime object to a HTTP ETag string
		/// <summary>
		public static string ToETag(this DateTime dateTime)
		{
			return dateTime.ToRFC1123().SHA1();
		}

		/// <summary>
		/// Creates a local UNIX timestamp from a DateTime object
		/// </summary>
		public static long ToLocalUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1).ToUniversalTime()).TotalSeconds;
		}

		/// <summary>
		/// Convert a DateTime object to a RFC1123 (HTTP Last-Modified) string
		/// <summary>
		public static string ToRFC1123(this DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss") + " GMT";
		}

		/// <summary>
		/// Creates a GMT UNIX timestamp from a DateTime object
		/// </summary>
		public static long ToUniversalUnixTimestamp(this DateTime dateTime)
		{
			return (long)(dateTime.ToLocalTime() - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
		}
	}
}
