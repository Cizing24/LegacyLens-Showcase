using System;
using System.Web;

namespace LegacyLens.Web
{
    public static class SessionProtection
    {
        public static string Key(HttpContext context)
        {
            // Writing session state persists the session cookie before the first Postback.
            if (context.Session["CsrfKey"] == null) context.Session["CsrfKey"] = Guid.NewGuid().ToString("N");
            return (string)context.Session["CsrfKey"];
        }
    }
}
