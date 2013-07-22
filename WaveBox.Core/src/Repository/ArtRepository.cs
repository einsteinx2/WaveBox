using System;
using WaveBox.Core.Injection;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Model.Repository
{
	public class ArtRepository : IArtRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public ArtRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public Art ArtForId(int artId)
		{
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
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
				database.CloseSqliteConnection(conn);
			}

			return new Art();
		}

		public int? ItemIdForArtId(int? artId)
		{
			if ((object)artId == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				int itemId = conn.ExecuteScalar<int>("SELECT ItemId FROM ArtItem WHERE ArtId = ?", artId);
				return itemId == 0 ? (int?)null : itemId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return null;
		}

		public int? ArtIdForItemId(int? itemId)
		{
			if ((object)itemId == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				int artId = conn.ExecuteScalar<int>("SELECT ArtId FROM ArtItem WHERE ItemId = ?", itemId);
				return artId == 0 ? (int?)null : artId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return null;
		}

		public int? ArtIdForMd5(string hash)
		{
			if ((object)hash == null)
			{
				return null;
			}

			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				int artId = conn.ExecuteScalar<int>("SELECT ArtId FROM Art WHERE Md5Hash = ?", hash);
				return artId == 0 ? (int?)null : artId;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return null;
		}

		public bool UpdateArtItemRelationship(int? artId, int? itemId, bool replace)
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
				conn = database.GetSqliteConnection();
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
				database.CloseSqliteConnection(conn);
			}

			return success;
		}

		public bool RemoveArtRelationshipForItemId(int? itemId)
		{
			if ((object)itemId == null)
			{
				return false;
			}

			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("DELETE FROM ArtItem WHERE ItemId = ?", itemId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return success;
		}

		public bool UpdateItemsToNewArtId(int? oldArtId, int? newArtId)
		{
			if ((object)oldArtId == null || (object)newArtId == null)
			{
				return false;
			}

			bool success = false;
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("UPDATE ArtItem SET ArtId = ? WHERE ArtId = ?", newArtId, oldArtId);

				success = affected > 0;
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return success;
		}
	}
}

