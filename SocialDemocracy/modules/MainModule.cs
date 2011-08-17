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
        //private string basePath = "http://socialdemocracy.apphb.com/";
        private string basePath = "http://localhost:81";

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

                var stringUri = GetRequestUriAbsolutePath();

                if (FacebookOAuthResult.TryParse(stringUri, out oauthResult))
                {
                    if (oauthResult.IsSuccess)
                    {
                        //Assign a temporary GUID to identify the user via cookies, we follow Nancy Forms Authentication to prevent storing facebook ids or tokens in cookies.
                        var userId = Guid.NewGuid();
                        AddAuthenticatedUserToCache(code, userId);
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

        public String GetOathRedirectUrl()
        {
            return basePath + "/oath";
        }


        public FacebookOAuthClient GetFacebookOAuthClient()
        {
            var oAuthClient = new FacebookOAuthClient(FacebookApplication.Current);
            oAuthClient.RedirectUri = new Uri(GetOathRedirectUrl());
            return oAuthClient;
        }

        private void AddAuthenticatedUserToCache(string code, Guid userId)
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
 
            InMemoryUserCache.Add(new FacebookUser
                                      {
                                          UserId = userId,
                                          AccessToken = accessToken,
                                          Expires = expiresOn,
                                          FacebookId = facebookId,
                                          Name = (string)me.name,
                                      });
        }

        private string GetRequestUriAbsolutePath()
        {
            var url = Context.Request.Url;
            return basePath + url.Port + "/" + url.Path + url.Query;
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
                var user = InMemoryUserCache.Get(facebookId);
                var client = new FacebookClient(user.AccessToken);
                dynamic me = client.Get("me");
                return "<h1>Welcome to Social Democracy! " + me.name + "</h1><p>You have logged in using facebook</p>";
            };
            
        }  
    }
}
     
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
            this.BeforeRequest.AddItemToEndOfPipeline(FacebookAuthenticatedCheckPipeline.GetFacebookLoggedOutUserResponse);
            
        }        
    }


    public static class FacebookAuthenticatedCheckPipeline
    {
        public static Response GetFacebookLoggedOutUserResponse(NancyContext context)
        {
            long? facebookId = null;
            try
            {

                if (AuthenticatedUserNameHasValue(context))
                {
                    facebookId = long.Parse(context.Items[SecurityConventions.AuthenticatedUsernameKey].ToString());
                    InMemoryUserCache.Remove(facebookId.Value);
                    var user = InMemoryUserCache.Get(facebookId.Value);
                    var client = new FacebookClient(user.AccessToken);
                    dynamic me = client.Get("me");
                }
            }
            catch (FacebookOAuthException)
            {
                // facebook web client will auto delete the fb cookie if you get oauth exception
                // so you don't need to invalidate the facebook cookie.
                //RemoveFormsAuthenticationCookie(context);
                RemoveUserFromCache(context, facebookId);
                return new Response() { StatusCode = HttpStatusCode.Unauthorized };
            }
            return context.Response;
        }

        private static void RemoveUserFromCache(NancyContext context, long? facebookId)
        {
            context.Items[SecurityConventions.AuthenticatedUsernameKey] = null;
            if (facebookId.HasValue) InMemoryUserCache.Remove(facebookId.Value);
        }

        private static bool AuthenticatedUserNameHasValue(NancyContext context)
        {
            return context.Items.ContainsKey(SecurityConventions.AuthenticatedUsernameKey) && context.Items[SecurityConventions.AuthenticatedUsernameKey] != null && context.Items[SecurityConventions.AuthenticatedUsernameKey].ToString() != String.Empty;
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

    public class InMemoryUserCache:IUsernameMapper
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

        public static void Remove(long facebookId)
        {
            users.Remove(facebookId);
        }
    
        public string GetUsernameFromIdentifier(Guid identifier)
        {
            var usersFound = users.Where(x => x.Value.UserId == identifier);
            if (usersFound.Count() > 0)
            {
                return usersFound.First().Value.FacebookId.ToString();
            }
            //returning null will trigger a non authenticated user
            return null;
        }
}
