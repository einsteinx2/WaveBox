using System;
using System.Collections.Generic;
using System.Linq;
using WaveBox.Core;
using WaveBox.Core.ApiResponse;
using WaveBox.Core.Model;
using Xunit;

namespace WaveBox.Core.Tests {
    // WaveBoxJsonContext is the source-generated System.Text.Json context required for NativeAOT.
    // Any response DTO missing from its [JsonSerializable] list fails at runtime, so assert that
    // the context can actually produce type info for every response class.
    public class WaveBoxJsonContextTests {
        [Fact]
        public void EveryApiResponseClassHasTypeInfo() {
            // All concrete classes in the WaveBox.Core.ApiResponse namespace proper (the Subsonic
            // sub-namespace uses its own registry and SubsonicJsonContext, not this context)
            List<Type> responseTypes = typeof(WaveBoxJsonContext).Assembly.GetTypes()
                .Where(t => t.Namespace == "WaveBox.Core.ApiResponse"
                    && t.IsClass
                    && t.IsPublic
                    && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();

            // Guard against a vacuous pass if the reflection filter ever breaks
            Assert.True(responseTypes.Count >= 20, "Expected at least 20 API response classes, found " + responseTypes.Count);

            List<Type> missing = responseTypes
                .Where(t => WaveBoxJsonContext.Default.GetTypeInfo(t) == null)
                .ToList();

            Assert.True(missing.Count == 0,
                "API response types missing from WaveBoxJsonContext's [JsonSerializable] list "
                + "(they will fail to serialize under NativeAOT): "
                + string.Join(", ", missing.Select(t => t.Name)));
        }

        [Fact]
        public void ModelsEmbeddedInResponsesHaveTypeInfo() {
            // Spot-check the model and collection shapes that response DTOs embed
            Type[] embedded = new Type[] {
                typeof(Album),
                typeof(Artist),
                typeof(Folder),
                typeof(Genre),
                typeof(Playlist),
                typeof(Session),
                typeof(Song),
                typeof(User),
                typeof(Video),
                typeof(PairList<string, int>),
                typeof(IList<IItem>),
                typeof(IList<IMediaItem>),
                typeof(IDictionary<string, object>),
            };

            foreach (Type type in embedded) {
                Assert.True(WaveBoxJsonContext.Default.GetTypeInfo(type) != null,
                    type.Name + " has no type info in WaveBoxJsonContext");
            }
        }

        [Fact]
        public void PrimitiveRuntimeTypesForObjectDictionariesHaveTypeInfo() {
            // StatusResponse/StatsResponse put runtime-typed values into object-valued
            // dictionaries; each runtime type used must be registered explicitly
            Type[] primitives = new Type[] {
                typeof(bool), typeof(int), typeof(long), typeof(float), typeof(double),
                typeof(string), typeof(List<string>), typeof(IList<string>),
            };

            foreach (Type type in primitives) {
                Assert.True(WaveBoxJsonContext.Default.GetTypeInfo(type) != null,
                    type.Name + " has no type info in WaveBoxJsonContext");
            }
        }
    }
}
