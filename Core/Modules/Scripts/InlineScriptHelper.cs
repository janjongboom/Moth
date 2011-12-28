using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Mvc;
using System.Web.UI;
using Moth.Core.Helpers;
using Moth.Core.Providers;
using Yahoo.Yui.Compressor;

namespace Moth.Core
{
    public static class InlineScriptHelper
    {
        public static MothInlineScriptWrapper BeginScript<TModel>(this HtmlHelper<TModel> htmlHelper)
        {
            return BeginScript(htmlHelper, ScriptPositionEnum.Current);
        }

        public static MothInlineScriptWrapper BeginScript<TModel>(this HtmlHelper<TModel> htmlHelper, ScriptPositionEnum position)
        {
            return new MothInlineScriptWrapper(htmlHelper, position);
        }
    }

    public enum ScriptPositionEnum
    {
        Current,
        EndOfPage
    }

    public class MothInlineScriptWrapper : IDisposable
    {
        private readonly Guid _thisGuid;
        private readonly HtmlHelper _htmlHelper;

        public MothInlineScriptWrapper(HtmlHelper htmlHelper, ScriptPositionEnum position)
        {
            _thisGuid = Guid.NewGuid();
            _htmlHelper = htmlHelper;
            _htmlHelper.ViewContext.Writer.WriteLine("<% Moth.BeginScript {0} {1:N} %>", position, _thisGuid);
        }

        public void Dispose()
        {
            _htmlHelper.ViewContext.Writer.WriteLine("<% Moth.EndScript {0:N} %>", _thisGuid);
        }
    }
}
