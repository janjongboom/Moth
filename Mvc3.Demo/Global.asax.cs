using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Moth.Core;

namespace Mvc3.Demo
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());

            // to make sure that Moth can post-process all requests, add a global filter
            // this doesn't enable output caching by default, so no danger
            filters.Add(new MothAction());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // call the Moth factory to register resources routes
            MothRouteFactory.RegisterRoutes(RouteTable.Routes);

            routes.MapRoute("Default",
                            "{controller}/{action}/{id}",
                            new {controller = "Home", action = "Index", viewname = "Index", id = UrlParameter.Optional} // Parameter defaults);
                );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }
    }
}