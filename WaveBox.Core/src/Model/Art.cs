using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using System.Text.Json.Serialization;
using WaveBox.Core.Model;
using WaveBox.Core.Static;
using WaveBox.Core.Model.Repository;

namespace WaveBox.Core.Model {
    public class Art {
        public static readonly string[] ValidExtensions = { "jpg", "jpeg", "png", "bmp", "gif" };
        private static readonly WaveBox.Core.Logging.ILog logger = WaveBox.Core.Logging.LogManager.GetLogger();

        /// <summary>
        /// Properties
        /// </summary>

        [JsonPropertyName("artId")]
        public int? ArtId { get; set; }

        [JsonPropertyName("md5Hash")]
        public string Md5Hash { get; set; }

        [JsonPropertyName("lastModified")]
        public long? LastModified { get; set; }

        [JsonPropertyName("fileSize")]
        public long? FileSize { get; set; }

        [JsonIgnore]
        public string FilePath { get; set; }

        public override string ToString() {
            return String.Format("[Art: ArtId={0}, FilePath={1}, LastModified={2}]", this.ArtId, this.FilePath, this.LastModified);
        }
    }
}
