using System;
using Facebook;
using Nancy;
using Nancy.Security;

public static class FacebookAuthenticatedCheckPipeline
{
    public static Response CheckUserIsNothAuthorisedByFacebookAnymore(NancyContext context)
    {
        long? facebookId = null;
        try
        {

            if (AuthenticatedUserNameHasValue(context))
            {
                facebookId = long.Parse(context.Items[SecurityConventions.AuthenticatedUsernameKey].ToString());
                var user = InMemoryUserCache.Get(facebookId.Value);
                var client = new FacebookClient(user.AccessToken);
                dynamic me = client.Get("me");
            }
        }
        catch (FacebookOAuthException)
        {
            //If an exception gets thrown the access token is no longer valid
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