using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Moth.Core.Helpers
{
    public static class MothSupportExtensions
    {
        private static readonly IOutputCacheProvider Provider;

        static MothSupportExtensions()
        {
            Provider = MothAction.CacheProvider;
        }

        public static bool IsMothEnabled(this ViewContext viewContext)
        {
            if (Provider.Enable.PageOutput
                && (viewContext.Writer is HtmlTextWriter || viewContext.RequestContext.HttpContext.Response.Output is HtmlTextWriter))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Indicates whether the views are rendered in reverse order.
        /// If so, scripts & css should be executed also in reverse order.
        /// 
        /// We do this for Razor views only, based on the LayoutPath.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="view"></param>
        /// <param name="viewPath"></param>
        /// <returns></returns>
        public static bool RegisterReverseOrder(this IViewDataContainer container, IView view, out string viewPath)
        {
            // yeah, I know this is kinda gay, but it's the only way to keep MVC 2 compatible
            Type razorViewAssignableFrom = new Func<Type>(() =>
                {
                    try
                    {
                        var mvcDll = AppDomain.CurrentDomain.GetAssemblies().Where(t => t.GetName().Name == "System.Web.Mvc");

                        var compiledView = mvcDll.First().GetType("System.Web.Mvc.RazorView");
                        if (compiledView.IsAssignableFrom(view.GetType())) return compiledView;

                        return null;
                    }
                    catch
                    {
                        return null;
                    }
                })();

            // not a Razor view? Then return false.
            if (razorViewAssignableFrom == null)
            {
                viewPath = null;
                return false;
            }

            viewPath = container.GetType().Name;
            return true;
        }
    }
}
