using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WaveBox.Subsonic {
    // Read-only view over the merged query string and form body of a Subsonic request.
    // Unlike the legacy UriWrapper, values are URL-decoded by ASP.NET and duplicate keys are
    // preserved (Subsonic clients send repeated id/songId/songIdToAdd parameters).
    public class SubsonicRequest {
        private readonly IQueryCollection query;
        private readonly IFormCollection form;

        public HttpContext Context { get; private set; }

        public SubsonicRequest(HttpContext context, IFormCollection form) {
            this.Context = context;
            this.query = context.Request.Query;
            this.form = form;
        }

        // First value for a key, or null when absent/empty
        public string Get(string key) {
            IList<string> all = this.GetAll(key);
            return all.Count > 0 ? all[0] : null;
        }

        // All non-empty values for a key, query string first, then form body
        public IList<string> GetAll(string key) {
            List<string> values = new List<string>();
            StringValues found;
            if (this.query != null && this.query.TryGetValue(key, out found)) {
                foreach (string value in found) {
                    if (!String.IsNullOrEmpty(value)) {
                        values.Add(value);
                    }
                }
            }
            if (this.form != null && this.form.TryGetValue(key, out found)) {
                foreach (string value in found) {
                    if (!String.IsNullOrEmpty(value)) {
                        values.Add(value);
                    }
                }
            }
            return values;
        }

        public int? GetInt(string key) {
            int value;
            return Int32.TryParse(this.Get(key), out value) ? value : (int?)null;
        }

        public long? GetLong(string key) {
            long value;
            return Int64.TryParse(this.Get(key), out value) ? value : (long?)null;
        }

        public bool GetBool(string key, bool defaultValue) {
            string value = this.Get(key);
            if (value == null) {
                return defaultValue;
            }
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public IList<int> GetIntList(string key) {
            List<int> values = new List<int>();
            foreach (string value in this.GetAll(key)) {
                int parsed;
                if (Int32.TryParse(value, out parsed)) {
                    values.Add(parsed);
                }
            }
            return values;
        }

        // Subsonic client name (c parameter)
        public string ClientName { get { return this.Get("c") ?? "Subsonic"; } }
    }
}
