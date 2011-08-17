using System.Dynamic;
using System.Web;
using Facebook;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using Nancy.Cryptography;
using Nancy.Security;
using Nancy.Extensions;

namespace SocialDemocracy.Modules
{
    public class MainModule:NancyModule
    {
      
        public MainModule()
        {
            this.RequiresAuthentication();
           
            Get["/"] = parameters =>
            {
                var facebookId = long.Parse(Context.Items[SecurityConventions.AuthenticatedUsernameKey].ToString());
                var user = InMemoryUserCache.Get(facebookId);
                var client = new FacebookClient(user.AccessToken);
                dynamic me = client.Get("me");
                return "<h1>Welcome to Social Democracy! " + me.name + "</h1><p>You have logged in using facebook</p>";
            };
            
        }  
    }
}