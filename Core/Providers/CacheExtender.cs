using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moth.Core.Providers
{
    internal static class CacheExtender
    {
        public static T GetFromCache<T>(this IOutputCacheProvider provider, string key, Func<T> fetchAction, TimeSpan duration)
            where T: class
        {
            var obj = provider.Get<T>(key);

            // hier moeten we nog wat op verzinnen want dit gaat natuurlijk mis ooit
            if(obj == null)
            {
                obj = fetchAction();
                provider.Store(key, obj, duration);
            }

            return obj;
        }
    }
}
