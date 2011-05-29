using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace Moth.Core
{
    public static class MothRouteFactory
    {
        /// <summary>
        /// Registers routes for usage of the External resources functionality in Moth
        /// </summary>
        /// <param name="routes"></param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("moth_external_resources", 
                "resources/{action}", 
                new { controller = typeof(ResourcesController).ControllerName() }, 
                new { action = "javascript|css" });
        }
    }

    /// <summary>
    /// Truc om strong-typed controllers te gebruiken in je mappings. Dat refactoret lekker.
    /// </summary>
    internal static class ControllerTypeExtender
    {
        public static string ControllerName(this Type t)
        {
            return t.Name.Replace("Controller", "");
        }
    }
}
