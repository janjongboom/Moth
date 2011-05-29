using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Moth.Core.Providers;

namespace Moth.Core
{
    public static class DataUriHelper
    {
        private static readonly IOutputCacheProvider Provider;
        private static readonly Random Random;
        static DataUriHelper()
        {
            Provider = MothAction.CacheProvider;
            Random = new Random();
        }

        public static MvcHtmlString DataUriImage<T>(this HtmlHelper<T> htmlHelper, string image, IDictionary<string, object> htmlAttributes)
        {
            DataUriImage dataUriImage = GetDataUriImageFromCache(image, htmlHelper);

            var tb = GetTagbuilder(dataUriImage, htmlAttributes);

            return MvcHtmlString.Create(tb.ToString(TagRenderMode.Normal));
        }

        internal static DataUriImage GetDataUriImageFromCache (string image, HtmlHelper htmlHelper)
        {
            return Provider.GetFromCache("datauri." + image, () =>
                {
                    var img = new DataUriImage();
                    img.ImageUrl = image;

                    byte[] buffer;

                    Uri uri;
                    if (Uri.TryCreate(image, UriKind.RelativeOrAbsolute, out uri) && uri.IsAbsoluteUri)
                    {
                        // download the file
                        using (var wc = new WebClient())
                        {
                            buffer = wc.DownloadData(uri);
                        }
                    }
                    else
                    {
                        buffer = File.ReadAllBytes(htmlHelper.ViewContext.RequestContext.HttpContext.Server.MapPath("~/" + image.TrimStart('~').TrimStart('/')));
                    }

                    using (var imageStream = new MemoryStream(buffer))
                    {
                        var bitmap = Image.FromStream(imageStream);

                        img.Width = bitmap.Width;
                        img.Height = bitmap.Height;

                        img.Type = GetStringFromImageType(bitmap.RawFormat);
                    }

                    img.Base64 = Convert.ToBase64String(buffer);

                    img.Id = "";
                    while(img.Id.Length < 10)
                    {
                        img.Id += (char)('a' + Random.Next(26));
                    }

                    return img;
                }, Provider.CacheDurations.DataUri);
        }

        public static string GetStringFromImageType(ImageFormat format)
        {
            if (format == System.Drawing.Imaging.ImageFormat.Jpeg)
            {
                return "image/jpg";
            }
            else if (format == System.Drawing.Imaging.ImageFormat.Png)
            {
                return "image/png";
            }
            else if (format == System.Drawing.Imaging.ImageFormat.Gif)
            {
                return "image/gif";
            }
            else
            {
                return "image/jpg";
            }
        }

        public static MvcHtmlString DataUriImage<T>(this HtmlHelper<T> htmlHelper, string image)
        {
            return DataUriImage<T>(htmlHelper, image, null);
        }

        private static TagBuilder GetTagbuilder(DataUriImage image, IDictionary<string, object> htmlAttributes)
        {
            TagBuilder tb = new TagBuilder("span");
            tb.MergeAttributes(htmlAttributes);

            tb.MergeAttribute("style", string.Format(";width:{0}px;height:{1}px;display:inline-block;", image.Width, image.Height));
            tb.MergeAttribute("class", image.Id);

            MothScriptHelper.RegisterDataUri(image.Id, image.ImageUrl);

            return tb;
        }
    }

    internal class DataUriImage
    {
        public string Id { get; set; }
        public string Type { get; set; }

        public string ImageUrl { get; set; }
        public string Base64 { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}
