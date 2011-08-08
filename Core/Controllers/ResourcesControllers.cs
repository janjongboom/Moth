using System.Web.Mvc;

namespace Moth.Core
{
    [MothAction(DisableMoth = true)]
    public partial class ResourcesController : Controller
    {
        private readonly IOutputCacheProvider _provider;
        public ResourcesController()
        {
            _provider = MothAction.CacheProvider;
        }
    }
}
