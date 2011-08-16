using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using Facebook;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Cookies;
using Nancy.Cryptography;
using Nancy.Extensions;
using Nancy.Security;
using Nancy.Extensions;

namespace SocialDemocracy.Modules
{

    public class SecurityModule:NancyModule
    {
        private string basePath = "http://socialdemocracy.apphb.com/";

        public String GetOathRedirectUrl()
        {
            return basePath + "/oath";
            //return Context.ToFullPath("~/oath");
        }

        public FacebookOAuthClient GetFacebookOAuthClient()
        {
            var oAuthClient = new FacebookOAuthClient(FacebookApplication.Current);
            oAuthClient.RedirectUri = new Uri(GetOathRedirectUrl());
            return oAuthClient;
        }

        public SecurityModule()
        {
            Get["/login"] = x =>
            {
                return Context.GetRedirect(GetFacebookOAuthClient().GetLoginUrl().AbsoluteUri);
            };

            Get["/oath"] = x =>
               {
                string code = Context.Request.Query.code;
                FacebookOAuthResult oauthResult;

                var url = Context.Request.Url;
                var stringUri = basePath + url.Port + "/" + url.Path + url.Query;

                if (FacebookOAuthResult.TryParse(stringUri, out oauthResult))
                {
                    if (oauthResult.IsSuccess)
                    {
                        var oAuthClient = GetFacebookOAuthClient();
                        dynamic tokenResult = oAuthClient.ExchangeCodeForAccessToken(code);
                        string accessToken = tokenResult.access_token;

                        DateTime? expiresOn = null;

                        if (tokenResult.ContainsKey("expires"))
                        {
                            expiresOn = DateTimeConvertor.FromUnixTime(tokenResult.expires);
                        }

                        var facebookClient = new FacebookClient(accessToken);
                        dynamic me = facebookClient.Get("me?fields=id,name");
                        long facebookId = Convert.ToInt64(me.id);
                        var userId = Guid.NewGuid();

                        InMemoryUserStore.Add(new FacebookUser
                        {
                            UserId = userId,
                            AccessToken = accessToken,
                            Expires = expiresOn,
                            FacebookId = facebookId,
                            Name = (string)me.name,
                        });

                        return this.LoginAndRedirect(userId);

                    }
                }

                return this.LogoutAndRedirect("~/");
            };


            Get["/logout"] = x =>
            {
                return this.LogoutAndRedirect("~/");

            };
        }
    }

    public class MainModule:NancyModule
    {
      
        public MainModule()
        {
            this.RequiresAuthentication();

            Get["/"] = parameters =>
            {
                var facebookId = long.Parse(Context.Items[SecurityConventions.AuthenticatedUsernameKey].ToString());
                var user = InMemoryUserStore.Get(facebookId);
                var client = new FacebookClient(user.AccessToken);
                dynamic me = client.Get("me");
                return "<h1>Welcome to Social Democracy! " + me.name + "</h1><p>Nothing to see here at the moment.</p>";
            };


            
        }
    }
}
     


    /*
     *  Check if logged out
            var fbWebContext = FacebookWebContext.Current;
            if (fbWebContext.IsAuthorized() && fbWebContext.UserId > 0)
            {
                try
                {
                    var fb = new FacebookWebClient(fbWebContext);
                    dynamic result = fb.Get("/me");
                }
                catch(FacebookOAuthException){
                // facebook web client will auto delete the fb cookie if you get oauth exception
                // so you don't need to invalidate the facebook cookie.
  
                 Response.Redirect("~/login.aspx");
                }
            }
     * 
     */


    public class FormsAuthBootstrapper : DefaultNancyBootstrapper
    {
        protected override void InitialiseInternal(TinyIoC.TinyIoCContainer container)
        {
            base.InitialiseInternal(container);

            var formsAuthConfiguration = 
                new FormsAuthenticationConfiguration()
                {
                    RedirectUrl = "~/login",
                    UsernameMapper = container.Resolve<IUsernameMapper>(),
                };

            FormsAuthentication.Enable(this, formsAuthConfiguration);
        }
    }

    public class FacebookUser
    {
        public Guid UserId { get; set;}
        public long FacebookId { get; set; }
        public string AccessToken { get; set; }
        public DateTime? Expires { get; set; }
        public string Name { get; set; }
    }

    public class InMemoryUserStore:IUsernameMapper
    {
        static readonly IDictionary<long, FacebookUser> users = new ConcurrentDictionary<long, FacebookUser>();

        public static void Add(FacebookUser user)
        {
            users[user.FacebookId] = user;
        }

        public static FacebookUser Get(long facebookId)
        {
            return users[facebookId];
        }
    
        public string GetUsernameFromIdentifier(Guid identifier)
        {
            var user = users.FirstOrDefault(x => x.Value.UserId == identifier);
            return user.Value.FacebookId.ToString();
        }
}
