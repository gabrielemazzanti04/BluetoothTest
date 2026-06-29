using Microsoft.Extensions.DependencyInjection;
using System;

namespace BluetoothTest;

public static class Services
{
    public static IServiceProvider Provider { get; private set; } = default!;

    public static void Initialize(Action<IServiceCollection>? platformServices = null)
    {
        var services = new ServiceCollection();

        services.AddCommonServices();

        platformServices?.Invoke(services);

        Provider = services.BuildServiceProvider();
    }
}
