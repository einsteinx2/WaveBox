using System;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Core.Model
{
	public class QueryLog
	{
		[PrimaryKey]
		public int? QueryId { get; set; }

		public string QueryString { get; set; }

		public string ValuesString { get; set; }

		public QueryLog()
		{
		}
	}
}
