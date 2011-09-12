using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Moth.Core;

namespace Mvc3.Demo.Controllers
{
    // I disable output caching on all examples except the Output Caching one, so you can more easily debug
    [MothAction(OutputCaching = false)]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [MothAction(OutputCaching = true)]
        public ActionResult OutputCaching()
        {
            return View();
        }

        public ActionResult DataUri()
        {
            return View();
        }

        public ActionResult InlineScript()
        {
            return View();
        }

        public ActionResult Css()
        {
            return View();
        }

        public ActionResult Javascript()
        {
            return View();
        }


        public ActionResult CurrentDateTime()
        {
            return Content(DateTime.Now.ToString());
        }

        public ActionResult JavascriptDirectory()
        {
            return View();
        }
    }
}
