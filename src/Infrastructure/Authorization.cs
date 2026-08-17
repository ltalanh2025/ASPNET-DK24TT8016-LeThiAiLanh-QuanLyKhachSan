using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Globalization;
using System.Text;

namespace QLKS.Infrastructure
{
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            if (!IsAuthorized(context.HttpContext))
            {
                HandleUnauthorized(context);
            }
        }

        protected virtual bool IsAuthorized(HttpContext httpContext)
        {
            return httpContext.Session.GetInt32(SessionKeys.UserId).HasValue;
        }

        protected virtual void HandleUnauthorized(AuthorizationFilterContext context)
        {
            if (IsAjaxRequest(context.HttpContext.Request))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var returnUrl = context.HttpContext.Request.PathBase
                + context.HttpContext.Request.Path
                + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        }

        protected static bool IsAjaxRequest(HttpRequest request)
        {
            return string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class RoleAuthorizeAttribute : SessionAuthorizeAttribute
    {
        private readonly string[] allowedRoles;

        public RoleAuthorizeAttribute(params string[] allowedRoles)
        {
            this.allowedRoles = allowedRoles ?? Array.Empty<string>();
        }

        protected override bool IsAuthorized(HttpContext httpContext)
        {
            if (!base.IsAuthorized(httpContext))
            {
                return false;
            }

            var role = httpContext.Session.GetString(SessionKeys.RoleName);
            return allowedRoles.Any(x => string.Equals(x, role, StringComparison.OrdinalIgnoreCase));
        }

        protected override void HandleUnauthorized(AuthorizationFilterContext context)
        {
            if (context.HttpContext.Session.GetInt32(SessionKeys.UserId).HasValue)
            {
                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/Error.cshtml",
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            base.HandleUnauthorized(context);
        }
    }

    public class CustomerAuthorizeAttribute : SessionAuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpContext httpContext)
        {
            return httpContext.Session.GetInt32(CustomerSessionKeys.CustomerId).HasValue;
        }

        protected override void HandleUnauthorized(AuthorizationFilterContext context)
        {
            if (IsAjaxRequest(context.HttpContext.Request))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var returnUrl = context.HttpContext.Request.PathBase
                + context.HttpContext.Request.Path
                + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "CustomerAccount", new { returnUrl });
        }
    }

    public static class RoleNameNormalizer
    {
        public static string Normalize(string roleName)
        {
            var decomposed = (roleName ?? string.Empty).Trim().ToLowerInvariant().Replace("đ", "d").Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(character))
                    builder.Append(character);
            }
            var value = builder.ToString();

            if (value == "admin" || value == "quantri") return RoleNames.Admin;
            if (value == "letan") return RoleNames.Receptionist;
            if (value == "tapvu") return RoleNames.Housekeeping;
            return roleName == null ? string.Empty : roleName.Trim();
        }
    }
}
