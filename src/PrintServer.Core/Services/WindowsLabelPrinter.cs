using System;
using System.Drawing;
using System.Drawing.Printing;

namespace PrintServer.Core.Services
{
    public static class WindowsLabelPrinter
    {
        public static void PrintLabel(Bitmap labelImage, string printerName, int widthMm, int heightMm)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrinterSettings.PrinterName = printerName;
            pd.DefaultPageSettings.Landscape = false;
            pd.DefaultPageSettings.PaperSize = new PaperSize("Custom", MmToHundredthsInch(widthMm), MmToHundredthsInch(heightMm));
            pd.PrintPage += (sender, e) =>
            {
                // Center the image on the page
                var bounds = e.MarginBounds;
                var x = bounds.X + (bounds.Width - labelImage.Width) / 2;
                var y = bounds.Y + (bounds.Height - labelImage.Height) / 2;
                e.Graphics.DrawImage(labelImage, x, y, labelImage.Width, labelImage.Height);
            };
            pd.Print();
        }

        private static int MmToHundredthsInch(int mm)
        {
            // 1 inch = 25.4 mm, 1 hundredth inch = 0.254 mm
            return (int)(mm / 0.254);
        }
    }
} 