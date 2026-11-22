using System;
using System.IO;
using System.Web;
using System.Web.Script.Serialization;

namespace LegacyLens.Web
{
    public class Global : HttpApplication
    {
        private static readonly object logLock = new object();
        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            Context.Items["CorrelationId"] = Guid.NewGuid().ToString();
            Response.Headers["X-Correlation-ID"] = (string)Context.Items["CorrelationId"];
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["X-Frame-Options"] = "DENY";
            Response.Headers["Referrer-Policy"] = "no-referrer";
            // Web Forms emits inline postback scripts; external resources and framing stay restricted.
            Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
            if (!Request.IsLocal) { Response.StatusCode = 403; Response.End(); }
        }
        public static void Log(HttpContext context, string eventName, Exception error = null)
        {
            var path = context.Server.MapPath("~/App_Data/logs");
            Directory.CreateDirectory(path);
            var entry = new { timestamp = DateTime.UtcNow.ToString("o"), correlationId = context.Items["CorrelationId"],
                eventName, path = context.Request.AppRelativeCurrentExecutionFilePath,
                errorType = error == null ? null : error.GetType().FullName,
                rootErrorType = error == null ? null : error.GetBaseException().GetType().FullName,
                stack = error == null ? null : error.GetBaseException().StackTrace };
            lock (logLock) File.AppendAllText(Path.Combine(path,"events.jsonl"),new JavaScriptSerializer().Serialize(entry) + Environment.NewLine);
        }
        protected void Application_Error(object sender, EventArgs e)
        {
            Log(Context,"UnhandledError",Server.GetLastError());
            Server.ClearError();
            Response.Clear();
            Response.ContentType = "text/html";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;
            Response.Write("<!doctype html><html lang='zh-Hant'><meta charset='utf-8'><title>暫時無法完成</title><h1>暫時無法完成操作</h1><p>請稍後再試，或提供追蹤編號給維護人員：" + HttpUtility.HtmlEncode((string)Context.Items["CorrelationId"]) + "</p><a href='Cases.aspx'>返回案件列表</a></html>");
        }
    }
}
