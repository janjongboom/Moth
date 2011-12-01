using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Moth.Core.Helpers
{
    public class ViewEngineWrapper : IViewEngine
    {
        private IViewEngine _inner;
        public ViewEngineWrapper(IViewEngine inner)
        {
            _inner = inner;
        }
        public static void Wrap()
        {
            var listOfEngines = ViewEngines.Engines.ToList();
            if(listOfEngines.Any(e => !(e is ViewEngineWrapper)))
            {
                ViewEngines.Engines.Clear();
                foreach (var engine in listOfEngines)
                {
                    if(engine is ViewEngineWrapper)
                    {
                        ViewEngines.Engines.Add(engine);
                    }else
                    {
                        ViewEngines.Engines.Add(new ViewEngineWrapper(engine));
                    }
                }
            }

        }
        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            var result = _inner.FindPartialView(controllerContext, partialViewName, useCache);
            if (result.View != null)
            {
                result = new ViewEngineResult(new ViewWrapper(result.View), result.ViewEngine);
            }
            return result;
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            var result = _inner.FindView(controllerContext, viewName, masterName, useCache);
            if (result.View != null)
            {
                result = new ViewEngineResult(new ViewWrapper(result.View), result.ViewEngine);
            }
            return result;
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            _inner.ReleaseView(controllerContext, view);
        }
    }
    public class ViewWrapper : IView
    {
        private IView _inner;
        public ViewWrapper(IView inner)
        {
            _inner = inner;
        }
        public void Render(ViewContext viewContext, System.IO.TextWriter writer)
        {
            TextWriter wrappedWriter = (viewContext.Writer is TextWriterWrapper) ? viewContext.Writer : new TextWriterWrapper() { InnerWriter = viewContext.Writer };
            viewContext.Writer = wrappedWriter;
            _inner.Render(viewContext, wrappedWriter);
        }
    }
    public class TextWriterWrapper : TextWriter
    {
        public TextWriter InnerWriter { get; set; }

        public override System.Text.Encoding Encoding
        {
            get { return InnerWriter.Encoding; }
        }
        public override void Write(char value)
        {
            InnerWriter.Write(value);
        }
        public override void Write(char[] buffer)
        {
            InnerWriter.Write(buffer);
        }
        public override void Write(char[] buffer, int index, int count)
        {
            InnerWriter.Write(buffer, index, count);
        }
    }
}
