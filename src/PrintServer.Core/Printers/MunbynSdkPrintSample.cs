using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
// using Munbyn.SDK; // Uncomment and add reference to Munbyn SDK DLL

namespace PrintServer.Core.Printers
{
    public class MunbynSdkPrintSample
    {
        // Path to the Munbyn SDK DLL (update as needed)
        // private const string SdkDllPath = "../libs/MunbynSDK.dll";

        public static void PrintSampleLabel()
        {
            // 56mm x 31mm at 203 DPI (8 dots/mm)
            int widthMm = 56;
            int heightMm = 31;
            int dpi = 203;
            int widthPx = (int)(widthMm * dpi / 25.4);
            int heightPx = (int)(heightMm * dpi / 25.4);

            // Generate a sample image
            using var bmp = new Bitmap(widthPx, heightPx);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                using var pen = new Pen(Color.Black, 4);
                g.DrawRectangle(pen, 0, 0, widthPx - 1, heightPx - 1);
                using var font = new Font("Arial", 24, FontStyle.Bold);
                var text = $"56mm x 31mm\n{DateTime.Now:HH:mm:ss}";
                var textSize = g.MeasureString(text, font);
                g.DrawString(text, font, Brushes.Black, (widthPx - textSize.Width) / 2, (heightPx - textSize.Height) / 2);
            }

            // Save to temp file for SDK
            var tempPath = Path.Combine(Path.GetTempPath(), $"munbyn_label_{Guid.NewGuid()}.bmp");
            bmp.Save(tempPath, ImageFormat.Bmp);

            // --- Munbyn SDK Print Call (pseudo-code) ---
            // var printer = new MunbynPrinter();
            // printer.Open("COM3"); // Or use the correct port
            // printer.PrintImage(tempPath);
            // printer.Close();
            // ------------------------------------------

            Console.WriteLine($"Sample label image saved to: {tempPath}");
            Console.WriteLine("[Munbyn SDK print call would be made here]");
        }
    }
} 