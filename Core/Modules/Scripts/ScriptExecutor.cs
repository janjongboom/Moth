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
using Yahoo.Yui.Compressor;
using System.Globalization;

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
        private static Regex _renderPattern = new Regex(@"<%\s*Moth\.RenderJavascript\(\);\s*%>", RegexOptions.Compiled);
        // <% Moth.BeginScript EndOfPage xxxxxxxxxxxxxxxxxxxxxxxxxx %>script script script<% Moth.EndScript xxxxxxxxxxxxxxxxxxxxxxxxxx %>
        private static Regex _inlinePattern = new Regex(@"<%\s*Moth\.BeginScript (Current|EndOfPage) ([0-9a-f]{32})\s*%>(.*)<%\s*Moth\.EndScript \2\s*%>", RegexOptions.Compiled | RegexOptions.Singleline);

        public string Replace(HttpContextBase httpContext, ControllerContext controllerContext, string input)
        {
            string resultString = input;
            string collectedInlineScripts = "";

            MatchCollection allInlineScriptBlocks = _inlinePattern.Matches(resultString);
            foreach (var block in allInlineScriptBlocks.Cast<Match>().Reverse())
            {
                string scriptContent = block.Groups[3].Value;
                string pos = block.Groups[1].Value;
                if (pos == ScriptPositionEnum.Current.ToString())
                {
                    resultString = resultString.Substring(0, block.Index) +
                        MakeScriptOutputFor(scriptContent) +
                        resultString.Substring(block.Index + block.Length);
                }
                else if (pos == ScriptPositionEnum.EndOfPage.ToString())
                {
                    collectedInlineScripts += scriptContent;
                    resultString = resultString.Substring(0, block.Index) +
                        resultString.Substring(block.Index + block.Length);
                }
            }


            Match m = _renderPattern.Match(resultString);
            if (m.Success)
            {
                resultString = resultString.Substring(0, m.Index) +
                    GetScriptsFromHttpContext(httpContext, controllerContext) +
                    MakeScriptBlockFor(collectedInlineScripts) +
                    resultString.Substring(m.Index + m.Length);
            }

            return resultString;
        }

        private string MakeScriptBlockFor(string content)
        {
            if (content == null || content.Trim() == "")
            {
                return "";
            }
            content = MakeScriptOutputFor(content);

            return String.Format("<script type=\"text/javascript\">{0}</script>", content);
        }

        private static string MakeScriptOutputFor(string content)
        {
            var key = "inputhelper.scripts." + new MurmurHash2UInt32Hack().Hash(Encoding.UTF8.GetBytes(content));
            if (Provider.Enable.ScriptMinification)
            {
                var minified = Provider.GetFromCache(key,
                                                     () => JavaScriptCompressor.Compress(content, false, true, true, false, Int32.MaxValue, Encoding.UTF8, new CultureInfo("en-US")),
                                                     Provider.CacheDurations.InlineScript);

                if (!content.Contains("<script") && minified.Contains("<script"))
                {
                    // YUI compressor makes some mistakes when doing inline thingy's. Like making '<scr' + 'ipt>' into <script> which breaks browser
                    // so we won't compress this part. sorry :-)
                }
                else
                {
                    content = minified;
                }
            }
            return content;
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

            sb.AppendLine(RenderDataUriFallback(httpContext, controllerContext));

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

        private string RenderDataUriFallback(HttpContextBase httpContext, ControllerContext controllerContext)
        {
            if (MothScriptHelper.DataUris.Count == 0) return "";

            StringBuilder cssSb = new StringBuilder("<style>");
            StringBuilder noJsSb = new StringBuilder("<noscript><style>");
            foreach (var item in MothScriptHelper.DataUris)
            {
                var img = DataUriHelper.GetDataUriImageFromCache(item.Value, httpContext);

                var urlHelper = new UrlHelper(new RequestContext(httpContext, controllerContext.RouteData));
                var bgUrl = urlHelper.Content(item.Value);

                cssSb.AppendLine("." + item.Key + string.Format(@" {{ background-image:url(data:{0};base64,{1}); }}", img.Type, img.Base64));
                cssSb.AppendLine(".no-data-uri ." + item.Key + " { background-image:url('" + bgUrl + "'); }");

                noJsSb.AppendLine(("." + item.Key + " { background-image:url('" + bgUrl + "'); }"));
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
