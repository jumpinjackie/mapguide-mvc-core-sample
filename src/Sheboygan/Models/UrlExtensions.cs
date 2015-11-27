namespace Microsoft.AspNet.Mvc
{
    public static class UrlExtensions
    {
        public static string MgAction(this IUrlHelper helper, string controller, string action, string session, string mapName)
        {
            return helper.Action(action, controller, new { Session = session, MapName = mapName });
        }
    }
}
