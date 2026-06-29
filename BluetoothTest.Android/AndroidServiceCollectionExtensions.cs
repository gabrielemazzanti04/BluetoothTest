using Microsoft.Extensions.DependencyInjection;

namespace BluetoothTest.Android;

public static class AndroidServiceCollectionExtensions
{
    public static IServiceCollection AddAndroidServices(this IServiceCollection services)
    {
        services.AddSingleton<IBluetoothService, AndroidBluetoothService>();
        return services;
    }
}
