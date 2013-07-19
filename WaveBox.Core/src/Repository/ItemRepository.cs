using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Ninject;
using WaveBox.Core.Extensions;
using WaveBox.Core.Injection;
using WaveBox.Model;
using WaveBox.Static;

namespace WaveBox.Model.Repository
{
	public class ItemRepository : IItemRepository
	{
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IDatabase database;

		public ItemRepository(IDatabase database)
		{
			if (database == null)
				throw new ArgumentNullException("database");

			this.database = database;
		}

		public int? GenerateItemId(ItemType itemType)
		{
			int? itemId = null;
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
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
				}
			}
			catch (Exception e)
			{
				logger.Error("GenerateItemId ERROR: ", e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return itemId;
		}

		public ItemType ItemTypeForItemId(int itemId)
		{
			int itemTypeId = 0;
			ISQLiteConnection conn = null;
			try
			{
				conn = database.GetSqliteConnection();
				itemTypeId = conn.ExecuteScalar<int>("SELECT ItemType FROM Item WHERE ItemId = ?", itemId);
			}
			catch (Exception e)
			{
				logger.Error(e);
			}
			finally
			{
				database.CloseSqliteConnection(conn);
			}

			return ItemTypeExtensions.ItemTypeForId(itemTypeId);
		}

		public ItemType ItemTypeForFilePath(string filePath)
		{
			// Make sure it's not null
			if (filePath == null)
			{
				return ItemType.Unknown;
			}

			// Get the extension
			string extension = "";
			var split = filePath.ToLower().Split('.');
			if (split.Length > 0)
			{
				extension = split[split.Length - 1];
			}

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
