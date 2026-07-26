using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WaveBox.Core;
using WaveBox.Core.Model;
using Xunit;

namespace WaveBox.Core.Tests {
    // The vendored sqlite-net ORM materializes models via reflection, so under NativeAOT every
    // ORM-mapped model must be rooted in ModelTypeRegistry.  This test discovers ORM-shaped model
    // types automatically so that adding a new model without registering it fails the build's
    // test run instead of failing at runtime on the AOT binary.
    public class ModelTypeRegistryTests {
        // Types in WaveBox.Core.Model that match the ORM shape but are deliberately NOT
        // registered, with the reason each is excluded:
        private static readonly Type[] KnownNonOrmTypes = new Type[] {
            // Transient in-memory now-playing entry; holds a live Timer and is only ever
            // serialized to JSON, never stored via the ORM
            typeof(NowPlaying),
            // Settings payload persisted as a JSON text blob (and wavebox.conf), not mapped to
            // a database table
            typeof(ServerSettingsData),
        };

        private static List<Type> DiscoverOrmShapedModelTypes() {
            // ORM shape: public non-abstract, non-generic class in WaveBox.Core.Model with a
            // public parameterless constructor and at least one public settable property —
            // exactly what sqlite-net needs to map a row
            return typeof(ModelTypeRegistry).Assembly.GetTypes()
                .Where(t => t.Namespace == "WaveBox.Core.Model"
                    && t.IsClass
                    && t.IsPublic
                    && !t.IsAbstract
                    && !t.IsGenericTypeDefinition
                    && t.GetConstructor(Type.EmptyTypes) != null
                    && t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Any(p => p.SetMethod != null && p.SetMethod.IsPublic))
                .Except(KnownNonOrmTypes)
                .OrderBy(t => t.Name)
                .ToList();
        }

        [Fact]
        public void EveryOrmShapedModelTypeIsRegistered() {
            List<Type> discovered = DiscoverOrmShapedModelTypes();
            List<Type> missing = discovered.Except(ModelTypeRegistry.RootedTypes).ToList();

            Assert.True(missing.Count == 0,
                "Model types not rooted in ModelTypeRegistry.EnsurePreserved (they will break under NativeAOT): "
                + string.Join(", ", missing.Select(t => t.Name))
                + ". Register them, or add them to KnownNonOrmTypes with a reason.");
        }

        [Fact]
        public void DiscoveryFindsTheCoreModels() {
            // Guards the discovery heuristic itself: if reflection filtering ever goes wrong and
            // discovers nothing, the coverage test above would pass vacuously
            List<Type> discovered = DiscoverOrmShapedModelTypes();
            Assert.Contains(typeof(Song), discovered);
            Assert.Contains(typeof(User), discovered);
            Assert.Contains(typeof(Playlist), discovered);
            Assert.True(discovered.Count >= 15, "Expected at least 15 ORM-shaped model types, found " + discovered.Count);
        }

        [Fact]
        public void RegistryHasNoDuplicates() {
            IReadOnlyList<Type> rooted = ModelTypeRegistry.RootedTypes;
            Assert.Equal(rooted.Count, rooted.Distinct().Count());
        }
    }
}
