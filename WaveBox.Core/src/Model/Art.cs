using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using TagLib;
using WaveBox.Core.Injection;
using WaveBox.Model;
using WaveBox.Static;
using WaveBox.Model.Repository;

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
			int? itemId = Injection.Kernel.Get<IItemRepository>().GenerateItemId(ItemType.Art);
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
