using System;
using System.Collections.Generic;
using System.Linq;

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
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Console.WriteLine("AppDelegate : FinishedLaunching");

            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            var blah = new Downloader();
            blah.Start();

            return base.FinishedLaunching(app, options);
        }

        public Action BackgroundSessionCompletionHandler { get; set; }

        public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
        {
            Console.WriteLine("HandleEventsForBackgroundUrl");
            BackgroundSessionCompletionHandler = completionHandler;
        }

        public override void OnActivated(UIApplication uiApplication)
        {
            base.OnActivated(uiApplication);
            Console.WriteLine("AppDelegate : OnActivated");
        }

        public override void OnResignActivation(UIApplication uiApplication)
        {
            base.OnResignActivation(uiApplication);
            Console.WriteLine("AppDelegate : OnResignActivation");
        }

        public override void DidEnterBackground(UIApplication uiApplication)
        {
            base.DidEnterBackground(uiApplication);
            Console.WriteLine("AppDelegate : DidEnterBackground");
        }

        public override void WillEnterForeground(UIApplication uiApplication)
        {
            base.WillEnterForeground(uiApplication);
            Console.WriteLine("AppDelegate : WillEnterForeground");
        }

        public override void WillTerminate(UIApplication uiApplication)
        {
            base.WillTerminate(uiApplication);
            Console.WriteLine("AppDelegate : WillTerminate");
        }
    }
}
