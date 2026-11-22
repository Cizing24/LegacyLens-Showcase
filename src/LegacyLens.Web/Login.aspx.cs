using System;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using LegacyLens.Infrastructure;

namespace LegacyLens.Web
{
    public class LoginPage : Page
    {
        protected TextBox Username,Password;
        protected Label Message;
        protected override void OnInit(EventArgs e)
        {
            ViewStateUserKey = SessionProtection.Key(Context);
            Response.Cache.SetCacheability(HttpCacheability.NoCache);Response.Cache.SetNoStore();
            base.OnInit(e);
        }
        protected void Submit_Click(object sender, EventArgs e)
        {
            var actor = new IdentityStore(Database.Configured).Authenticate(Username.Text,Password.Text);
            Password.Text = "";
            if (actor == null) { Message.Text = "帳號或密碼不正確，或帳號暫時鎖定。請確認後再試。";Global.Log(Context,"LoginRejected");return; }
            Session.Clear();Session.Abandon();
            FormsAuthentication.SetAuthCookie(actor.Name,false);
            Global.Log(Context,"LoginSucceeded");
            Response.Redirect("~/Cases.aspx",true);
        }
    }
}
