using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlServerCe;
using pms.DataModel.Singletons;
using pms.DataModel.Model;

namespace pms.DataModel.Model
{
	public class CoverArt
	{
		public const string ART_PATH = "C:/tmp/pms/art/";
		public const string TMP_ART_PATH = "C:/tmp/pms/art/tmp/";

		/// <summary>
		/// Properties
		/// </summary>

		private int _artId;
		public int ArtId
		{
			get
			{
				return _artId;
			}

			set
			{
				_artId = value;
			}
		}

		private long _adlerHash;
		public long AdlerHash
		{
			get
			{
				return _adlerHash;
			}

			set
			{
				_adlerHash = value;
			}
		}

		public StreamReader artFile()
		{
			return new StreamReader(ART_PATH + AdlerHash);
		}

		/// <summary>
		/// Constructors
		/// </summary>

		public CoverArt()
		{
		}

		public CoverArt(int artId)
		{
			SqlCeConnection conn = null;
			SqlCeDataReader reader = null;

			try
			{
				conn = Database.getDbConnection();

				string query = string.Format("SELECT * FROM art WHERE art_id = {0}", artId);

				var q = new SqlCeCommand(query);
				q.Connection = conn;
				q.Prepare();
				reader = q.ExecuteReader();

				if (reader.Read())
				{
					_artId = reader.GetInt32(0);
					_adlerHash = reader.GetInt64(1);
				}
			}

			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}

			finally
			{
				Database.close(conn, reader);
			}
		}
	}
}
