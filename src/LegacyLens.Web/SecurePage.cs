using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using LegacyLens.Domain;
using LegacyLens.Infrastructure;

namespace LegacyLens.Web
{
    public class SecurePage : Page
    {
        protected Database Db { get { return Database.Configured; } }
        public Actor Actor { get; private set; }
        public string CorrelationId { get { return (string)Context.Items["CorrelationId"]; } }
        protected override void OnInit(EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            ViewStateUserKey = SessionProtection.Key(Context);
            if (!Request.IsAuthenticated) { Response.Redirect("~/Login.aspx",true); return; }
            Actor = new IdentityStore(Db).Find(User.Identity.Name);
            if (Actor == null) { System.Web.Security.FormsAuthentication.SignOut();Response.Redirect("~/Login.aspx",true);return; }
            base.OnInit(e);
        }
        protected void Attempt(Action action, Label message)
        {
            try { action(); }
            catch (UnauthorizedAccessException ex) { Response.StatusCode = 403;Response.TrySkipIisCustomErrors = true;message.Text = Server.HtmlEncode(string.IsNullOrEmpty(ex.Message) ? "此操作需要更高權限。" : ex.Message);Global.Log(Context,"AuthorizationDenied",ex); }
            catch (ArgumentException ex) { message.Text = Server.HtmlEncode(ex.Message);Global.Log(Context,"ValidationRejected",ex); }
            catch (ConflictException ex) { message.Text = Server.HtmlEncode(ex.Message);Global.Log(Context,"ConcurrencyConflict",ex); }
        }
    }
}
