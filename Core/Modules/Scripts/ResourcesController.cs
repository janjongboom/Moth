using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Moth.Core.Filters;
using Moth.Core.Providers;
using Yahoo.Yui.Compressor;

namespace Moth.Core
{
    public partial class ResourcesController
    {
        [HttpGet, FarFutureExpiration]
        public ActionResult Javascript(string keys)
        {
            string result = _provider.GetFromCache("resources.javascript." + Request.Url.PathAndQuery, () =>
            {
                var scriptFiles = keys.Split('|').ToList();

                var sb = new StringBuilder();

                foreach (var s in scriptFiles)
                {
                    var script = MothScriptHelper.GetFileContent(new List<string> { s }, HttpContext).ToString();
                    if (_provider.Enable.ScriptMinification)
                    {
                        script = JavaScriptCompressor.Compress(script, false, true, true, false, Int32.MaxValue, Encoding.UTF8, new CultureInfo("en-US"));
                    }
                    sb.AppendLine(script);
                }

                return sb.ToString();
            }, _provider.CacheDurations.ExternalScript);

            return new ContentResult()
            {
                Content = result,
                ContentType = "text/javascript",
            };
        }
    }
}
