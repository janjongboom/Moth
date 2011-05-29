using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoneSoft.CSS;

namespace Moth.Core.Helpers
{
    public static class CssDocumentExtender
    {
        public static string ToOutput(this CSSDocument doc)
        {
            return doc.ToString();
        }
    }
}
