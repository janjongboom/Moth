using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Moth.Core.Execution;
using Moth.Core.Helpers;
using Moth.Core.Providers;

namespace Moth.Core.Modules.Css
{
    public class CssExecutor : IExecutor 
    {
        private static readonly MurmurHash2UInt32Hack HashingInstance;
        private static readonly IOutputCacheProvider Provider;

        static CssExecutor()
        {
            HashingInstance = new MurmurHash2UInt32Hack();
            Provider = MothAction.CacheProvider;
        }

        // <% Moth.RenderJavascript(); %>
        private static Regex _pattern = new Regex(@"<%\s*Moth\.RenderCss\(\);\s*%>", RegexOptions.Compiled);

        public string Replace(HttpContextBase httpContext, ControllerContext controllerContext, string input)
        {
            if (!_pattern.Match(input).Success) return input;

            return _pattern.Replace(input, GetContentFromHttpContext(httpContext, controllerContext));
        }

        internal string GetContentFromHttpContext(HttpContextBase httpContext, ControllerContext controllerContext)
        {
            var urlHelper = new UrlHelper(controllerContext.RequestContext);

            StringBuilder sb = new StringBuilder();
            foreach (var ses in (from s in MothScriptHelper.Stylesheets.SelectMany(i => i.Items)
                                 group s by s.Category into g
                                 select g.Select(x => x.Filename)))
            {
                var stylesheets = ses.ToList();

                var key = "scripthelper.rendercss." + string.Join("|", stylesheets.ToArray());

                sb.AppendLine(Provider.GetFromCache(key, () =>
                {
                    // hashcode bepalen
                    StringBuilder stylo = MothScriptHelper.GetFileContent(stylesheets, httpContext);

                    var outputFile = Encoding.UTF8.GetBytes(stylo.ToString());
                    var hashcode = HashingInstance.Hash(outputFile);

                    string url = urlHelper.Content("~/resources/css/");
                    return string.Format("<link rel=\"stylesheet\" type=\"text/css\" media=\"all\" href=\"{2}?keys={0}&amp;{1}\" />", string.Join("|", stylesheets.ToArray()), hashcode, url);
                }, Provider.CacheDurations.ExternalScript));
            }

            return sb.ToString();
        }

        public AllowCachingEnum AllowCaching
        {
            get { return AllowCachingEnum.Yes; }
        }
    }
}
