using System;
using System.Text;
using WaveBox.Core.Model;

namespace WaveBox.Core.Extensions
{
	public static class FileTypeExtensions
	{
		/// <summary>
		/// Returns a FileType enumeration matching the input TagLib mime type
		/// </summary>
		public static FileType FileTypeForTagLibMimeType(this FileType ft, string mimeType)
		{
			// Try audio types
			switch (mimeType)
			{
				case "taglib/m4a":
				case "taglib/aac":
					return FileType.AAC;
				case "taglib/mp3":
					return FileType.MP3;
				case "taglib/mpc":
					return FileType.MPC;
				case "taglib/oggo":
					return FileType.OGG;
				case "taglib/wma":
					return FileType.WMA;
				case "taglib/flac":
					return FileType.FLAC;
				case "taglib/wv":
					return FileType.WV;
				case "taglib/ape":
					return FileType.APE;
				default:
					break;
			}

			// Then video types
			if (mimeType.Contains("taglib/mp4"))
			{
				return FileType.MP4;
			}
			else if (mimeType.Contains("taglib/mkv"))
			{
				return FileType.MKV;
			}
			else if (mimeType.Contains("taglib/avi"))
			{
				return FileType.AVI;
			}

			// Else, unknown!
			return FileType.Unknown;
		}

		/// <summary>
		/// Returns a mime type string matching a FileType enumeration
		/// </summary>
		public static string MimeType(this FileType ft)
		{
			// Need to verify these
			switch (ft)
			{
				case FileType.AAC: return "audio/mp4";
				case FileType.MP3: return "audio/mpeg";
				case FileType.MPC: return "audio/mpc";
				case FileType.OGG: return "audio/ogg";
				case FileType.WMA: return "audio/wma";
				case FileType.ALAC: return "audio/alac";
				case FileType.FLAC: return "audio/flac";
				case FileType.WV: return "audio/wv";
				case FileType.APE: return "audio/ape";
				case FileType.MP4: return "video/mp4";
				case FileType.MKV: return "video/mkv";
				case FileType.AVI: return "video/avi";
				default: return "text/plain";
			}
		}

		/// <summary>
		/// Returns a FileType enumeration matching the input ID
		/// </summary>
		public static FileType FileTypeForId(this FileType ft, int id)
		{
			foreach (FileType type in Enum.GetValues(typeof(FileType)))
			{
				if (id == (int)type)
				{
					return type;
				}
			}
			return FileType.Unknown;
		}
	}
}
