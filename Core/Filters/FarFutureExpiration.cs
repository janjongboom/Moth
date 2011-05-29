using System;
using System.Web;
using System.Web.Mvc;

namespace Moth.Core.Filters
{
    internal class FarFutureExpiration : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            HttpCachePolicyBase cache = filterContext.HttpContext.Response.Cache;
            TimeSpan cacheDuration = new TimeSpan(180, 0, 0, 0);

            cache.SetCacheability(HttpCacheability.Private);
            cache.SetExpires(DateTime.Now.Add(cacheDuration));
            cache.SetMaxAge(cacheDuration);
            cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
        }
    }
}