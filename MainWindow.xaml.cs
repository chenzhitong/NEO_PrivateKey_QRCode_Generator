using Gma.QrCodeNet.Encoding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NEO_PrivateKey_QRCode_Generator
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int.TryParse(Count.Text, out int count);
            byte[] privateKey = new byte[32];
            for (int i = 0; i < count; i++)
            {
                using (CngKey key = CngKey.Create(CngAlgorithm.ECDsaP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextArchiving }))
                {
                    privateKey = key.Export(CngKeyBlobFormat.EccPrivateBlob);
                }
                Generate(privateKey);
            }
            MessageBox.Show("生成完毕");
        }

        public void Generate(byte[] privateKey)
        {
            var account = new Neo.Wallets.KeyPair(privateKey);
            var contract = Neo.SmartContract.Contract.CreateSignatureContract(account.PublicKey);
            var address = contract.Address;
            var wif = account.Export();
            File.AppendAllText("export.txt", $"{address}\t{wif}\r\n", Encoding.Default);

            QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.M);
            QrCode qrCode = qrEncoder.Encode(wif);

            int w = (int)Math.Sqrt(qrCode.Matrix.InternalArray.Length);
            bool[,] qr = Zoom(qrCode.Matrix.InternalArray, 10, w);
            w = (int)Math.Sqrt(qr.Length);

            byte[] data = new byte[qr.Length];

            for (int j = 0; j < w; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    data[j * w + i] = (byte)(qr[i, j] ? 0 : 255);
                }
            }
            var dpi = 144;

            BitmapSource qrBitSource = BitmapSource.Create(w, w, dpi, dpi, PixelFormats.Gray8, null, data, w);
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(new SolidColorBrush(Colors.White), new Pen(new SolidColorBrush(Colors.White), 0), new Rect(new Point(0, 0), new Size(1000, 1000)));
                drawingContext.DrawText(
                    new FormattedText(
                        address,
                        System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("微软雅黑"), FontStyles.Normal, FontWeights.Regular, FontStretches.Normal),
                        9, new SolidColorBrush(Colors.Black)
                    ),
                    new Point(30, 10));

                //图片-二维码
                drawingContext.DrawImage(qrBitSource, new Rect(new Point(30, 30), new Size(200, 200)));
            }
            try
            {
                RenderTargetBitmap bmp = new RenderTargetBitmap(390, 390, dpi, dpi, PixelFormats.Pbgra32);
                bmp.Render(drawingVisual);
                //canvas1.Children.Add(new Image());
                //(canvas1.Children[0] as Image).Source = bmp;
                var fileName = $"code/{address}.jpg";
                Save(bmp, fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private bool[,] Zoom(bool[,] input, int X, int width)
        {
            int newWinth = width * X;
            bool[,] result = new bool[newWinth, newWinth];
            for (int i = 0; i < newWinth; i++)
                for (int j = 0; j < newWinth; j++)
                    result[i, j] = input[i * width / newWinth, j * width / newWinth];
            return result;
        }

        public static void Save(BitmapSource bitmapSource, string fileName)
        {
            BitmapEncoder bitmapEncoder = new JpegBitmapEncoder();
            using (Stream stream = File.Create(fileName))
            {
                bitmapEncoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                bitmapEncoder.Save(stream);
            }
        }
    }
}
