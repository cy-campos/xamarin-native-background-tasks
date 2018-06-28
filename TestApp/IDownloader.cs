using System;
namespace TestApp
{
    using System;
using System.Threading;
using System.Threading.Tasks;


using System.Collections.Generic;


    public interface IDownloader
    {
        /// <summary>
        /// EventHandler that is invoked when the download has completed successfully
        /// </summary>
        event EventHandler DownloadCompletedEventHandler;
        
        /// <summary>
        /// EventHandler that is invoked when the download has failed
        /// </summary>
        event EventHandler DownloadFailedEventHandler;
        

        // We will just comment this out for now.. it might be nice to have later, but we can just catch a DownloadCancelledException to handle a cancel request
        /// <summary>
        /// EventHandler that is invoked when the download is cancelled
        /// </summary>
        //public event EventHandler DownloadCancelledEventHandler;
        //private void onDownloadCancelled(object sender, EventArgs e) { DownloadCancelledEventHandler?.Invoke(sender, e); }

        long TotalSizeBytes { get; set; }
        long TotalBytesDownloaded { get; set; }

        int PercentageDownloaded { get; }

        Task GetFileFromUrl(string url);

        Task GetFileFromUrl(string url, Action<int> onPercentUpdate, object CancellationToken);
    }
}


