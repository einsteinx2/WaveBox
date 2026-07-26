using System.Runtime.CompilerServices;
using WaveBox.Core.Static;

namespace WaveBox.Core.Tests {
    internal static class TestPasswordWorkFactor {
        /// <summary>
        /// Drops bcrypt to its minimum work factor for the duration of the test run. The production
        /// value is tuned to take a noticeable fraction of a second per hash, and fixtures across
        /// this assembly create users constantly, so leaving it alone would dominate the run time.
        /// </summary>
        [ModuleInitializer]
        internal static void Reduce() {
            PasswordHasher.WorkFactor = 4;
        }
    }
}
