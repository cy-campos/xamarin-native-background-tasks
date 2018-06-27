using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

using System.Collections.Generic;

namespace TestApp.Droid
{
    public class DownloadManagerService
    {
        /// <summary>
        /// EventHandler that is invoked when the download has completed successfully
        /// </summary>
        public event EventHandler DownloadCompletedEventHandler;
        private void onDownloadCompleted(object sender, EventArgs e) { DownloadCompletedEventHandler?.Invoke(sender, e); }

        /// <summary>
        /// EventHandler that is invoked when the download has failed
        /// </summary>
        public event EventHandler DownloadFailedEventHandler;
        private void onDownloadFailed(object sender, EventArgs e) { DownloadFailedEventHandler?.Invoke(sender, e); }

        // We will just comment this out for now.. it might be nice to have later, but we can just catch a DownloadCancelledException to handle a cancel request
        /// <summary>
        /// EventHandler that is invoked when the download is cancelled
        /// </summary>
        //public event EventHandler DownloadCancelledEventHandler;
        //private void onDownloadCancelled(object sender, EventArgs e) { DownloadCancelledEventHandler?.Invoke(sender, e); }

        // Broadcaster Receiver
        private MyBroadcastReceiver downloadCompleteReceiver = new MyBroadcastReceiver();

        public long TotalSizeBytes { get; set; }
        public long TotalBytesDownloaded { get; set; }

        public int PercentageDownloaded { get { return _getPercentageDownloaded(); } }
        private int _getPercentageDownloaded()
        {
            if (TotalSizeBytes < 1)
                return 0;
            else
            {
                return (int)(((double)TotalBytesDownloaded / (double)TotalSizeBytes) * 100);
            }
        }


        /// <summary>
        /// The number of exceptions allowed before failure is reported
        /// </summary>
        /// <value>The max error limit.</value>
        public int MaxExceptionCount
        {
            get { return _maxExceptionCount; }
            set { _maxExceptionCount = value; }
        }
        private int _maxExceptionCount = 5;

        /// <summary>
        /// The number of errors that have occurred during the download process
        /// </summary>
        /// <value>The error count.</value>
        public int ExceptionCount { get { return _exceptionList.Count; } }

        // The list of exceptions that have occured during the download process
        public List<Exception> ExceptionList
        {
            get { return _exceptionList; }
        }
        private List<Exception> _exceptionList = new List<Exception>();

        public DownloadManagerService()
        {
            // Create an IntentFilter to listen for a specific broadcast; In this case, a completion event from DownloadManager
            // Not really used right now, but could come in handy if we need to handle broadcast messages from DownloadManager
            IntentFilter downloadCompleteIntentFilter = new IntentFilter(DownloadManager.ActionDownloadComplete);

            // register the broadcast receiver
            Application.Context.RegisterReceiver(downloadCompleteReceiver, downloadCompleteIntentFilter);
        }

        public async Task GetFileFromUrl(string url)
        {
            await GetFileFromUrl(url, null, null);
        }

        public async Task GetFileFromUrl(string url, Action<int> onPercentUpdate, object CancellationToken)
        {
            await Task.Run(() => { _download(url, onPercentUpdate, CancellationToken); });
        }

        private void _download(string url, Action<int> onPercentUpdate, object CancellationToken)
        {
            onPercentUpdate = onPercentUpdate ?? ((obj) => { });

            // handle object CancellationToken, since this can be null
            var cancelToken = CancellationToken != null ? (CancellationToken)CancellationToken : new CancellationToken();

            // check to see if cancelled first
            if (cancelToken.IsCancellationRequested)
                cancelToken.ThrowIfCancellationRequested();

            // create an Android.Net.Uri
            var uri = Android.Net.Uri.Parse(url);

            // get the downloadManager instance from Android
            var downloadManager = (DownloadManager)Android.App.Application.Context.GetSystemService(Context.DownloadService);

            // create a download request and queue it to the downloadManager
            var request = new DownloadManager.Request(uri);
            var downloadId = downloadManager.Enqueue(request);      // .Enqueue(request) returns the DownloadManager download Id

            // set a query filter to point to what we are downloading
            var query = new DownloadManager.Query();
            query.SetFilterById(downloadId);

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
                    TotalSizeBytes = result.GetLong(totalSizeIndex);

                    var totalDownloadedIndex = result.GetColumnIndex(DownloadManager.ColumnBytesDownloadedSoFar);
                    TotalBytesDownloaded = result.GetLong(totalDownloadedIndex);

                    onPercentUpdate(PercentageDownloaded);

                    System.Diagnostics.Debug.WriteLine(String.Format("Status: {0} | {1}% {2}/{3}", status, PercentageDownloaded, TotalBytesDownloaded, TotalSizeBytes));
                    
                    // Check for cancellation in loop
                    if (cancelToken.IsCancellationRequested)
                        cancelToken.ThrowIfCancellationRequested();
                    
                    // Sleeping is necessary because our query requests for data seem to operate faster than the DownloadManager
                    // can respond. For example, it can take a few seconds for the DB to be ready before we query it or after 
                    // a download completes (see the catch below), thus causing SQL exceptions and other weird behavior.
                    // The sleep offers us a little more time to ensure everything is ready before we query again.
                    Thread.Sleep(1000);
                }
                catch (Android.Database.CursorIndexOutOfBoundsException ex)
                {
                    // this happens when cancelled from the DownloadManager UI
                    if (this.PercentageDownloaded != 100)     // percentage check..just in case
                        throw new DownloadCancelledException("Cancelled", ex, cancelToken);
                    else
                        throw ex;
                }
                catch (OperationCanceledException ex)
                {
                    // this happens when cancellation is processed from within the app somewhere
                    throw new DownloadCancelledException("Cancelled", ex, cancelToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(String.Format("Error hit: {0}", ex.Message));

                    _exceptionList.Add(ex);

                    if (_exceptionList.Count > MaxExceptionCount)
                    {
                        System.Diagnostics.Debug.WriteLine(String.Format("The max number of exceptions allowed has been reached. Allowed: {0}", _maxExceptionCount));
                        onDownloadFailed(this, null);
                        return;
                    }

                    Thread.Sleep(5000);
                }
            }

            System.Diagnostics.Debug.WriteLine(String.Format("Exited gracefully. Errors: {0}", _exceptionList.Count));

            onDownloadCompleted(this, null);
        }
    }

    public class MyBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            System.Diagnostics.Debug.WriteLine(String.Format("MyBroadcastReceiver : OnReceive() | intent.Action: {0}", intent.Action));
        }
    }

    public class DownloadCancelledException : OperationCanceledException
    {
        public DownloadCancelledException(string message, Exception innerException, System.Threading.CancellationToken cancellationToken)
         : base(message, innerException, cancellationToken) { }
    }
}
