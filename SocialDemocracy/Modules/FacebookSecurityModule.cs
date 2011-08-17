using System;
using Facebook;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Extensions;

namespace SocialDemocracy.Modules
{
    public class FacebookSecurityModule:NancyModule
    {
        private string basePath = "http://socialdemocracy.apphb.com";
        // private string basePath = "http://localhost:81";

        public FacebookSecurityModule()
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

        private FacebookOAuthClient GetFacebookOAuthClient()
        {
            var oAuthClient = new FacebookOAuthClient(FacebookApplication.Current);
            oAuthClient.RedirectUri = new Uri(GetOathRedirectUrl());
            return oAuthClient;
        }


        //The base path is not part the context request.. so this needs to be configured :(.
        private string GetRequestUriAbsolutePath()
        {
            var url = Context.Request.Url;
            return basePath + "/" + url.Path + url.Query;
        }

        private void AddAuthenticatedUserToCache(string code, Guid userId)
        {
            var oAuthClient = GetFacebookOAuthClient();
            dynamic tokenResult = oAuthClient.ExchangeCodeForAccessToken(code);
            string accessToken = tokenResult.access_token;
            var facebookClient = new FacebookClient(accessToken);
            dynamic me = facebookClient.Get("me?fields=id,name");
            long facebookId = Convert.ToInt64(me.id);
 
            InMemoryUserCache.Add(new FacebookUser
                                      {
                                          UserId = userId,
                                          AccessToken = accessToken,
                                          FacebookId = facebookId,
                                          Name = (string)me.name,
                                      });
        }

    }
}