using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moth.Core.Execution;
using Moth.Core.Helpers;
using Moth.Core.Providers;

namespace Moth.Core.Modules.Scripts
{
    public class ScriptExecutor : IExecutor
    {
        private static readonly MurmurHash2UInt32Hack HashingInstance;
        private static readonly IOutputCacheProvider Provider;

        static ScriptExecutor()
        {
            HashingInstance = new MurmurHash2UInt32Hack();
            Provider = MothAction.CacheProvider;
        }

        // <% Moth.RenderJavascript(); %>
        private static Regex _pattern = new Regex(@"<%\s*Moth\.RenderJavascript\(\);\s*%>", RegexOptions.Compiled);

        public string Replace(HttpContextBase httpContext, ControllerContext controllerContext, string input)
        {
            if (!_pattern.Match(input).Success) return input;

            return _pattern.Replace(input, GetScriptsFromHttpContext(httpContext, controllerContext));
        }

        internal string GetScriptsFromHttpContext(HttpContextBase httpContext, ControllerContext controllerContext)
        {
            var urlHelper = new UrlHelper(new RequestContext(httpContext, controllerContext.RouteData));

            StringBuilder sb = new StringBuilder();
            foreach (var ses in (from s in MothScriptHelper.Scripts.SelectMany(i=>i.Items)
                                         group s by s.Category into g
                                         select g.Select(x=>x.Filename)))
            {
                var scripts = ses.ToList();

                //var scripts = MothScriptHelper.Scripts[cat].ToList();

                var key = "scripthelper.renderscripts." + string.Join("|", scripts.ToArray());

                sb.AppendLine(Provider.GetFromCache(key, () =>
                {
                    // hashcode bepalen
                    StringBuilder script = MothScriptHelper.GetFileContent(scripts, httpContext);

                    var outputFile = Encoding.UTF8.GetBytes(script.ToString());
                    var hashcode = HashingInstance.Hash(outputFile);

                    var keySb = new StringBuilder();
                    if (Provider.Enable.ScriptMinification)
                    {
                        string url = urlHelper.Content("~/resources/javascript/");
                        keySb.AppendLine(string.Format("<script src=\"{2}?keys={0}&amp;{1}\"></script>", string.Join("|", scripts.ToArray()), hashcode, url));
                    }
                    else
                    {
                        foreach (var s in scripts)
                        {
                            string url = urlHelper.Content(s);
                            keySb.AppendLine(string.Format("<script src=\"{0}?{1}\"></script>", url, hashcode));
                        }
                    }
                    return keySb.ToString();
                }, Provider.CacheDurations.ExternalScript));
            }

            sb.AppendLine(RenderDataUriFallback(httpContext));

            if (MothScriptHelper.InlineScripts.Any())
            {
                var isb = new StringBuilder("<script>");
                foreach (var s in MothScriptHelper.InlineScripts)
                {
                    isb.AppendLine(s);
                }
                isb.Append("</script>");

                sb.Append(isb.ToString());
            }

            return sb.ToString();
        }

        private string RenderDataUriFallback(HttpContextBase httpContext)
        {
            if (MothScriptHelper.DataUris.Count == 0) return "";

            StringBuilder cssSb = new StringBuilder("<style>");
            StringBuilder noJsSb = new StringBuilder("<noscript><style>");
            foreach (var item in MothScriptHelper.DataUris)
            {
                var img = DataUriHelper.GetDataUriImageFromCache(item.Value, httpContext);

                cssSb.AppendLine("." + item.Key + string.Format(@" {{ background-image:url(data:{0};base64,{1}); }}", img.Type, img.Base64));
                cssSb.AppendLine(".no-data-uri ." + item.Key + " { background-image:url('" + item.Value + "'); }");

                noJsSb.AppendLine(("." + item.Key + " { background-image:url('" + item.Value + "'); }"));
            }
            noJsSb.Append("</style></noscript>");
            cssSb.Append("</style>");

            // trucje om te checken of dataUri's worden ondersteund
            /*
             * (function() {
    var callback = function() {
        if (this.width !== 1 || this.height !== 1) {
			document.documentElement.className+=' no-data-uri';
		}
    };

    var img = new Image(), img = document.id(img) || new Element("img");
    img.onload = img.onerror = img.onabort = callback;
    // 1x1 px gif to test with
    img.src = "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==";
})();
             */

            cssSb.AppendLine(@"<script>(function(){var callback=function(){if(this.width!==1||this.height!==1){document.documentElement.className+=' no-data-uri';}};var img=new Image();img.onload=img.onerror=img.onabort=callback;img.src='data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==';})();</script>");

            return cssSb.ToString() + Environment.NewLine + noJsSb.ToString();
        }

        public AllowCachingEnum AllowCaching
        {
            get { return AllowCachingEnum.Yes; }
        }
    }
}
