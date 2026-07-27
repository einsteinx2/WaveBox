using System;

namespace WaveBox.Core.Static {
    public static class ThreadSafeRandom {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Environment.CurrentManagedThreadId))); }
        }
    }
}

