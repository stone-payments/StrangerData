using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace StrangerData.Utils
{
    public static class MemoryCache
    {
        private static IDictionary<Tuple<string, string>, object> _cache;

        private static IDictionary<Tuple<string, string>, object> Cache
        {
            get
            {
                if (_cache == null)
                    _cache = new ConcurrentDictionary<Tuple<string, string>, object>();
                return _cache;
            }
        }

        public static T TryGetFromCache<T>(string primaryKey, string secondaryKey, Func<object> getValue)
            where T : class
        {
            Tuple<string, string> compositeKey = new Tuple<string, string>(primaryKey, secondaryKey);
            
            if (!Cache.ContainsKey(compositeKey))
            {
                Cache[compositeKey] = getValue();
            }

            return Cache[compositeKey] as T;
        }
    }
}
