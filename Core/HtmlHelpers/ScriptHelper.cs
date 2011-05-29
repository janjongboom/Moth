using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Moth.Core.Helpers;
using Moth.Core.Providers;

namespace Moth.Core
{
    public static partial class MothScriptHelper
    {
        private static readonly MurmurHash2UInt32Hack HashingInstance;
        private static readonly IOutputCacheProvider Provider;

        static MothScriptHelper()
        {
            HashingInstance = new MurmurHash2UInt32Hack();
            Provider = MothAction.CacheProvider;
        }

        private static Dictionary<string, List<string>> Scripts
        {
            get { return (HttpContext.Current.Items["Scripts"] ?? (HttpContext.Current.Items["Scripts"] = new Dictionary<string, List<string>>())) as Dictionary<string, List<string>>; }
        }

        private static List<string> InlineScripts
        {
            get { return (HttpContext.Current.Items["InlineScripts"] ?? (HttpContext.Current.Items["InlineScripts"] = new List<string>())) as List<string>; }
        }

        private static List<string> Stylesheets
        {
            get { return (HttpContext.Current.Items["Stylesheets"] ?? (HttpContext.Current.Items["Stylesheets"] = new List<string>())) as List<string>; }
        }

        private static Dictionary<string, string> DataUris
        {
            get { return (HttpContext.Current.Items["DataUris"] ?? (HttpContext.Current.Items["DataUris"] = new Dictionary<string, string>())) as Dictionary<string, string>; }
        }

        public static void RegisterScript(this HtmlHelper html, string jsFile)
        {
            RegisterScript(html, jsFile, null);
        }

        public static void RegisterScript(this HtmlHelper html, string jsFile, string categorie)
        {
            categorie = categorie ?? "";

            if (!Scripts.ContainsKey(categorie))
                Scripts.Add(categorie, new List<string>());

            Scripts[categorie].Add(jsFile);
        }

        public static void RegisterStylesheet(this HtmlHelper html, string cssFile)
        {
            Stylesheets.Add(cssFile);
        }

        internal static void RegisterDataUri(string id, string file)
        {
            if(!DataUris.ContainsKey(id))
                DataUris.Add(id, file);
        }

        internal static void RegisterInlineScript(string script)
        {
            InlineScripts.Add(script);
        }

        public static MvcHtmlString RenderScripts(this HtmlHelper html)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var cat in Scripts.Keys)
            {
                var scripts = Scripts[cat].ToList();

                var key = "scripthelper.renderscripts." + string.Join("|", scripts.OrderBy(s => s).ToArray());

                sb.AppendLine(Provider.GetFromCache(key, () =>
                {
                    // hashcode bepalen
                    StringBuilder script = GetFileContent(scripts, html.ViewContext.RequestContext.HttpContext);

                    var outputFile = Encoding.UTF8.GetBytes(script.ToString());
                    var hashcode = HashingInstance.Hash(outputFile);

                    var keySb = new StringBuilder();
                    if (Provider.Enable.ScriptMinification)
                    {
                        keySb.AppendLine(string.Format("<script src=\"/resources/javascript/?keys={0}&amp;{1}\"></script>", string.Join("|", scripts.ToArray()), hashcode));
                    }
                    else
                    {
                        foreach (var s in scripts)
                        {
                            keySb.AppendLine(string.Format("<script src=\"/js/{0}?{1}\"></script>", s, hashcode));
                        }
                    }
                    return keySb.ToString();
                }, Provider.CacheDurations.ExternalScript));
            }

            sb.AppendLine(RenderDataUriFallback(html));

            if (InlineScripts.Any())
            {
                var isb = new StringBuilder("<script>");
                foreach (var s in InlineScripts)
                {
                    isb.AppendLine(s);
                }
                isb.Append("</script>");

                sb.Append(isb.ToString());
            }

            return MvcHtmlString.Create(sb.ToString());
        }

        public static MvcHtmlString RenderCss(this HtmlHelper html)
        {
            var css = Stylesheets.ToList();
            var key = "scripthelper.rendercss." + string.Join("|", css.OrderBy(s => s).ToArray());
            return MvcHtmlString.Create(Provider.GetFromCache(key, () =>
            {
                StringBuilder stylo = GetFileContent(css, html.ViewContext.RequestContext.HttpContext);

                var outputFile = Encoding.UTF8.GetBytes(stylo.ToString());
                var hashcode = HashingInstance.Hash(outputFile);

                return string.Format("<link rel=\"stylesheet\" type=\"text/css\" media=\"all\" href=\"/resources/css/?keys={0}&amp;{1}\" />", string.Join("|", css.ToArray()), hashcode);
            }, Provider.CacheDurations.ExternalScript));
        }

        private static string RenderDataUriFallback(this HtmlHelper html)
        {
            if (DataUris.Count == 0) return "";

            StringBuilder cssSb = new StringBuilder("<style>");
            StringBuilder noJsSb = new StringBuilder("<noscript><style>");
            foreach (var item in DataUris)
            {
                var img = DataUriHelper.GetDataUriImageFromCache(item.Value, html);

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

        public static StringBuilder GetFileContent(List<string> items, HttpContextBase httpContext)
        {
            StringBuilder script = new StringBuilder();
            foreach (var s in items)
            {
                var filename = s;
                if(!filename.StartsWith("~/")) filename = "~/" + filename.TrimStart('/');

                var filenameWithMapPath = httpContext.Server.MapPath(filename);

                string content = File.ReadAllText(filenameWithMapPath, Encoding.UTF8);
                script.AppendLine(content);
            }
            return script;
        }
    }
}
