using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using WaveBox.Static;
using WaveBox.Model;
using System.Security.Cryptography;
using TagLib;
using Newtonsoft.Json;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using WaveBox.Core.Injected;

namespace WaveBox.Model
{
	public class Art
	{
		public static readonly string[] ValidExtensions = { "jpg", "jpeg", "png", "bmp", "gif" };
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Properties
		/// </summary>

		[JsonProperty("artId")]
		public int? ArtId { get; set; }

		[JsonProperty("md5Hash")]
		public string Md5Hash { get; set; }

		[JsonProperty("lastModified")]
		public long? LastModified { get; set; }

		[JsonProperty("fileSize")]
		public long? FileSize { get; set; }

		[JsonIgnore]
		public string FilePath { get; set; }

		/// <summary>
		/// Constructors
		/// </summary>

		public Art()
		{

		}

		public void InsertArt()
		{
			int? itemId = Item.GenerateItemId(ItemType.Art);
			if (itemId == null)
			{
				return;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				ArtId = itemId;
				int affected = conn.InsertLogged(this);

				if (affected == 0)
				{
					ArtId = null;
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}
		}

		public static int? ItemIdForArtId(int? artId)
		{
			if ((object)artId == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int itemId = conn.ExecuteScalar<int>("SELECT ItemId FROM ArtItem WHERE ArtId = ?", artId);
				return itemId == 0 ? (int?)null : itemId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return null;
		}

		public static int? ArtIdForItemId(int? itemId)
		{
			if ((object)itemId == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int artId = conn.ExecuteScalar<int>("SELECT ArtId FROM ArtItem WHERE ItemId = ?", itemId);
				return artId == 0 ? (int?)null : artId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return null;
		}

		public static int? ArtIdForMd5(string hash)
		{
			if ((object)hash == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int artId = conn.ExecuteScalar<int>("SELECT ArtId FROM Art WHERE Md5Hash = ?", hash);
				return artId == 0 ? (int?)null : artId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return null;
		}

		public static bool UpdateArtItemRelationship(int? artId, int? itemId, bool replace)
		{
			if (artId == null || itemId == null)
			{
				return false;
			}

			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				// insert the song into the database
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				string type = replace ? "REPLACE" : "INSERT OR IGNORE";
				int affected = conn.ExecuteLogged(type + " INTO ArtItem (ArtId, ItemId) VALUES (?, ?)", artId, itemId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return success;
		}

		public static bool RemoveArtRelationshipForItemId(int? itemId)
		{
			if ((object)itemId == null)
			{
				return false;
			}

			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM ArtItem WHERE ItemId = ?", itemId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return success;
		}

		public static bool UpdateItemsToNewArtId(int? oldArtId, int? newArtId)
		{
			if ((object)oldArtId == null || (object)newArtId == null)
			{
				return false;
			}

			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
				int affected = conn.ExecuteLogged("UPDATE ArtItem SET ArtId = ? WHERE ArtId = ?", newArtId, oldArtId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
			}

			return success;
		}

		public class Factory
		{
			public Art CreateArt(int artId)
			{
				ISQLiteConnection conn = null;
				try
				{
					conn = Injection.Kernel.Get<IDatabase>().GetSqliteConnection();
					var result = conn.DeferredQuery<Art>("SELECT * FROM Art WHERE ArtId = ?", artId);

					foreach (Art a in result)
					{
						return a;
					}
				}
				catch (Exception e)
				{
					logger.Error(e);
				}
				finally
				{
					Injection.Kernel.Get<IDatabase>().CloseSqliteConnection(conn);
				}

				return new Art();
			}
		}
	}
}
