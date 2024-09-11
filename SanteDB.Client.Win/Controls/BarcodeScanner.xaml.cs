using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Threading;
using Windows.Graphics.Imaging;
using Microsoft.UI.Dispatching;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SanteDB.Client.Win.Controls
{
#nullable disable

    public sealed partial class BarcodeScanner : UserControl
    {
        readonly SemaphoreSlim _CameraSemaphore;
        readonly DispatcherQueue _DQueue;

        private Popup _ParentPopup;


        public BarcodeScanner()
        {
            _CameraSemaphore = new SemaphoreSlim(1);

            this.InitializeComponent();

            _DQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            this.Loaded += BarcodeScanner_Loaded;
            this.Unloaded += BarcodeScanner_Unloaded;
        }

        /// <summary>
        /// Gets the resulting scanned barcode from the scanner. This will be null if the scan was cancelled.
        /// </summary>
        public string Result { get; private set; }

        private void BarcodeScanner_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            //this.Unloaded -= BarcodeScanner_Unloaded;
            this.UnloadCamera();
        }

        private void BarcodeScanner_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            //this.Loaded -= BarcodeScanner_Loaded;
            this.LoadCamera();
        }

        private async void UnloadCamera()
        {
            //_Timer.Stop();

            CameraPreview.CameraHelper.FrameArrived -= CameraHelper_FrameArrived;
            await CameraPreview.CameraHelper.CleanUpAsync();
            CameraPreview.Stop();
            CameraPreview.PreviewFailed -= CameraPreview_PreviewFailed;
        }

        private async void LoadCamera()
        {
            await _CameraSemaphore.WaitAsync();

            await CameraPreview.StartAsync();
            var result = await CameraPreview.CameraHelper.InitializeAndStartCaptureAsync();
            CameraPreview.PreviewFailed += CameraPreview_PreviewFailed;
            CameraPreview.CameraHelper.FrameArrived += CameraHelper_FrameArrived;

            //_Timer.Start();

            _CameraSemaphore.Release();

        }

        private void ClosePopup()
        {
            _ = _DQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                if (null != _ParentPopup)
                {
                    _ParentPopup.IsOpen = false;

                }
                else
                {
                    var parent = this.Parent as FrameworkElement;

                    while (parent != null)
                    {
                        if (parent is Popup popup)
                        {
                            _ParentPopup = popup;
                            popup.IsOpen = false;
                            break;
                        }

                        parent = parent.Parent as FrameworkElement;
                    }
                }
            });

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            ClosePopup();
        }

        private async void CameraHelper_FrameArrived(object sender, CommunityToolkit.WinUI.Helpers.FrameEventArgs e)
        {

            try
            {
                using var frame = e.VideoFrame;

                using var bitmap = frame.SoftwareBitmap;
                using var bitmap2 = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Rgba8);

                var reader = new ZXing.Windows.Compatibility.BarcodeReader();

                var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();

                var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.BmpEncoderId, stream);

                encoder.SetSoftwareBitmap(bitmap2);

                await encoder.FlushAsync();

                using var bmpstream = stream.AsStreamForRead();

                using var drawingbmp = System.Drawing.Bitmap.FromStream(bmpstream) as System.Drawing.Bitmap;

                var result = reader.Decode(drawingbmp);

                if (null != result)
                {
                    Result = result.Text;
                    ClosePopup();
                }
            }
            catch (Exception ex)
            {
                Result = null;
                ClosePopup();
            }

        }

        private void CameraPreview_PreviewFailed(object sender, CommunityToolkit.WinUI.Controls.PreviewFailedEventArgs e)
        {
            Result = null;
            ClosePopup();
        }
    }
}
