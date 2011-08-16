using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Moth.Core.Execution
{
    /// <summary>
    /// An Executor is a HTML post processor action, which will be executed after the view engine
    /// has generated the HTML. You can use this interface to replace certain parts of the HTML.
    /// </summary>
    public interface IExecutor
    {
        /// <summary>
        /// Allows a 'replacement' action to replace placeholders.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="controllerContext"></param>
        /// <param name="input">Input HTML</param>
        /// <returns>The new HTML that will be emitted to the browser</returns>
        string Replace(HttpContextBase httpContext, ControllerContext controllerContext, string input);

        /// <summary>
        /// Indicates whether this executer should be ran every request, or may be cached.
        /// </summary>
        AllowCachingEnum AllowCaching { get; }
    }

    public enum AllowCachingEnum
    {
        /// <summary>
        /// Indicates that the executor isn't request dependant, and that the output can be cached
        /// </summary>
        Yes,
        /// <summary>
        /// This executor is request dependant, and cannot be cached
        /// </summary>
        No
    }
}
