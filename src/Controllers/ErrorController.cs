using System;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKS.Infrastructure;

namespace QLKS.Controllers
{
    public class ErrorController : BaseController
    {
        [AllowAnonymous]
        public ActionResult Forbidden()
        {
            return Show(HttpStatusCode.Forbidden);
        }

        [AllowAnonymous]
        [ActionName("NotFound")]
        public ActionResult NotFoundPage()
        {
            return Show(HttpStatusCode.NotFound);
        }

        [AllowAnonymous]
        public ActionResult ServerError()
        {
            return Show(HttpStatusCode.InternalServerError);
        }

        [AllowAnonymous]
        [ActionName("StatusCode")]
        public ActionResult StatusCodePage(int code)
        {
            var statusCode = Enum.IsDefined(typeof(HttpStatusCode), code)
                ? (HttpStatusCode)code
                : HttpStatusCode.InternalServerError;
            return Show(statusCode);
        }

        private ActionResult Show(HttpStatusCode statusCode)
        {
            Response.StatusCode = (int)statusCode;
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
