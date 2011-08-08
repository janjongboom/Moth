using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Moth.Core.Helpers;
using Moth.Core.Modules.Scripts;
using Moth.Core.Providers;

namespace Moth.Core
{
    public static partial class MothScriptHelper
    {
        internal static List<ResourceGroup> Scripts
        {
            get { return (HttpContext.Current.Items["Scripts"] ?? (HttpContext.Current.Items["Scripts"] = new List<ResourceGroup>())) as List<ResourceGroup>; }
        }

        internal static List<string> InlineScripts
        {
            get { return (HttpContext.Current.Items["InlineScripts"] ?? (HttpContext.Current.Items["InlineScripts"] = new List<string>())) as List<string>; }
        }

        public static void RegisterScript(this HtmlHelper html, string jsFile)
        {
            RegisterScript(html, jsFile, null);
        }

        public static void RegisterScript(this HtmlHelper html, string jsFile, string category)
        {
            bool inReverseOrder = false;

            string viewPath;
            inReverseOrder = (html.ViewDataContainer.RegisterReverseOrder(html.ViewContext.View, out viewPath));

            category = category ?? "";

            if (!Scripts.Any(v => v.ViewPath == viewPath))
            {
                if (inReverseOrder)
                {
                    Scripts.Insert(0, new ResourceGroup {ViewPath = viewPath});
                }
                else
                {
                    Scripts.Add(new ResourceGroup {ViewPath = viewPath});
                }
            }

            Scripts.First(s => s.ViewPath == viewPath).Items.Add(new ResourceWrapper
                {
                    Category = category,
                    Filename = jsFile
                });
        }

        internal static void RegisterInlineScript(string script)
        {
            InlineScripts.Add(script);
        }

        public static MvcHtmlString RenderScripts(this HtmlHelper html)
        {
            if (html.ViewContext.IsMothEnabled())
            {
                return MvcHtmlString.Create("<% Moth.RenderJavascript(); %>");
            }
            else
            {
                return MvcHtmlString.Create(new ScriptExecutor().GetScriptsFromHttpContext(html.ViewContext.HttpContext, html.ViewContext.Controller.ControllerContext));
            }
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
