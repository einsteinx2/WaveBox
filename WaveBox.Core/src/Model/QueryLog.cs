using System;
using Cirrious.MvvmCross.Plugins.Sqlite;

namespace WaveBox.Core.Model {
    public class QueryLog {
        [PrimaryKey]
        public int? QueryId { get; set; }

        public string QueryString { get; set; }

        public string ValuesString { get; set; }

        public QueryLog() {
        }

        public override string ToString() {
            return String.Format("[QueryLog: QueryId={0}, QueryString={1}, ValuesString={2}]", this.QueryId, this.QueryString, this.ValuesString);
        }
    }
}
