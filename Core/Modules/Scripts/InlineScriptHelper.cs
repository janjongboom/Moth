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

        private readonly MemoryStream _ms;
        private readonly TextWriter _tw;
        private readonly TextWriter _originalTw;
        private readonly HtmlHelper _htmlHelper;
        private readonly ScriptPositionEnum _position;

        private readonly int _startIndexOf;

        static MothInlineScriptWrapper()
        {
            Provider = MothAction.CacheProvider;
        }

        public MothInlineScriptWrapper(HtmlHelper htmlHelper, ScriptPositionEnum position)
        {
            _ms = new MemoryStream();
            _tw = new StreamWriter(_ms);
            _htmlHelper = htmlHelper;
            _position = position;

            // MVC 2
            if (htmlHelper.ViewContext.Writer is HtmlTextWriter)
            {
                _originalTw = ((HtmlTextWriter)htmlHelper.ViewContext.Writer).InnerWriter;

                ((HtmlTextWriter)htmlHelper.ViewContext.Writer).InnerWriter = _tw;
            }

            // MVC 3
            if (htmlHelper.ViewContext.Writer is StringWriter)
            {
                _startIndexOf = ((StringWriter) _htmlHelper.ViewContext.Writer).GetStringBuilder().Length;
            }
        }

        public void Dispose()
        {
            string content;


            if (_originalTw == null)
            {
                if (_htmlHelper.ViewContext.Writer is StringWriter)
                {
                    // MVC 3
                    var stringBuilder = ((StringWriter)_htmlHelper.ViewContext.Writer).GetStringBuilder();

                    char[] inlineScript = new char[stringBuilder.Length - _startIndexOf];
                    stringBuilder.CopyTo(_startIndexOf, inlineScript, 0, inlineScript.Length);

                    content = new string(inlineScript);

                    stringBuilder.Remove(_startIndexOf, inlineScript.Length);
                }
                else
                {
                    // fak joe
                    return;
                }
            }
            else
            {
                _tw.Flush();

                _ms.Seek(0, SeekOrigin.Begin);

                using (StreamReader sr = new StreamReader(_ms))
                {
                    content = sr.ReadToEnd();
                }
            }

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
                if (_originalTw == null)
                {
                    // MVC 3?
                    _htmlHelper.ViewContext.Writer.Write(content);
                }
                else
                {
                    _originalTw.Write(content);
                    _tw.Dispose();
                }
            }
            else
            {
                MothScriptHelper.RegisterInlineScript(content);
            }
            // Set the writer back to the original, whether we are outputting here or at the bottom
            if (_originalTw == null)
            {
                ((HtmlTextWriter)_htmlHelper.ViewContext.Writer).InnerWriter = _originalTw;
            }
        }
    }
}
