using System.Drawing;
using System.Drawing.Imaging;
using ZXing;

namespace CameraComApp
{
    public class QrCodeReader
    {
        private readonly BarcodeReader _barcodeReader;
        private const int MaxImageWidth = 640; // Resize image to reduce processing load

        public QrCodeReader()
        {
            _barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                }
            };
        }

        public string ReadQrCode(Bitmap image)
        {
            try
            {
                // Resize image to reduce processing load
              //  Bitmap resizedImage = ResizeImage(image, MaxImageWidth); //GIAM DUNG LUONG ANH
                Bitmap resizedImage = image;
                var result = _barcodeReader.Decode(resizedImage);
                resizedImage.Dispose();
                return result?.Text;
            }
            catch
            {
                return null;
            }
        }

        private Bitmap ResizeImage(Bitmap original, int maxWidth)
        {
            if (original.Width <= maxWidth)
                return new Bitmap(original);

            float ratio = (float)maxWidth / original.Width;
            int newHeight = (int)(original.Height * ratio);
            Bitmap resized = new Bitmap(maxWidth, newHeight, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, maxWidth, newHeight);
            }

            return resized;
        }
    }
}