using System;
using System.Collections.Generic;

namespace WaveBox.Core.Model {
    // http://stackoverflow.com/a/8593419/299262
    public class PairList<TKey, TValue> : List<KeyValuePair<TKey, TValue>> {
        public void Add(TKey key, TValue value) {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }
    }
}

