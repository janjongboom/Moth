using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Moth.Core
{
    public static partial class MothScriptHelper
    {
        internal static Dictionary<string, string> DataUris
        {
            get { return (HttpContext.Current.Items["DataUris"] ?? (HttpContext.Current.Items["DataUris"] = new Dictionary<string, string>())) as Dictionary<string, string>; }
        }

        internal static void RegisterDataUri(string id, string file)
        {
            if (!DataUris.ContainsKey(id))
                DataUris.Add(id, file);
        }
    }
}
