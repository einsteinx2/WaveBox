using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WaveBox.Core.ApiResponse.Subsonic;
using Xunit;

namespace WaveBox.Core.Tests {
    // The Subsonic XML serializer walks DTO properties via reflection, so under NativeAOT every
    // DTO reachable from the SubsonicResponse envelope must be rooted in SubsonicDtoRegistry.
    // This test walks the public property graph from the envelope so a newly added DTO that is
    // not registered fails here instead of silently losing properties on the AOT binary.
    public class SubsonicDtoRegistryTests {
        private const string SubsonicNamespace = "WaveBox.Core.ApiResponse.Subsonic";

        private static HashSet<Type> ReachableDtoTypes() {
            HashSet<Type> reached = new HashSet<Type>();
            Queue<Type> queue = new Queue<Type>();
            queue.Enqueue(typeof(SubsonicResponse));

            while (queue.Count > 0) {
                Type type = queue.Dequeue();
                if (!reached.Add(type)) {
                    continue;
                }

                // Base classes need their metadata preserved too
                if (type.BaseType != null && IsSubsonicDto(type.BaseType)) {
                    queue.Enqueue(type.BaseType);
                }

                foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                    foreach (Type candidate in ComponentTypes(prop.PropertyType)) {
                        if (IsSubsonicDto(candidate)) {
                            queue.Enqueue(candidate);
                        }
                    }
                }
            }

            return reached;
        }

        private static bool IsSubsonicDto(Type type) {
            return type.Namespace == SubsonicNamespace && type.IsClass;
        }

        // Unwraps arrays and generic containers (List<T>, IList<T>, Nullable<T>, ...) down to
        // their element/argument types
        private static IEnumerable<Type> ComponentTypes(Type propertyType) {
            if (propertyType.IsArray) {
                yield return propertyType.GetElementType();
            } else if (propertyType.IsGenericType) {
                foreach (Type arg in propertyType.GetGenericArguments()) {
                    yield return arg;
                }
            } else {
                yield return propertyType;
            }
        }

        [Fact]
        public void EveryDtoReachableFromTheResponseEnvelopeIsRegistered() {
            HashSet<Type> reached = ReachableDtoTypes();
            List<Type> missing = reached.Except(SubsonicDtoRegistry.RootedTypes).OrderBy(t => t.Name).ToList();

            Assert.True(missing.Count == 0,
                "Subsonic DTO types reachable from SubsonicResponse but not rooted in SubsonicDtoRegistry "
                + "(their XML rendering will silently lose properties under NativeAOT): "
                + string.Join(", ", missing.Select(t => t.Name)));
        }

        [Fact]
        public void EveryRegisteredDtoIsReachableFromTheResponseEnvelope() {
            // The inverse direction: a registered type nobody can reach any more is dead weight
            // and usually means a payload property was removed without updating the registry
            HashSet<Type> reached = ReachableDtoTypes();
            List<Type> unreachable = SubsonicDtoRegistry.RootedTypes.Except(reached).OrderBy(t => t.Name).ToList();

            Assert.True(unreachable.Count == 0,
                "Types rooted in SubsonicDtoRegistry but not reachable from SubsonicResponse: "
                + string.Join(", ", unreachable.Select(t => t.Name)));
        }

        [Fact]
        public void WalkerFindsTheDtoGraph() {
            // Guards the walker itself against vacuous passes
            HashSet<Type> reached = ReachableDtoTypes();
            Assert.Contains(typeof(SubsonicResponseBody), reached);
            Assert.Contains(typeof(SubsonicError), reached);
            Assert.True(reached.Count >= 30, "Expected at least 30 reachable Subsonic DTO types, found " + reached.Count);
        }

        [Fact]
        public void RegistryHasNoDuplicates() {
            IReadOnlyList<Type> rooted = SubsonicDtoRegistry.RootedTypes;
            Assert.Equal(rooted.Count, rooted.Distinct().Count());
        }
    }
}
