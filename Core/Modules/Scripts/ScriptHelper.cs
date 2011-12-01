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

        public static void RegisterScriptDirectory(this HtmlHelper html, string jsDir, string category = null, int maxDepth = 0)
        {
            // If the directory doesn't exist, let the user know
            string physicalPath = html.ViewContext.HttpContext.Server.MapPath(jsDir);
            if (!Directory.Exists(physicalPath))
                throw new DirectoryNotFoundException(string.Format("The script directory '{0}' does not exist", jsDir));

            RegisterScriptDirectory(html, jsDir, category, 0, maxDepth);
        }

        internal static void RegisterScriptDirectory(HtmlHelper html, string contentDir, string category, int currentDepth, int maxDepth)
        {
            // Get the physical path for the content path
            string physicalPath = html.ViewContext.HttpContext.Server.MapPath(contentDir);
            if (string.IsNullOrEmpty(physicalPath))
                return;

            // Remove any leading slashes on the content directory
            if (contentDir.Last() == '/')
                contentDir = contentDir.Substring(0, contentDir.Length - 1);

            // Get all javascript files in the directory and register them
            foreach (var file in Directory.GetFiles(physicalPath))
            {
                if (Path.GetExtension(file) != ".js")
                    continue;

                string scriptPath = string.Format("{0}/{1}", contentDir, Path.GetFileName(file));
                RegisterScript(html, scriptPath, category);
            }

            // Proceed to sub-directories if we aren't past the max depth
            currentDepth++;
            if (currentDepth > maxDepth)
                return;

            foreach (var subDir in Directory.GetDirectories(physicalPath))
            {
                string subDirContentPath = string.Format("{0}/{1}", contentDir, subDir.Substring(subDir.LastIndexOf(@"\") + 1));
                RegisterScriptDirectory(html, subDirContentPath, category, currentDepth, maxDepth);
            }
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
                var filename = MothAction.CacheProvider.PathFixup(s);

                var filenameWithMapPath = httpContext.Server.MapPath(filename);

                string content = File.ReadAllText(filenameWithMapPath, Encoding.UTF8);
                script.AppendLine(content);
            }
            return script;
        }
    }
}
