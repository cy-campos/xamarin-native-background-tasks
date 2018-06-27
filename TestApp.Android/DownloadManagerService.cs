using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace TestApp.Droid
{
    public class DownloadManagerService
    {
        public event EventHandler DownloadCompletedEventHandler;
        private void onDownloadCompleted(object sender, EventArgs e) 
        {
            DownloadCompletedEventHandler?.Invoke(sender, e);
        }
        
        // the address of what to download
        public string DownloadUrlString { get; set; }

        // the path to where it should be saved
        const string DownloadPath = "";     // not implemented

        public DownloadManagerService(string url = "http://mirror.cessen.com/blender.org/peach/trailer/trailer_iphone.m4v")
        {
            this.DownloadUrlString = url;

            // Create an IntentFilter to listen for a specific broadcast; In this case, a completion event from DownloadManager
            IntentFilter downloadCompleteIntentFilter = new IntentFilter(DownloadManager.ActionDownloadComplete);

            // register the broadcast receiver
            Application.Context.RegisterReceiver(downloadCompleteReceiver, downloadCompleteIntentFilter);
        }

        public async Task getFileFromUrl()
        {
            //Func<Task> someTask = async () =>
            //{
            //    _download();
            //};

            //await someTask();

            await Task.Run(() => { _download(); });
        }
 
        private void _download()
        {
            // create an Android.Net.Uri
            var uri = Android.Net.Uri.Parse(DownloadUrlString);
            
            // get the downloadManager instance from Android
            var downloadManager = (DownloadManager)Android.App.Application.Context.GetSystemService(Context.DownloadService);

            // create a download request and queue it to the downloadManager
            var request = new DownloadManager.Request(uri);
            var downloadId = downloadManager.Enqueue(request);      // .Enqueue(request) returns the DownloadManager download Id

            // set a query filter to point to what we are downloading
            var query = new DownloadManager.Query();
            query.SetFilterById(downloadId);

            var errorCount = 0;
            var errorMax = 20;

            var isDownloading = true;

            // get status of the download here
            while (isDownloading)
            {
                try
                {
                    // Data from the downloadManager are not provided as typical C# properties.
                    // Android stores downloadManager info in a SQL table. To access it, we must query the table and grab the property info from each column we need
                    var result = downloadManager.InvokeQuery(query);
                    result.MoveToFirst();
                    var columnNames = result.GetColumnNames();

                    // Status column holds an integer value that maps to Android.App.DownloadStatus. Ref: https://developer.xamarin.com/api/type/Android.App.DownloadStatus/
                    // .Pending     = 1
                    // .Running     = 2
                    // .Paused      = 4
                    // .Successful  = 8
                    // .Failed      = 16
                    var statusIndex = result.GetColumnIndex(DownloadManager.ColumnStatus);
                    var status = result.GetInt(statusIndex);

                    if ((status == (int)Android.App.DownloadStatus.Successful) || (status == (int)Android.App.DownloadStatus.Failed))
                        isDownloading = false;

                    var totalSizeIndex = result.GetColumnIndex(DownloadManager.ColumnTotalSizeBytes);
                    var totalSize = result.GetLong(totalSizeIndex);

                    var totalDownloadedIndex = result.GetColumnIndex(DownloadManager.ColumnBytesDownloadedSoFar);
                    var totalDownloaded = result.GetLong(totalDownloadedIndex);

                    System.Diagnostics.Debug.WriteLine(String.Format("Status: {0} | {1}/{2}", status, totalDownloaded, totalSize));

                    // Sleeping is necessary because our query requests for data seem to operate faster than the DownloadManager
                    // can respond. For example, it can take a few seconds for the DB to be ready before we query it or after 
                    // a download completes (see the catch below), thus causing SQL exceptions and other weird behavior.
                    // The sleep offers us a little more time to ensure everything is ready before we query again.
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Error hit: {0}", ex.Message));

                    errorCount++;

                    if (errorCount > errorMax)
                        throw new Exception("Reached exception limit.", ex);

                    Thread.Sleep(5000);
                }
            }

            System.Diagnostics.Debug.WriteLine(String.Format("Exited gracefully. Errors: {0}", errorCount));
            
            onDownloadCompleted(this, null);

        }

        private MyBroadcastReceiver downloadCompleteReceiver = new MyBroadcastReceiver();
        private MyBroadcastReceiver downloadStatusReceiver = new MyBroadcastReceiver();
    }

    public class MyBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("MyBroadcastReceiver : OnReceive() | intent.Action: {0}", intent.Action));
        }
    }
}
