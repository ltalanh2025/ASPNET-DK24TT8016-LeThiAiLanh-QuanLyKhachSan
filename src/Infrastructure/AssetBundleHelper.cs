using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QLKS.Infrastructure
{
    public sealed class HttpStatusCodeResult : StatusCodeResult
    {
        public HttpStatusCodeResult(HttpStatusCode statusCode) : base((int)statusCode) { }
        public HttpStatusCodeResult(int statusCode) : base(statusCode) { }
    }

    public static class HtmlFormExtensions
    {
        public static MvcForm BeginForm(
            this IHtmlHelper html,
            string actionName,
            string controllerName,
            object routeValues,
            FormMethod method,
            object htmlAttributes)
        {
            return html.BeginForm(actionName, controllerName, routeValues, method, antiforgery: null, htmlAttributes);
        }
    }

    public static class Styles
    {
        public static IHtmlContent Render(string bundlePath)
        {
            var files = bundlePath switch
            {
                "~/Content/css" => new[]
                {
                    "/Content/bootstrap.css", "/Content/site.css", "/Content/layout.css", "/Content/components.css"
                },
                "~/Content/Customer/css" => new[]
                {
                    "/Content/Customer/customer-site.css", "/Content/Customer/customer-responsive.css"
                },
                _ => Array.Empty<string>()
            };

            return new HtmlString(string.Join(Environment.NewLine,
                files.Select(path => $"<link rel=\"stylesheet\" href=\"{path}\" />")));
        }
    }

    public static class Scripts
    {
        public static IHtmlContent Render(string bundlePath)
        {
            var files = bundlePath switch
            {
                "~/bundles/jquery" => new[] { "/Scripts/jquery-3.7.0.js" },
                "~/bundles/jqueryval" => new[] { "/Scripts/jquery.validate.js", "/Scripts/jquery.validate.unobtrusive.js" },
                "~/bundles/modernizr" => new[] { "/Scripts/modernizr-2.8.3.js" },
                "~/bundles/bootstrap" => new[] { "/Scripts/bootstrap.bundle.js" },
                "~/bundles/site" => new[] { "/Scripts/site.js" },
                "~/bundles/customer-js" => new[]
                {
                    "/Scripts/Customer/customer-menu.js", "/Scripts/Customer/booking-search.js",
                    "/Scripts/Customer/image-fallback.js", "/Scripts/Customer/payment-countdown.js"
                },
                _ => Array.Empty<string>()
            };

            return new HtmlString(string.Join(Environment.NewLine,
                files.Select(path => $"<script src=\"{path}\"></script>")));
        }
    }
}
