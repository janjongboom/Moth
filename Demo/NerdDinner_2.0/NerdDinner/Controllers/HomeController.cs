using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Moth.Core;

namespace NerdDinner.Controllers {

	[HandleErrorWithELMAH]
    // enable output caching on all Moth actions
    [MothAction(OutputCaching=true)]
    public class HomeController : Controller {
    
        public ActionResult Index() {
            return View();
        }

        public ActionResult About() {
            return View();
        }

        public ActionResult PrivacyPolicy()
        {
            return View();
        }
   }
}
