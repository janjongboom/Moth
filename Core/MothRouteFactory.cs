using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                new { action = typeof(ResourcesController).PublicMethodsConstraint() },
                new [] { typeof(ResourcesController).Namespace });
        }
    }

    /// <summary>
    /// Some strong typed extensions to define routes
    /// </summary>
    internal static class ControllerTypeExtender
    {
        /// <summary>
        /// Returns the name of the controller so it can be used in a route
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string ControllerName(this Type t)
        {
            return t.Name.Replace("Controller", "");
        }

        /// <summary>
        /// Creates a constraint for the 'action' parameter, based on the public instance methods of a type
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string PublicMethodsConstraint(this Type t)
        {
            return string.Join("|", t.GetMethods(BindingFlags.Public | BindingFlags.Instance).Select(s => s.Name.ToLower()).ToArray());
        }
    }
}
