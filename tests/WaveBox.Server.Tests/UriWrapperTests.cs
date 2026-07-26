using System;
using System.Collections.Generic;
using WaveBox.ApiHandler;
using Xunit;

namespace WaveBox.Server.Tests {
    public class UriWrapperTests {
        [Fact]
        public void ParsesUriPartsAndShortcuts() {
            UriWrapper uri = new UriWrapper("/api/songs/6");

            Assert.Equal(new List<string> { "api", "songs", "6" }, uri.UriParts);
            Assert.Equal("api", uri.FirstPart);
            Assert.Equal("6", uri.LastPart);
            Assert.Equal("api", uri.UriPart(0));
            Assert.Equal("songs", uri.UriPart(1));
        }

        [Fact]
        public void UriPartOutOfRangeReturnsNull() {
            UriWrapper uri = new UriWrapper("/api/songs");

            Assert.Null(uri.UriPart(2));
            Assert.Null(uri.UriPart(100));
        }

        [Fact]
        public void ExtractsRestStyleTrailingNumericId() {
            Assert.Equal(6, new UriWrapper("/api/songs/6").Id);
            Assert.Null(new UriWrapper("/api/songs").Id);
        }

        [Fact]
        public void ParsesQueryParameters() {
            UriWrapper uri = new UriWrapper("/api/songs?limit=5&start=10");

            Assert.Equal("5", uri.Parameters["limit"]);
            Assert.Equal("10", uri.Parameters["start"]);
            // Parameters are stripped from UriString before splitting into parts
            Assert.Equal("songs", uri.LastPart);
        }

        [Fact]
        public void ParametersAreNotUrlDecoded() {
            // Pins legacy behavior: no URL-decoding is performed on parameter values
            UriWrapper uri = new UriWrapper("/api/search?q=hello%20world");

            Assert.Equal("hello%20world", uri.Parameters["q"]);
        }

        [Fact]
        public void EmptyQueryStringYieldsNoParameters() {
            UriWrapper uri = new UriWrapper("/api/songs?");

            Assert.Empty(uri.Parameters);
            Assert.Equal("songs", uri.LastPart);
        }

        [Fact]
        public void TrailingAmpersandIsIgnored() {
            UriWrapper uri = new UriWrapper("/api/songs?a=1&");

            Assert.Single(uri.Parameters);
            Assert.Equal("1", uri.Parameters["a"]);
        }

        [Fact]
        public void TokenWithoutEqualsIsSilentlyDropped() {
            // Pins legacy behavior: a bare token with no '=' never makes it into the dictionary
            Assert.Empty(new UriWrapper("/api/songs?flag").Parameters);

            UriWrapper uri = new UriWrapper("/api/songs?a=1&flag");
            Assert.Single(uri.Parameters);
            Assert.Equal("1", uri.Parameters["a"]);
        }

        [Fact]
        public void DuplicateKeysThrowAtConstruction() {
            // Pins legacy behavior: Dictionary.Add throws on the second occurrence of a key,
            // which surfaces as an ArgumentException from the constructor
            Assert.Throws<ArgumentException>(() => new UriWrapper("/api/songs?a=1&a=2"));
        }

        [Fact]
        public void ActionDefaultsToReadAndHonorsActionParameter() {
            Assert.Equal("read", new UriWrapper("/api/songs").Action);
            Assert.Equal("create", new UriWrapper("/api/songs?action=create").Action);
        }

        [Fact]
        public void HttpMethodOverridesActionParameter() {
            Assert.Equal("delete", new UriWrapper("/api/songs/6?action=create", "DELETE").Action);
            Assert.Equal("update", new UriWrapper("/api/songs/6?action=create", "PUT").Action);
            // Unrecognized methods leave the parameter action in place
            Assert.Equal("create", new UriWrapper("/api/songs/6?action=create", "GET").Action);
        }

        [Fact]
        public void ApiCallDetectionAndActionLowercasing() {
            UriWrapper uri = new UriWrapper("/api/SONGS");
            Assert.True(uri.IsApiCall);
            Assert.Equal("songs", uri.ApiAction);

            UriWrapper web = new UriWrapper("/web/index.html");
            Assert.False(web.IsApiCall);
            Assert.Null(web.ApiAction);
        }

        [Fact]
        public void EmptyUriHasNoPartsAndLastPartThrows() {
            UriWrapper uri = new UriWrapper("/");

            Assert.Empty(uri.UriParts);
            Assert.Null(uri.FirstPart);
            // Pins legacy behavior: LastPart indexes UriParts.Count - 1 == -1, which throws;
            // the constructor swallows this internally but the public property does not
            Assert.Throws<ArgumentOutOfRangeException>(() => uri.LastPart);
        }
    }
}
