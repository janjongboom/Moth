using System.IO;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using System.Web.UI;
using Moth.Core.Helpers;
using Moth.Core.Providers;

namespace Moth.Core
{
    /// <summary>
    /// Renders donut placeholders
    /// </summary>
    public static class OutputSubstitutionHelper
    {
        private static readonly IOutputCacheProvider Provider;
        static OutputSubstitutionHelper()
        {
            Provider = MothAction.CacheProvider;
        }


        public static void RenderMothAction<TModel>(this HtmlHelper<TModel> htmlHelper, string actionName, string controllerName)
        {
            RenderMothAction(htmlHelper, actionName, controllerName, null);
        }

        public static void RenderMothAction<TModel>(this HtmlHelper<TModel> htmlHelper, string actionName, string controllerName, string areaName)
        {
            // als pagecaching uberhaupt aanstaat, en de huidige viewcontext schrijft tegen een gemockte writer aan; dan doen we donut.renderAction
            if (htmlHelper.ViewContext.IsMothEnabled())
            {
                htmlHelper.ViewContext.Writer.Write(string.Format("<% Moth.RenderAction('{0}','{1}','{2}'); %>", actionName, controllerName, areaName));
            }
            else
            {
                htmlHelper.RenderAction(actionName, controllerName, new {area = areaName});
            }
        }
    }
}