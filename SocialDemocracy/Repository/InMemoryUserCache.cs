using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nancy.Authentication.Forms;

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