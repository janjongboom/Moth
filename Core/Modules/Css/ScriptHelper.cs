using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Moth.Core.Helpers;
using Moth.Core.Modules.Css;

namespace Moth.Core
{
    public static partial class MothScriptHelper
    {
        internal static List<ResourceGroup> Stylesheets
        {
            get { return (HttpContext.Current.Items["Stylesheets"] ?? (HttpContext.Current.Items["Stylesheets"] = new List<ResourceGroup>())) as List<ResourceGroup>; }
        }

        /// <summary>
        /// Register a stylesheet
        /// </summary>
        /// <param name="html"></param>
        /// <param name="cssFile"></param>
        public static void RegisterStylesheet(this HtmlHelper html, string cssFile)
        {
            RegisterStylesheet(html, cssFile, null);
        }

        public static void RegisterStylesheet(this HtmlHelper html, string cssFile, string category)
        {
            bool inReverseOrder = false;

            string viewPath;
            inReverseOrder = (html.ViewDataContainer.RegisterReverseOrder(html.ViewContext.View, out viewPath));

            category = category ?? "";

            if (!Stylesheets.Any(v => v.ViewPath == viewPath))
            {
                if (inReverseOrder)
                {
                    Stylesheets.Insert(0, new ResourceGroup { ViewPath = viewPath });
                }
                else
                {
                    Stylesheets.Add(new ResourceGroup { ViewPath = viewPath });
                }
            }

            Stylesheets.First(s => s.ViewPath == viewPath).Items.Add(new ResourceWrapper
            {
                Category = category,
                Filename = cssFile
            });
        }

        public static MvcHtmlString RenderCss(this HtmlHelper html)
        {
            if (html.ViewContext.IsMothEnabled())
            {
                return MvcHtmlString.Create("<% Moth.RenderCss(); %>");
            }
            else
            {
                return MvcHtmlString.Create(new CssExecutor().GetContentFromHttpContext(html.ViewContext.HttpContext, html.ViewContext.Controller.ControllerContext));
            }
        }
    }
}
