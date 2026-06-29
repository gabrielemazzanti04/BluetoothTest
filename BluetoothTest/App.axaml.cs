using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BluetoothTest.ViewModels;
using BluetoothTest.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace BluetoothTest
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; set; } = default!;



        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                var vm = BluetoothTest.Services.Provider.GetRequiredService<MainViewModel>();

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = vm
                    };
                }
                else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
                {
                    singleViewFactoryApplicationLifetime.MainViewFactory = () => new MainView { DataContext = vm };
                }
                else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
                {
                    singleViewPlatform.MainView = new MainView
                    {
                        DataContext = vm
                    };
                }

                base.OnFrameworkInitializationCompleted();

            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}