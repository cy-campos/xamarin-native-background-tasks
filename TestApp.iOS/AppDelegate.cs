using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace TestApp.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            System.Diagnostics.Debug.WriteLine("AppDelegate : FinishedLaunching");

            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            //await Task.Run(() => { testDownload(); });
            //testDownload();
            testDownloadCancel();
            
            return base.FinishedLaunching(uiApplication, launchOptions);
        }
        
        private async void testDownload()
        {
            // onStart, download something            
            var dms = new Downloader();
            dms.DownloadCompletedEventHandler += ((object sender, EventArgs e) =>
            {
                System.Diagnostics.Debug.WriteLine("DownloadCompleted Event Fired.");
            });

            await dms.GetFileFromUrl("http://mirror.cessen.com/blender.org/peach/trailer/trailer_iphone.m4v");
            //await dms.GetFileFromUrl("hasdfasdfa");
            //await dms.GetFileFromUrl("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi");
        }
        
        private void testDownloadCancel() 
        {
            var dms = new Downloader();
            
            // test cancel token
            var cts = new CancellationTokenSource();
            var cancelToken = cts.Token;

            int lastVal = 0;
            var onPercentUpdate = new Action<int>((percentAsInteger) => 
            {
                if (percentAsInteger != lastVal)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Progress: {0}%", percentAsInteger));
                    lastVal = percentAsInteger;
                }
            });
            
            var po = new ParallelOptions();

            var a1 = new Action(async () => 
            { 
                System.Diagnostics.Debug.WriteLine("Running download");
                await dms.GetFileFromUrl("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi", onPercentUpdate, cancelToken);
            });

            var a2 = new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine("10 secs till cancel");
                cts.CancelAfter(10000);
            });

            Parallel.Invoke(po, a1, a2);
        }

        public Action BackgroundSessionCompletionHandler { get; set; }

        public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
        {
            System.Diagnostics.Debug.WriteLine("HandleEventsForBackgroundUrl");
            BackgroundSessionCompletionHandler = completionHandler;
        }

        public override void OnActivated(UIApplication uiApplication)
        {
            base.OnActivated(uiApplication);
            System.Diagnostics.Debug.WriteLine("AppDelegate : OnActivated");
        }

        public override void OnResignActivation(UIApplication uiApplication)
        {
            base.OnResignActivation(uiApplication);
            System.Diagnostics.Debug.WriteLine("AppDelegate : OnResignActivation");
        }

        public override void DidEnterBackground(UIApplication uiApplication)
        {
            base.DidEnterBackground(uiApplication);
            System.Diagnostics.Debug.WriteLine("AppDelegate : DidEnterBackground");
        }

        public override void WillEnterForeground(UIApplication uiApplication)
        {
            base.WillEnterForeground(uiApplication);
            System.Diagnostics.Debug.WriteLine("AppDelegate : WillEnterForeground");
        }

        public override void WillTerminate(UIApplication uiApplication)
        {
            base.WillTerminate(uiApplication);
            System.Diagnostics.Debug.WriteLine("AppDelegate : WillTerminate");
        }
    }
}
