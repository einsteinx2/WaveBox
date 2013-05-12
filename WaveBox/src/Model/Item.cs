using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using System.Data.SQLite;
using System.IO;
using WaveBox.Model;
using WaveBox.Singletons;

namespace WaveBox
{
	public static class Item
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static int? GenerateItemId(ItemType itemType)
		{
			int? itemId = null;
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO item (item_type_id, time_stamp) VALUES (@itemType, @timestamp)", conn);
				q.AddNamedParam("@itemType", itemType);
				q.AddNamedParam("@timestamp", DateTime.UtcNow.ToUniversalUnixTimestamp());
				q.Prepare();
				int affected = (int)q.ExecuteNonQueryLogged();

				if (affected >= 1)
				{
					IDataReader reader2 = null;
					try
					{
						q.CommandText = "SELECT last_insert_rowid()";
						reader2 = q.ExecuteReader();

						if (reader2.Read())
						{
							itemId = reader2.GetInt32(0);
						}
					}
					catch(Exception e)
					{
						logger.Error(e);
					}
					finally
					{
						Database.Close(null, reader2);
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("GenerateItemId ERROR: ", e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return itemId;
		}

		public static ItemType ItemTypeForItemId(int itemId)
		{
			int itemTypeId = 0;
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("SELECT item_type_id FROM item WHERE item_id = @itemid", conn);
				q.AddNamedParam("@itemid", itemId);
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					itemTypeId = reader.GetInt32(0);
				}
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				Database.Close(conn, reader);
			}

			return ItemTypeExtensions.ItemTypeForId(itemTypeId);
		}

		public static ItemType ItemTypeForFilePath(string filePath)
		{
			// Make sure it's not null
			if (filePath == null)
			{
				return ItemType.Unknown;
			}

			// Get the extension
			string extension = Path.GetExtension(filePath).ToLower();

			// Compare to valid song extensions
			if (Song.ValidExtensions.Contains(extension))
			{
				return ItemType.Song;
			}
			else if (Video.ValidExtensions.Contains(extension))
			{
				return ItemType.Video;
			}
			else if (Art.ValidExtensions.Contains(extension))
			{
				return ItemType.Art;
			}

			// Return unknown, if we didn't return yet
			return ItemType.Unknown;
		}
	}
}

