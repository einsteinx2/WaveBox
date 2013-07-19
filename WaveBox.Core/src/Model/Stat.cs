using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using Newtonsoft.Json;
using Ninject;
using TagLib;
using WaveBox.Core.Injection;
using WaveBox.Model;
using WaveBox.Static;

namespace WaveBox.Model
{
	public enum StatType
	{
		PLAYED = 0,
		Unknown = 2147483647 // Int32.MaxValue used for database compatibility
	}

	public class Stat
	{
		[PrimaryKey]
		public int? StatId { get; set; }

		public StatType? StatType { get; set; }

		public int? ItemId { get; set; }

		public long? Timestamp { get; set; }
	}
}

