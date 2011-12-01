using System;

namespace Moth.Core
{
    public interface IOutputCacheProvider
    {
        T Get<T> (string key) where T: class;
        void Store (string key, object o, TimeSpan duration);

        IOutputCacheDurations CacheDurations { get; }
        IOutputCacheRestrictions Enable { get; }
        Func<string, string> PathFixup { get; }

    }

    public interface IOutputCacheDurations
    {
        TimeSpan PageOutput { get; }
        TimeSpan InlineScript { get; }
        TimeSpan ExternalScript { get; }
        TimeSpan DataUri { get; }
    }

    public interface IOutputCacheRestrictions
    {
        /// <summary>
        /// Overrides all page output settings
        /// </summary>
        bool PageOutput { get;  }

        /// <summary>
        /// Minifies scripts and css files
        /// </summary>
        bool ScriptMinification { get; }

        /// <summary>
        /// Requires write and execute permissions in the output directory of the app
        /// </summary>
        bool CssTidy { get; }

        /// <summary>
        /// Enables in-file extensions to CSS, like 'moth-sprite'
        /// </summary>
        bool CssPreprocessing { get; }
    }

    public class OutputCacheDurations : IOutputCacheDurations
    {
        public TimeSpan PageOutput { get; set; }
        public TimeSpan InlineScript { get; set; }
        public TimeSpan ExternalScript { get; set; }
        public TimeSpan DataUri { get; set; }
    }

    public class OutputCacheRestrictions : IOutputCacheRestrictions
    {
        /// <summary>
        /// Overrides all page output settings
        /// </summary>
        public bool PageOutput { get; set; }

        /// <summary>
        /// Minifies scripts and css files
        /// </summary>
        public bool ScriptMinification { get; set; }

        /// <summary>
        /// Requires write and execute permissions in the output directory of the app
        /// </summary>
        public bool CssTidy { get; set; }

        /// <summary>
        /// Enables in-file extensions to CSS, like 'moth-sprite'
        /// </summary>
        public bool CssPreprocessing { get; set; }
    }
}
