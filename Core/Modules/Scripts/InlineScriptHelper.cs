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
        private static readonly IOutputCacheProvider Provider;

        private readonly StringWriter _tw;
        private readonly TextWriter _originalTw;
        private readonly HtmlHelper _htmlHelper;
        private readonly ScriptPositionEnum _position;

        static MothInlineScriptWrapper()
        {
            Provider = MothAction.CacheProvider;
        }

        public MothInlineScriptWrapper(HtmlHelper htmlHelper, ScriptPositionEnum position)
        {
            if(htmlHelper == null || htmlHelper.ViewContext == null || htmlHelper.ViewContext.Writer == null)
            {
                throw new ArgumentNullException("htmlHelper.ViewContext.Writer");
            }
            if(! (htmlHelper.ViewContext.Writer is TextWriterWrapper))
            {
                throw new System.InvalidOperationException(
                    "ViewContext.Writer is of the wrong type. This means that you have not yet initialized the ViewEngineWrapper for Moth or that extra ViewEngines have been registered afterwards. Add any ViewEngines before you make any call to Moth.");
            }

            _position = position;
            _tw = new StringWriter();
            _htmlHelper = htmlHelper;
            _htmlHelper.ViewContext.Writer.Flush();

            _originalTw = ((TextWriterWrapper)htmlHelper.ViewContext.Writer).InnerWriter;
            ((TextWriterWrapper)htmlHelper.ViewContext.Writer).InnerWriter = _tw;
        }

        public void Dispose()
        {
            string content;
            _htmlHelper.ViewContext.Writer.Flush();
            content = _tw.GetStringBuilder().ToString();
            ((TextWriterWrapper)_htmlHelper.ViewContext.Writer).InnerWriter = _originalTw;

            var key = "inputhelper.scripts." + new MurmurHash2UInt32Hack().Hash(Encoding.UTF8.GetBytes(content));
            if (Provider.Enable.ScriptMinification)
            {
                var minified = Provider.GetFromCache(key,
                                                     () => JavaScriptCompressor.Compress(content, false, true, true, false, Int32.MaxValue, Encoding.UTF8, new CultureInfo("en-US")),
                                                     Provider.CacheDurations.InlineScript);

                if (!content.Contains("<script") && minified.Contains("<script"))
                {
                    // YUI compressor makes some mistakes when doing inline thingy's. Like making '<scr' + 'ipt>' into <script> which breaks browser
                    // so we won't compress this part. sorry :-)
                }
                else
                {
                    content = minified;
                }
            }

            if (_position == ScriptPositionEnum.Current)
            {
                _htmlHelper.ViewContext.Writer.Write(content);
            }
            else
            {
                MothScriptHelper.RegisterInlineScript(content);
            }
        }
    }
}
