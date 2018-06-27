using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;

using System.Threading;
using System.Threading.Tasks;

namespace TestApp.Droid
{
    [Activity(Label = "TestApp", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }

        protected async override void OnStart()
        {
            System.Diagnostics.Debug.WriteLine("MainActivity : OnStart()");
            base.OnStart();

            // onStart, download something            
            var dms = new DownloadManagerService();
            dms.DownloadCompletedEventHandler += onDownloadCompleted;

            //await dms.GetFileFromUrl("http://mirror.cessen.com/blender.org/peach/trailer/trailer_iphone.m4v");
            //await dms.GetFileFromUrl("hasdfasdfa");
            //await dms.GetFileFromUrl("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi");


            // test cancel token
            var cts = new CancellationTokenSource();
            var cancelToken = cts.Token;

            var po = new ParallelOptions();

            var a1 = new Action(async () => 
            { 
                System.Diagnostics.Debug.WriteLine("Running download");
                await dms.GetFileFromUrl("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi", null, cancelToken);
            });

            var a2 = new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine("10 secs till cancel");
                cts.CancelAfter(10000);
            });

            Parallel.Invoke(po, a1, a2);
            
        }

        private void onDownloadCompleted(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Download completed event fired");
        }
    }
}

