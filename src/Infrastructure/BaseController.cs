using Microsoft.AspNetCore.Mvc;

namespace QLKS.Infrastructure
{
    public abstract class BaseController : Controller
    {
        protected SessionManager Session => new SessionManager(HttpContext.Session);
        protected ServerPathResolver Server { get; } = new ServerPathResolver();
        protected ActionResult HttpNotFound() => NotFound();
    }
}
