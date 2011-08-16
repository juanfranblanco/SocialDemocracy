// Type: Nancy.Extensions.ContextExtensions
// Assembly: Nancy, Version=0.7.1.0, Culture=neutral
// Assembly location: e:\users\juanfran\documents\visual studio 2010\Projects\SocialDemocracy\packages\Nancy.0.7.1\lib\net40\Nancy.dll

using Nancy;
using Nancy.Responses;

namespace Nancy.Extensions
{
    public static class ContextExtensions
    {
        public static bool IsAjaxRequest(this NancyContext context);
        public static string ToFullPath(this NancyContext context, string path);
        public static RedirectResponse GetRedirect(this NancyContext context, string path);
    }
}
