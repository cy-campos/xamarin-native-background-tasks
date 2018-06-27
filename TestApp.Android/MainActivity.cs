using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;

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
            
            await dms.getFileFromUrl();
        }

        private void onDownloadCompleted(object sender, EventArgs e) 
        {
            System.Diagnostics.Debug.WriteLine("Download completed event fired");
        }
        
    
        
        
    }
    


}

