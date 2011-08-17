// Type: Nancy.Security.ModuleSecurity
// Assembly: Nancy, Version=0.7.1.0, Culture=neutral
// Assembly location: e:\users\juanfran\documents\visual studio 2010\Projects\SocialDemocracy\packages\Nancy.0.7.1\lib\net40\Nancy.dll

using Nancy;
using System;
using System.Collections.Generic;

namespace Nancy.Security
{
    public static class ModuleSecurity
    {
        public static void RequiresAuthentication(this NancyModule module);
        public static void RequiresClaims(this NancyModule module, IEnumerable<string> requiredClaims);
        public static void RequiresValidatedClaims(this NancyModule module, Func<IEnumerable<string>, bool> isValid);
    }
}
