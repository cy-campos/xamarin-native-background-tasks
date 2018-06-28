using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using TestApp.iOS;
using UIKit;

namespace TestApp
{
    public class Downloader : IDownloader
    {
        public long TotalSizeBytes { get; set; }
        public long TotalBytesDownloaded { get; set; }

        public int PercentageDownloaded { get { throw new NotImplementedException(); } }

        public event EventHandler DownloadCompletedEventHandler;
        private void onDownloadCompleted(object sender, EventArgs e) { DownloadCompletedEventHandler?.Invoke(sender, e); }

        public event EventHandler DownloadFailedEventHandler;
        private void onDownloadFailed(object sender, EventArgs eventArgs) { DownloadFailedEventHandler?.Invoke(sender, eventArgs); }

        public async Task GetFileFromUrl(string url)
        {
            await GetFileFromUrl(url, null, null);
        }

        public NSUrlSessionDownloadTask downloadTask;
        public NSUrlSession session;
        const string identifier = "com.SimpleBackgroundTransfer.BackgroundSession";
        
        public async Task GetFileFromUrl(string url, Action<int> onPercentUpdate, object CancellationToken)
        {
            await Task.Run(() =>
            {
                // start a background session if not started already    
                session = session ?? initBackgroundSession(onPercentUpdate, CancellationToken);

                // get a NSUrl, get a request, and queue the download
                using (var nsurl = NSUrl.FromString(url))
                using (var request = NSUrlRequest.FromUrl(nsurl))
                {
                    downloadTask = session.CreateDownloadTask(request);
                    downloadTask.Resume();
                }
            });   
        }

        private UrlSessionDelegate _urlSessionDelegate;
        private NSUrlSession initBackgroundSession(Action<int> onPercentUpdate, object CancellationToken)
        {
            // Because eventually NSUrlSession.FromConfiguration must receive an interface as a param (INSUrlSessionDelegate),
            // we have to assign whatever properties we are listening for here.
            _urlSessionDelegate = new UrlSessionDelegate(this, onPercentUpdate, CancellationToken);
            
            System.Diagnostics.Debug.WriteLine("InitBackgroundSession");
            using (var configuration = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(identifier))
            {
                return NSUrlSession.FromConfiguration(configuration, (INSUrlSessionDelegate)_urlSessionDelegate, null);
            }
        }
    }
    
    public class UrlSessionDelegate : NSUrlSessionDownloadDelegate, INSUrlSessionDelegate
    {
        private Downloader controller;

        private Action<int> onPercentUpdate;
        private CancellationToken CancellationToken;
        
        public UrlSessionDelegate(Downloader controller, Action<int> onPercentUpdate, object CancellationToken)
        {
            this.controller = controller;
            //this.onPercentUpdate = onPercentUpdate;
            //this.CancellationToken = CancellationToken;
            
            // set up onPercentUpdate - Action should take an integer parameter and display it somehow
            this.onPercentUpdate = onPercentUpdate ?? ((percentageAsInteger) => { });

            // handle object CancellationToken, since this can be null
            this.CancellationToken = CancellationToken != null ? (CancellationToken)CancellationToken : new CancellationToken();

            // check to see if cancelled first
            if (this.CancellationToken.IsCancellationRequested)
                this.CancellationToken.ThrowIfCancellationRequested();
        }

        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
        {
            if (downloadTask == controller.downloadTask)
            {
                //System.Diagnostics.Debug.WriteLine(string.Format("DownloadTask: {0}  progress: {1}/{2}", downloadTask, totalBytesWritten, totalBytesExpectedToWrite));

                onPercentUpdate(_getPercentageDownloaded(totalBytesWritten, totalBytesExpectedToWrite));
                CancellationToken.ThrowIfCancellationRequested();
                //InvokeOnMainThread(async () =>
                //{
                //    await System.Threading.Tasks.Task.Delay(100000);
                //});
            }
        }
        
        private int _getPercentageDownloaded(long totalBytesWritten, long totalBytesExpectedToWrite)
        {
            return totalBytesExpectedToWrite < 1 ? 0 : (int)(((double)totalBytesWritten / (double)totalBytesExpectedToWrite) * 100);
        }

        public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
        {
            System.Diagnostics.Debug.WriteLine("Finished");
            System.Diagnostics.Debug.WriteLine("File downloaded in : {0}", location);
            //NSFileManager fileManager = NSFileManager.DefaultManager;

            //var URLs = fileManager.GetUrls(NSSearchPathDirectory.DocumentDirectory, NSSearchPathDomain.User);
            //NSUrl documentsDictionry = URLs[0];

            //NSUrl originalURL = downloadTask.OriginalRequest.Url;
            //NSUrl destinationURL = documentsDictionry.Append("image1.png", false);
            //NSError removeCopy;
            //NSError errorCopy;

            //fileManager.Remove(destinationURL, out removeCopy);
            //bool success = fileManager.Copy(location, destinationURL, out errorCopy);

            //if (success)
            //{
            //    // we do not need to be on the main/UI thread to load the UIImage
            //    UIImage image = UIImage.FromFile(destinationURL.Path);
            //    InvokeOnMainThread(() =>
            //    {

            //    });
            //}
            //else
            //{
            //    System.Diagnostics.Debug.WriteLine("Error during the copy: {0}", errorCopy.LocalizedDescription);
            //}
        }

        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
        {
            System.Diagnostics.Debug.WriteLine("DidComplete");
            if (error == null)
                System.Diagnostics.Debug.WriteLine("Task: {0} completed successfully", task);
            else
                System.Diagnostics.Debug.WriteLine("Task: {0} completed with error: {1}", task, error.LocalizedDescription);

            float progress = task.BytesReceived / (float)task.BytesExpectedToReceive;
            InvokeOnMainThread(() =>
            {

            });

            controller.downloadTask = null;
        }

        public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
        {
            System.Diagnostics.Debug.WriteLine("DidResume");
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

            System.Diagnostics.Debug.WriteLine("All tasks are finished");
        }
    }
}
