using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using WaveBox.Model;
using WaveBox.Static;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox
{
	public static class Item
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static int? GenerateItemId(ItemType itemType)
		{
			int? itemId = null;
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				int affected = conn.ExecuteLogged("INSERT INTO Item (ItemType, Timestamp) VALUES (?, ?)", itemType, DateTime.UtcNow.ToUniversalUnixTimestamp());

				if (affected >= 1)
				{
					try
					{
						int rowId = conn.ExecuteScalar<int>("SELECT last_insert_rowid()");

						if (rowId != 0)
						{
							itemId = rowId;
						}
					}
					catch(Exception e)
					{
						logger.Error(e);
					}
					finally
					{
						conn.Close();
					}
				}
			}
			catch (Exception e)
			{
				logger.Error("GenerateItemId ERROR: ", e);
			}
			finally
			{
				conn.Close();
			}

			return itemId;
		}

		public static ItemType ItemTypeForItemId(int itemId)
		{
			int itemTypeId = 0;
			ISQLiteConnection conn = null;
			try
			{
				conn = Database.GetSqliteConnection();
				itemTypeId = conn.ExecuteScalar<int>("SELECT ItemType FROM Item WHERE ItemId = ?", itemId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				conn.Close();
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

		public static bool RecordStat(this IItem item, StatType statType, long timestamp)
		{
			return (object)item.ItemId == null ? false : Stat.RecordStat((int)item.ItemId, statType, timestamp);
		}
	}
}

