using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using Avalonia;
using Avalonia.Android;
using System.Linq;
using Android;
using AndroidX.Core.Content;

namespace BluetoothTest.Android
{
    [Activity(
        Label = "BluetoothTest.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var permissions = new[]
                {
                Manifest.Permission.BluetoothConnect,
                Manifest.Permission.BluetoothScan
            };

                var missing = permissions
                    .Where(p => ContextCompat.CheckSelfPermission(this, p) != Permission.Granted)
                    .ToArray();

                if (missing.Length > 0)
                {
                    ActivityCompat.RequestPermissions(this, missing, 100);
                }
            }
        }
    }
}
