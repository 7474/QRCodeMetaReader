using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using ZXing;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace QRCodeMetaReader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IAppEvent
    {
        private MediaCapture mediaCapture;
        private DispatcherTimer captureTimer;

        public MainPage()
        {
            this.InitializeComponent();

            // https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/CameraGetPreviewFrame/cs/MainPage.xaml.cs
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wbitmap = await PickBarcodeImageFile();
                Decode(wbitmap);
                image.Source = wbitmap;
            }
            catch (Exception ex)
            {
                textBox.Text = ex.ToString();
            }
        }

        private async void buttonReal_Click(object sender, RoutedEventArgs e)
        {
            if (mediaCapture == null)
            {
                // XXX PCのインカメラではプロファイルの選択なんてできなかった
                var videoDeviceId = await GetVideoProfileSupportedDeviceIdAsync(Windows.Devices.Enumeration.Panel.Back);
                var profiles = MediaCapture.FindAllVideoProfiles(videoDeviceId);
                var selectedProfile = profiles
                    .SelectMany(profile => profile.SupportedPhotoMediaDescription.Select(photo => new { profile, photo }))
                    .OrderByDescending(x => x.photo.Height)
                    .FirstOrDefault();

                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings()
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    //VideoProfile = selectedProfile.profile,
                    //PhotoMediaDescription = selectedProfile.photo
                });
                captureElement.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                captureTimer = new DispatcherTimer();
                captureTimer.Interval = TimeSpan.FromSeconds(1);
                captureTimer.Tick += CaptureTimer_Tick;
            }
            captureTimer.Start();
        }

        // https://msdn.microsoft.com/windows/uwp/audio-video-camera/camera-profiles
        public async Task<string> GetVideoProfileSupportedDeviceIdAsync(Windows.Devices.Enumeration.Panel panel)
        {
            string deviceId = string.Empty;

            // Finds all video capture devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            foreach (var device in devices)
            {
                // Check if the device on the requested panel supports Video Profile
                //if (MediaCapture.IsVideoProfileSupported(device.Id) && device.EnclosureLocation.Panel == panel)
                {
                    // We've located a device that supports Video Profiles on expected panel
                    deviceId = device.Id;
                    break;
                }
            }

            return deviceId;
        }

        private async void CaptureTimer_Tick(object sender, object e)
        {
            try
            {
                //var frame = await mediaCapture.GetPreviewFrameAsync();
                var props = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.Photo) as VideoEncodingProperties;
                var stream = new InMemoryRandomAccessStream();
                var imageProps = ImageEncodingProperties.CreateBmp();
                imageProps.Width = props.Width;
                imageProps.Height = props.Height;
                await mediaCapture.CapturePhotoToStreamAsync(imageProps, stream);
                stream.Seek(0);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                var wbitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                stream.Seek(0);
                await wbitmap.SetSourceAsync(stream);

                if (Decode(wbitmap))
                {
                    captureTimer.Stop();
                }
                image.Source = wbitmap;
            }
            catch (Exception ex)
            {
                textBox.Text = ex.ToString();
            }
        }

        private async Task<WriteableBitmap> PickBarcodeImageFile()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");
            picker.FileTypeFilter.Add(".jpg");
            var file = await picker.PickSingleFileAsync();

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(file.OpenStreamForReadAsync().Result.AsRandomAccessStream());
            // XXX 二度読みがださい
            var wbitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight);
            await wbitmap.SetSourceAsync(file.OpenStreamForReadAsync().Result.AsRandomAccessStream());

            return wbitmap;
        }

        private bool Decode(WriteableBitmap wbitmap)
        {
            var barcodeReader = new BarcodeReader()
            {
                AutoRotate = true
            };
            var barcodeResult = barcodeReader.Decode(wbitmap);
            if (barcodeResult == null)
            {
                textBox.Text = "Decode failed.";
                return false;
            }
            else
            {
                var meta = string.Join(
                        Environment.NewLine,
                        barcodeResult.ResultMetadata
                            .Select(x => string.Format("{0}:{1}", x.Key, x.Value)));
                textBox.Text = string.Format(
@"---- ResultMetadata ----
{0}
----
BarcodeFormat:{3}
RawBytes.Count:{1}
Text:{2}", meta, barcodeResult.RawBytes.Count(), barcodeResult.Text, barcodeResult.BarcodeFormat);
                return true;
            }
        }

        public async Task OnSuspending(object sender, SuspendingEventArgs e)
        {
            captureElement.Source = null;
            image.Source = null;

            mediaCapture.Dispose();
            mediaCapture = null;
        }

        public async Task OnResuming(object sender, object e)
        {
            //
        }
    }
}
