using System;

using Foundation;
using TestApp.iOS;
using UIKit;

namespace TestApp
{

    public class SimpleBackgroundTransferViewController
    {

        const string Identifier = "com.SimpleBackgroundTransfer.BackgroundSession";
        //const string DownloadUrlString = "https://upload.wikimedia.org/wikipedia/commons/9/97/The_Earth_seen_from_Apollo_17.jpg";
        const string DownloadUrlString = "https://download.blender.org/peach/bigbuckbunny_movies/BigBuckBunny_640x360.m4v";

        public NSUrlSessionDownloadTask downloadTask;
        public NSUrlSession session;

        public SimpleBackgroundTransferViewController()
        {
            if (session == null)
                session = InitBackgroundSession();
        }

        public void Start()
        {
            if (downloadTask != null)
                return;

            using (var url = NSUrl.FromString(DownloadUrlString))
            using (var request = NSUrlRequest.FromUrl(url))
            {
                downloadTask = session.CreateDownloadTask(request);
                downloadTask.Resume();
            }
        }

        public NSUrlSession InitBackgroundSession()
        {
            Console.WriteLine("InitBackgroundSession");
            using (var configuration = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(Identifier))
            {
                return NSUrlSession.FromConfiguration(configuration, (INSUrlSessionDelegate)new UrlSessionDelegate(this), null);
            }
        }



        public class UrlSessionDelegate : NSUrlSessionDownloadDelegate, INSUrlSessionDelegate
        {
            public SimpleBackgroundTransferViewController controller;

            public UrlSessionDelegate(SimpleBackgroundTransferViewController controller)
            {
                this.controller = controller;
            }

            public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
            {
                Console.WriteLine("Set Progress");
                if (downloadTask == controller.downloadTask)
                {
                    float progress = totalBytesWritten / (float)totalBytesExpectedToWrite;
                    Console.WriteLine(string.Format("DownloadTask: {0}  progress: {1}", downloadTask, progress));



                    InvokeOnMainThread(async () =>
                    {
                        await System.Threading.Tasks.Task.Delay(100000);
                    });
                }
            }

            public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
            {
                Console.WriteLine("Finished");
                Console.WriteLine("File downloaded in : {0}", location);
                NSFileManager fileManager = NSFileManager.DefaultManager;

                var URLs = fileManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User);
                NSUrl documentsDictionry = URLs[0];

                NSUrl originalURL = downloadTask.OriginalRequest.Url;
                NSUrl destinationURL = documentsDictionry.Append("image1.png", false);
                NSError removeCopy;
                NSError errorCopy;

                fileManager.Remove(destinationURL, out removeCopy);
                bool success = fileManager.Copy(location, destinationURL, out errorCopy);

                if (success)
                {
                    // we do not need to be on the main/UI thread to load the UIImage
                    UIImage image = UIImage.FromFile(destinationURL.Path);
                    InvokeOnMainThread(() =>
                    {
                        
                    });
                }
                else
                {
                    Console.WriteLine("Error during the copy: {0}", errorCopy.LocalizedDescription);
                }
            }

            public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
            {
                Console.WriteLine("DidComplete");
                if (error == null)
                    Console.WriteLine("Task: {0} completed successfully", task);
                else
                    Console.WriteLine("Task: {0} completed with error: {1}", task, error.LocalizedDescription);

                float progress = task.BytesReceived / (float)task.BytesExpectedToReceive;
                InvokeOnMainThread(() =>
                {
                    
                });

                controller.downloadTask = null;
            }

            public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
            {
                Console.WriteLine("DidResume");
            }

            public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
            {
                using (AppDelegate appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate)
                {
                    var handler = appDelegate.BackgroundSessionCompletionHandler;
                    if (handler != null)
                    {
                        appDelegate.BackgroundSessionCompletionHandler = null;
                        handler();
                    }
                }

                Console.WriteLine("All tasks are finished");
            }
        }
    }
}