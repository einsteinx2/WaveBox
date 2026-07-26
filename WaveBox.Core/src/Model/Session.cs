using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;
using WaveBox;
using WaveBox.Core.Extensions;
using WaveBox.Core.Model;
using WaveBox.Core.Model.Repository;
using WaveBox.Core.Static;

namespace WaveBox.Core.Model {
    public class Session {
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        [JsonPropertyName("rowId"), IgnoreWrite]
        public int RowId { get; set; }

        [JsonIgnore]
        public string SessionId { get; set; }

        [JsonPropertyName("userId")]
        public int? UserId { get; set; }

        [JsonPropertyName("clientName")]
        public string ClientName { get; set; }

        [JsonPropertyName("createTime")]
        public long? CreateTime { get; set; }

        [JsonPropertyName("updateTime")]
        public long? UpdateTime { get; set; }

        public bool Update() {
            this.UpdateTime = DateTime.UtcNow.ToUnixTime();

            return Injection.Get<ISessionRepository>().UpdateSession(this);
        }

        // Remove this session by its row ID
        public bool Delete() {
            return Injection.Get<ISessionRepository>().DeleteSessionForRowId(this.RowId);
        }

        public override string ToString() {
            return String.Format("[Session: RowId={0}, SessionId={1}, UserId={2}]", this.RowId, this.SessionId, this.UserId);
        }
    }
}
