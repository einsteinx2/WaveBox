using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using WaveBox.Subsonic;
using Xunit;

namespace WaveBox.Server.Tests {
    public class SubsonicRequestTests {
        private static readonly string[] formSongIds = new string[] { "2", "3" };

        private static SubsonicRequest Request(string queryString, Dictionary<string, StringValues> formValues = null) {
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString(queryString);
            IFormCollection form = formValues == null ? null : new FormCollection(formValues);
            return new SubsonicRequest(context, form);
        }

        [Fact]
        public void GetReturnsFirstValueOrNull() {
            SubsonicRequest req = Request("?id=1&id=2&name=foo");

            Assert.Equal("1", req.Get("id"));
            Assert.Equal("foo", req.Get("name"));
            Assert.Null(req.Get("missing"));
        }

        [Fact]
        public void GetAllPreservesDuplicateKeys() {
            SubsonicRequest req = Request("?id=1&id=2&id=3");

            Assert.Equal(new List<string> { "1", "2", "3" }, req.GetAll("id"));
        }

        [Fact]
        public void GetAllMergesQueryThenForm() {
            SubsonicRequest req = Request("?songId=1", new Dictionary<string, StringValues> {
                { "songId", new StringValues(formSongIds) }
            });

            Assert.Equal(new List<string> { "1", "2", "3" }, req.GetAll("songId"));
            // Query string wins for single-value Get
            Assert.Equal("1", req.Get("songId"));
        }

        [Fact]
        public void EmptyValuesAreFilteredOut() {
            SubsonicRequest req = Request("?id=&c=DSub");

            Assert.Empty(req.GetAll("id"));
            Assert.Null(req.Get("id"));
        }

        [Fact]
        public void GetIntAndGetLongParseOrReturnNull() {
            SubsonicRequest req = Request("?size=500&big=9999999999&bad=abc");

            Assert.Equal(500, req.GetInt("size"));
            Assert.Null(req.GetInt("bad"));
            Assert.Null(req.GetInt("missing"));
            Assert.Equal(9999999999L, req.GetLong("big"));
            // Out of int range parses as long but not as int
            Assert.Null(req.GetInt("big"));
        }

        [Fact]
        public void GetBoolAcceptsOneAndTrueCaseInsensitive() {
            SubsonicRequest req = Request("?a=1&b=true&c=TRUE&d=0&e=yes");

            Assert.True(req.GetBool("a", false));
            Assert.True(req.GetBool("b", false));
            Assert.True(req.GetBool("c", false));
            Assert.False(req.GetBool("d", true));
            Assert.False(req.GetBool("e", false));
            // Absent key falls back to the default
            Assert.True(req.GetBool("missing", true));
            Assert.False(req.GetBool("missing", false));
        }

        [Fact]
        public void GetIntListSkipsUnparseableValues() {
            SubsonicRequest req = Request("?id=1&id=abc&id=3");

            Assert.Equal(new List<int> { 1, 3 }, req.GetIntList("id"));
            Assert.Empty(req.GetIntList("missing"));
        }

        [Fact]
        public void ClientNameDefaultsToSubsonic() {
            Assert.Equal("DSub", Request("?c=DSub").ClientName);
            Assert.Equal("Subsonic", Request("?u=ben").ClientName);
        }

        [Fact]
        public void ValuesAreUrlDecodedByAspNet() {
            SubsonicRequest req = Request("?query=hello%20world%26more");

            Assert.Equal("hello world&more", req.Get("query"));
        }
    }
}
