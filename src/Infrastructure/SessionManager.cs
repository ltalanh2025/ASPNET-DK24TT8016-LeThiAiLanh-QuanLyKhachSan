using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace QLKS.Infrastructure
{
    public sealed class SessionManager
    {
        private readonly ISession session;
        private static readonly HashSet<string> IntegerKeys = new(StringComparer.Ordinal)
        {
            SessionKeys.UserId,
            SessionKeys.RoleId,
            CustomerSessionKeys.CustomerId
        };

        public SessionManager(ISession session)
        {
            this.session = session;
        }

        public object this[string key]
        {
            get
            {
                return IntegerKeys.Contains(key)
                    ? session.GetInt32(key)
                    : session.GetString(key);
            }
            set
            {
                if (value == null)
                {
                    session.Remove(key);
                }
                else if (value is int integer)
                {
                    session.SetInt32(key, integer);
                }
                else
                {
                    session.SetString(key, Convert.ToString(value) ?? string.Empty);
                }
            }
        }

        public void Clear() => session.Clear();
        public void Abandon() => session.Clear();
        public void Remove(string key) => session.Remove(key);
    }

    public sealed class SessionAccessor
    {
        private readonly IHttpContextAccessor accessor;

        public SessionAccessor(IHttpContextAccessor accessor)
        {
            this.accessor = accessor;
        }

        public object this[string key] => accessor.HttpContext == null
            ? null
            : new SessionManager(accessor.HttpContext.Session)[key];
    }
}
