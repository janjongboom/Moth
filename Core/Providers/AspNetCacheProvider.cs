using System;
using System.Web;
using System.Web.Caching;

namespace Moth.Core.Providers
{
    public class AspNetCacheProvider : IOutputCacheProvider
    {
        public AspNetCacheProvider()
        {
            CacheDurations = new OutputCacheDurations()
                {
                    PageOutput = new TimeSpan(0, 10, 0),
                    InlineScript = new TimeSpan(0, 10, 0),
                    ExternalScript = new TimeSpan(0, 10, 0),
                    DataUri = new TimeSpan(0, 20, 0)
                };

            Enable = new OutputCacheRestrictions()
                {
                    ScriptMinification = true,
                    PageOutput = true,
                    CssPreprocessing = true,
                    CssTidy = true
                };
            PathFixup = (file) =>
                            {
                                var filename = file;
                                if (!filename.StartsWith("~/")) filename = "~/" + filename.TrimStart('/');
                                return filename;
                            };
        }

        public virtual T Get<T> (string key)
            where T: class
        {
            return HttpContext.Current.Cache.Get(key) as T;
        }

        public virtual void Store (string key, object o, TimeSpan duration)
        {
            HttpContext.Current.Cache.Insert(key, o, null, DateTime.Now.Add(duration), Cache.NoSlidingExpiration);
        }

        public virtual IOutputCacheDurations CacheDurations { get; private set; }
        public virtual IOutputCacheRestrictions Enable { get; private set; }
        public virtual Func<string, string> PathFixup { get; private set; }

    }
}
