namespace ACS_View.Views;

internal sealed class ServiceProviderRouteFactory<TPage>(IServiceProvider serviceProvider) : RouteFactory
    where TPage : Element
{
    public override Element GetOrCreate()
    {
        return serviceProvider.GetRequiredService<TPage>();
    }

    public override Element GetOrCreate(IServiceProvider services)
    {
        return services.GetRequiredService<TPage>();
    }
}
