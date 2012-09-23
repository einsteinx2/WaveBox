using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using System.Data.SQLite;
using System.IO;
using WaveBox.DataModel.Model;
using WaveBox.DataModel.Singletons;

namespace WaveBox
{
	public static class Item
	{
		public static int? GenerateItemId(ItemType itemType)
		{
			int? itemId = null;
			IDbConnection conn = null;
			IDataReader reader = null;
			try
			{
				conn = Database.GetDbConnection();
				IDbCommand q = Database.GetDbCommand("INSERT INTO item (item_type_id) VALUES (@itemType)", conn);
				q.AddNamedParam("@itemType", itemType);
				q.Prepare();
				int affected = (int)q.ExecuteNonQuery();

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
						Console.WriteLine("[ITEMID] GenerateItemId ERROR: " + e.ToString());
					}
					finally
					{
						Database.Close(null, reader2);
					}
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("[ITEMID] GenerateItemId ERROR: " + e.ToString());
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
			catch(Exception e)
			{
				Console.WriteLine("[DATABASE(1)] ERROR: " + e.ToString());
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

			// Return unknown, if we didn't return yet
			return ItemType.Unknown;
		}
	}
}

