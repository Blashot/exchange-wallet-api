using Hangfire.Dashboard;

namespace Web.Api.Infrastructure;


internal sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}

