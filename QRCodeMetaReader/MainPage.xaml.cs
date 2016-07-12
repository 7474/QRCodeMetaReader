using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
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
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await PickBarcodeImageFile();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            await PickBarcodeImageFile();
        }

        private async Task PickBarcodeImageFile()
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
            var barcodeReader = new BarcodeReader()
            {
                AutoRotate = true
            };
            var barcodeResult = barcodeReader.Decode(wbitmap);
            if (barcodeResult == null)
            {
                textBox.Text = "Decode failed.";
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
RawBytes.Count:{1}
Text:{2}", meta, barcodeResult.RawBytes.Count(), barcodeResult.Text);
            }
            image.Source = bitmap;
        }
    }
}
