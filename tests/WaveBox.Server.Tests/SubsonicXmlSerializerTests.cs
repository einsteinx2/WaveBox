using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WaveBox.Core.ApiResponse.Subsonic;
using WaveBox.Subsonic;
using Xunit;

namespace WaveBox.Server.Tests {
    public class SubsonicXmlSerializerTests {
        private static readonly XNamespace ns = "http://subsonic.org/restapi";

        private static XElement Root(SubsonicResponseBody body, bool indent = false) {
            return XDocument.Parse(SubsonicXmlSerializer.Serialize(body, indent)).Root;
        }

        [Fact]
        public void RootElementHasSubsonicNamespaceAndEnvelopeAttributes() {
            XElement root = Root(new SubsonicResponseBody());

            Assert.Equal(ns + "subsonic-response", root.Name);
            Assert.Equal("ok", (string)root.Attribute("status"));
            Assert.Equal("1.16.1", (string)root.Attribute("version"));
            Assert.Equal("WaveBox", (string)root.Attribute("type"));
            Assert.Equal("true", (string)root.Attribute("openSubsonic"));
        }

        [Fact]
        public void NullPropertiesAreOmitted() {
            XElement root = Root(new SubsonicResponseBody());

            // ServerVersion is null by default, and no payload was set
            Assert.Null(root.Attribute("serverVersion"));
            Assert.Empty(root.Elements());
        }

        [Fact]
        public void XmlDeclarationIsUtf8() {
            string xml = SubsonicXmlSerializer.Serialize(new SubsonicResponseBody(), false);

            Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?><subsonic-response", xml);
        }

        [Fact]
        public void IndentedOutputContainsNewlineAfterDeclaration() {
            string xml = SubsonicXmlSerializer.Serialize(new SubsonicResponseBody { License = new SubsonicLicense() }, true);

            Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n", xml);
        }

        [Fact]
        public void NestedObjectBecomesChildElementWithScalarAttributes() {
            SubsonicResponseBody body = new SubsonicResponseBody {
                License = new SubsonicLicense { Email = "ben@example.com" }
            };

            XElement license = Root(body).Element(ns + "license");

            Assert.NotNull(license);
            Assert.Equal("true", (string)license.Attribute("valid"));
            Assert.Equal("ben@example.com", (string)license.Attribute("email"));
            Assert.Null(license.Attribute("licenseExpires"));
        }

        [Fact]
        public void BoolRendersAsLowercaseTrueFalse() {
            SubsonicResponseBody body = new SubsonicResponseBody {
                License = new SubsonicLicense { Valid = false }
            };

            XElement license = Root(body).Element(ns + "license");

            Assert.Equal("false", (string)license.Attribute("valid"));
        }

        [Fact]
        public void ListRendersAsRepeatedChildElements() {
            SubsonicResponseBody body = new SubsonicResponseBody {
                Playlists = new SubsonicPlaylists {
                    Playlist = new List<SubsonicPlaylist> {
                        new SubsonicPlaylist { Id = "1", Name = "First", SongCount = 10, Duration = 600 },
                        new SubsonicPlaylist { Id = "2", Name = "Second", SongCount = 0, Duration = 0 }
                    }
                }
            };

            XElement playlists = Root(body).Element(ns + "playlists");
            List<XElement> entries = playlists.Elements(ns + "playlist").ToList();

            Assert.Equal(2, entries.Count);
            Assert.Equal("1", (string)entries[0].Attribute("id"));
            Assert.Equal("First", (string)entries[0].Attribute("name"));
            Assert.Equal("10", (string)entries[0].Attribute("songCount"));
            Assert.Equal("600", (string)entries[0].Attribute("duration"));
            Assert.Equal("2", (string)entries[1].Attribute("id"));
        }

        [Fact]
        public void AttributeNamesComeFromJsonPropertyName() {
            SubsonicResponseBody body = new SubsonicResponseBody {
                Error = new SubsonicError { Code = 70, Message = "Not found" }
            };

            XElement error = Root(body).Element(ns + "error");

            // [JsonPropertyName] gives lowercase names, numbers render InvariantCulture
            Assert.Equal("70", (string)error.Attribute("code"));
            Assert.Equal("Not found", (string)error.Attribute("message"));
        }

        [Fact]
        public void SubsonicXmlTextPropertyRendersAsElementText() {
            SubsonicResponseBody body = new SubsonicResponseBody {
                Genres = new SubsonicGenres {
                    Genre = new List<SubsonicGenre> {
                        new SubsonicGenre { SongCount = 5, AlbumCount = 2, Value = "Rock & Roll" }
                    }
                }
            };

            XElement genre = Root(body).Element(ns + "genres").Element(ns + "genre");

            Assert.Equal("5", (string)genre.Attribute("songCount"));
            Assert.Equal("2", (string)genre.Attribute("albumCount"));
            Assert.Equal("Rock & Roll", genre.Value);
            Assert.Null(genre.Attribute("value"));
        }

        [Fact]
        public void ScalarListItemsRenderAsTextContentElements() {
            SubsonicResponseBody body = new SubsonicResponseBody {
                OpenSubsonicExtensions = new List<SubsonicExtension> {
                    new SubsonicExtension { Name = "formPost", Versions = new List<int> { 1, 2 } }
                }
            };

            XElement extension = Root(body).Element(ns + "openSubsonicExtensions");
            List<XElement> versions = extension.Elements(ns + "versions").ToList();

            Assert.Equal("formPost", (string)extension.Attribute("name"));
            Assert.Equal(2, versions.Count);
            Assert.Equal("1", versions[0].Value);
            Assert.Equal("2", versions[1].Value);
        }

        [Fact]
        public void DeeplyNestedGraphSerializes() {
            SubsonicResponseBody body = new SubsonicResponseBody {
                SearchResult2 = new SubsonicSearchResult2 {
                    Artist = new List<SubsonicIndexArtist> {
                        new SubsonicIndexArtist { Id = "3", Name = "AC/DC" }
                    },
                    Song = new List<SubsonicChild> {
                        new SubsonicChild { Id = "42", Title = "Thunderstruck", IsDir = false, Duration = 292 }
                    }
                }
            };

            XElement result = Root(body).Element(ns + "searchResult2");
            XElement artist = result.Element(ns + "artist");
            XElement song = result.Element(ns + "song");

            Assert.Equal("AC/DC", (string)artist.Attribute("name"));
            Assert.Equal("42", (string)song.Attribute("id"));
            Assert.Equal("false", (string)song.Attribute("isDir"));
            Assert.Equal("292", (string)song.Attribute("duration"));
        }
    }
}
