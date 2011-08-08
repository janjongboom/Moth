using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moth.Core.Helpers
{
    [Serializable]
    internal class ResourceWrapper
    {
        public string Category { get; set; }
        public string Filename { get; set; }
    }

    [Serializable]
    internal class ResourceGroup
    {
        public string ViewPath { get; set; }
        public List<ResourceWrapper> Items { get; set; }

        public ResourceGroup()
        {
            Items = new List<ResourceWrapper>();
        }
    }
}
