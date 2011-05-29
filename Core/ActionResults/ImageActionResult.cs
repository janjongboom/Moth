using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Moth.Core.ActionResults
{
    public class ImageResult : ActionResult
    {
        public Image Image { get; set; }
        public ImageFormat ImageFormat { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            // output 
            context.HttpContext.Response.Clear();
            if (ImageFormat.Equals(ImageFormat.Bmp)) context.HttpContext.Response.ContentType = "image/bmp";
            if (ImageFormat.Equals(ImageFormat.Gif)) context.HttpContext.Response.ContentType = "image/gif";
            if (ImageFormat.Equals(ImageFormat.Icon)) context.HttpContext.Response.ContentType = "image/vnd.microsoft.icon";
            if (ImageFormat.Equals(ImageFormat.Jpeg)) context.HttpContext.Response.ContentType = "image/jpeg";
            if (ImageFormat.Equals(ImageFormat.Png)) context.HttpContext.Response.ContentType = "image/png";
            if (ImageFormat.Equals(ImageFormat.Tiff)) context.HttpContext.Response.ContentType = "image/tiff";
            if (ImageFormat.Equals(ImageFormat.Wmf)) context.HttpContext.Response.ContentType = "image/wmf";
            Image.Save(context.HttpContext.Response.OutputStream, ImageFormat);
        }
    }
}
