using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Moth.Core.Execution;
using Moth.Core.Helpers;
using Moth.Core.Providers;

namespace Moth.Core
{
    /// <summary>
    /// Attaches to the page-rendering process, and allows for better control after the rendering of a page
    /// </summary>
    public class MothAction : ActionFilterAttribute
    {
        #region Dependencies
        // gotta replace these kind of things with StuctureMap one day
        private static readonly IAssemblySearcher AssemblySearcher = new AssemblySearcher();
        #endregion

        // default cache provider is asp.net met 10 minuten
        internal static IOutputCacheProvider CacheProvider { get; private set; }

        private static List<IExecutor> _executors;

        static MothAction()
        {
            CacheProvider = new AspNetCacheProvider();
            _executors = AssemblySearcher.FindImplementations<IExecutor>().Select(t=>Activator.CreateInstance(t)).Cast<IExecutor>().ToList();
        }

        /// <summary>
        /// Initialize the MothAction (do this in global.asax) with a custom Provider
        /// </summary>
        /// <param name="provider"></param>
        public static void Initialize(IOutputCacheProvider provider)
        {
            CacheProvider = provider;
        }

        private static readonly MethodInfo SwitchWriterMethod = typeof(HttpResponse).GetMethod("SwitchWriter", BindingFlags.Instance | BindingFlags.NonPublic);
        private TextWriter _originalWriter;
        private string _cacheKey;

        /// <summary>
        /// Enables page outputcaching (including Donut Holes)
        /// </summary>
        public bool OutputCaching { get; set; }

        /// <summary>
        /// Disables Moth fully for this action
        /// </summary>
        public bool DisableMoth { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Trace.Write("OutputCaching " + OutputCaching);

            if (!(filterContext.RequestContext.HttpContext.Response.Output is HttpWriter) || DisableMoth)
            {
                // the output has already been switched; this is caused by partial actions that also have the attribute
                // we don't have to switch again
                return;
            }

            // output caching aan, en ook in config?);
            if (OutputCaching && CacheProvider.Enable.PageOutput)
            {
                // genereer key
                _cacheKey = ComputeCacheKey(filterContext);

                // is er al resultaat?
                var cacheResult = CacheProvider.Get<OutputCacheItem>(_cacheKey);

                // zoja, doe dan dat maar teruggeven
                if(cacheResult != null)
                {
                    string content = cacheResult.Content;

                    // execute all executors that don't allow for caching
                    foreach(var executor in _executors.Where(e=>e.AllowCaching == AllowCachingEnum.No))
                    {
                        content = executor.Replace(filterContext.HttpContext, filterContext.Controller.ControllerContext, content);
                    }

                    filterContext.Result = new ContentResult() { Content = content, ContentType = cacheResult.ContentType };
                    return;
                }
            }

            _originalWriter = (TextWriter)SwitchWriterMethod.Invoke(HttpContext.Current.Response,
                                                                      new object[]
                                                                              {
                                                                                  new HtmlTextWriter(new StringWriter())
                                                                              });

        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (_originalWriter == null || DisableMoth || !(filterContext.RequestContext.HttpContext.Response.Output is HtmlTextWriter))
            {
                return;
            }

            // switch de writer terug
            HtmlTextWriter cacheWriter = (HtmlTextWriter)SwitchWriterMethod.Invoke(HttpContext.Current.Response, new object[] { _originalWriter });
            var textWritten = cacheWriter.InnerWriter.ToString();

            // execute all executors that allow for caching
            foreach (var executor in _executors.Where(e => e.AllowCaching == AllowCachingEnum.Yes))
            {
                textWritten = executor.Replace(filterContext.HttpContext, filterContext.Controller.ControllerContext, textWritten);
            }

            // cachen voor de volgende keer?
            if (OutputCaching && CacheProvider.Enable.PageOutput && filterContext.Exception == null)
            {
                var result = new OutputCacheItem()
                    {
                        Content = textWritten,
                        ContentType = filterContext.Result is ContentResult ? ((ContentResult) filterContext.Result).ContentType : ""
                    };

                CacheProvider.Store(_cacheKey, result, CacheProvider.CacheDurations.PageOutput);
            }

            foreach (var executor in _executors.Where(e => e.AllowCaching == AllowCachingEnum.No))
            {
                textWritten = executor.Replace(filterContext.HttpContext, filterContext.Controller.ControllerContext, textWritten);
            }

            // output naar response stream
            filterContext.HttpContext.Response.Write(textWritten);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if(filterContext.Exception != null)
            {
                // direct terugswitchen om te zorgen dat we de output van de melding nog zien
                SwitchWriterMethod.Invoke(HttpContext.Current.Response, new object[] { _originalWriter });
            }
        }

        private static string ComputeCacheKey(ActionExecutingContext filterContext)
        {
            var items = new List<string>();

            items.Add(filterContext.ActionDescriptor.ControllerDescriptor.ControllerName);
            items.Add(filterContext.ActionDescriptor.ActionName);

            foreach (var param in filterContext.ActionParameters)
            {
                items.Add((param.Value ?? (object)0).GetHashCode().ToString());
            }

            return string.Join("|", items.ToArray());
        }
    }

    internal class OutputCacheItem
    {
        public string Content { get; set; }
        public string ContentType { get; set; }
    }
}