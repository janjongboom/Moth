using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Moth.Core.Execution;

namespace Moth.Core.Modules.OutputSubstitution
{
    internal class OutputSubstitutionExecutor : IExecutor
    {
        private static readonly Regex PartialAction = new Regex(@"<%\s*?Moth\.RenderAction\((['""])(?<action>\w+)\1,\1(?<controller>\w+)\1\);\s*%>", RegexOptions.Compiled);

        public string Replace(HttpContextBase httpContext, ControllerContext controllerContext, string input)
        {
            Stopwatch sw = Stopwatch.StartNew();

            //Use HtmlHelper to render partial view to fake context  
            var html = new HtmlHelper(new ViewContext(controllerContext,
                new FakeView(), new ViewDataDictionary(), new TempDataDictionary(), new StringWriter()), new ViewPage());

            foreach (var m in PartialAction.Matches(input).Cast<Match>())
            {
                string action = m.Groups["action"].Value;
                string controller = m.Groups["controller"].Value;

                using (var ms = new MemoryStream())
                using (var tw = new StreamWriter(ms))
                {
                    html.ViewContext.Writer = tw;

                    Stopwatch actionSw = Stopwatch.StartNew();

                    html.RenderAction(action, controller);

                    actionSw.Stop();
                    Trace.Write(string.Format("Donut hole: {0} took {1:F2} ms.", action, actionSw.Elapsed.TotalMilliseconds));

                    tw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);

                    using (var sr = new StreamReader(ms))
                    {
                        input = input.Replace(m.Value, sr.ReadToEnd());
                    }
                }
            }

            sw.Stop();

            Trace.Write(string.Format("Fill Donut Hole took {0:F2} ms.", sw.Elapsed.TotalMilliseconds));

            return input;
        }

        // Should be executed every time
        public AllowCachingEnum AllowCaching
        {
            get { return AllowCachingEnum.No; }
        }

        class FakeView : IView
        {
            public void Render(ViewContext viewContext, TextWriter writer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
