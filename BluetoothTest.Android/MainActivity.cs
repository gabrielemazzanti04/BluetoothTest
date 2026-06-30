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
                    ActivityCompat.RequestPermissions(this, missing, RequestBluetoothPermissions);
            }
        }

        private const int RequestBluetoothPermissions = 100;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode != RequestBluetoothPermissions)
                return;

            bool allGranted = grantResults.Length > 0 &&
                              grantResults.All(r => r == Permission.Granted);

            if (!allGranted)
            {
                // Almeno un permesso negato: mostriamo un Toast e non carichiamo i dispositivi
                global::Android.Widget.Toast
                    .MakeText(this, "Permessi Bluetooth necessari per usare l'app.", global::Android.Widget.ToastLength.Long)!
                    .Show();
            }
        }
    }
}
