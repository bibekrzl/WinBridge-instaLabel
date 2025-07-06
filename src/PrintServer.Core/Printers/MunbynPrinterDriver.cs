using PrintServer.Core.Interfaces;
using PrintServer.Core.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PrintServer.Core.Printers
{
    public class MunbynPrinterDriver : IPrinterDriver
    {
        private readonly ILogger<MunbynPrinterDriver> _logger;
        private SerialPort? _serialPort;
        private readonly Dictionary<string, SerialPort> _connectedPrinters = new();

        public string DriverName => "Munbyn Label Printer";
        public string DriverVersion => "1.0.0";
        public bool IsSupported => true;

        public MunbynPrinterDriver(ILogger<MunbynPrinterDriver> logger)
        {
            _logger = logger;
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing Munbyn printer driver");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Munbyn printer driver");
                return false;
            }
        }

        public async Task<List<string>> GetAvailablePrintersAsync()
        {
            var printers = new List<string>();
            
            try
            {
                // Get available COM ports
                var comPorts = SerialPort.GetPortNames();
                
                foreach (var port in comPorts)
                {
                    if (await TestConnectionAsync(port))
                    {
                        printers.Add($"Munbyn-{port}");
                    }
                }

                _logger.LogInformation("Found {Count} Munbyn printers", printers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available printers");
            }

            return printers;
        }

        public async Task<bool> TestConnectionAsync(string printerName)
        {
            try
            {
                var portName = ExtractPortName(printerName);
                if (string.IsNullOrEmpty(portName))
                    return false;

                using var testPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                testPort.ReadTimeout = 3000;
                testPort.WriteTimeout = 3000;
                
                testPort.Open();
                
                // Send initialization command
                var initCommand = new byte[] { 0x1B, 0x40 }; // ESC @
                testPort.Write(initCommand, 0, initCommand.Length);
                
                await Task.Delay(100);
                testPort.Close();
                
                _logger.LogInformation("Successfully tested connection to {PrinterName}", printerName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to test connection to {PrinterName}", printerName);
                return false;
            }
        }

        public async Task<bool> PrintAsync(PrintJob job)
        {
            try
            {
                _logger.LogInformation("Starting print job {JobId} on {PrinterName}", job.Id, job.Settings.PrinterName);
                
                var portName = ExtractPortName(job.Settings.PrinterName);
                if (string.IsNullOrEmpty(portName))
                {
                    throw new InvalidOperationException($"Invalid printer name: {job.Settings.PrinterName}");
                }

                using var port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                port.ReadTimeout = 10000;
                port.WriteTimeout = 10000;
                port.Open();

                // Initialize printer
                await SendCommand(port, new byte[] { 0x1B, 0x40 }); // ESC @
                await Task.Delay(100);

                // Set label size
                await SetLabelSize(port, job.Settings.Width, job.Settings.Height);

                // Print image
                await PrintImage(port, job.ImageData, job.Settings);

                // Feed and cut
                if (job.Settings.AutoCut)
                {
                    await SendCommand(port, new byte[] { 0x1D, 0x56, 0x41, 0x00 }); // GS V A
                }
                else
                {
                    await SendCommand(port, new byte[] { 0x0A, 0x0A, 0x0A }); // Feed
                }

                port.Close();
                
                _logger.LogInformation("Successfully completed print job {JobId}", job.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print job {JobId}", job.Id);
                return false;
            }
        }

        private async Task SetLabelSize(SerialPort port, int width, int height)
        {
            // Convert mm to dots (assuming 203 DPI)
            var dotsPerMm = 8; // 203 DPI / 25.4 mm
            var widthDots = width * dotsPerMm;
            var heightDots = height * dotsPerMm;

            // Set label width
            var widthBytes = BitConverter.GetBytes(widthDots);
            var widthCommand = new byte[] { 0x1D, 0x57, widthBytes[0], widthBytes[1] }; // GS W
            await SendCommand(port, widthCommand);

            // Set label height
            var heightBytes = BitConverter.GetBytes(heightDots);
            var heightCommand = new byte[] { 0x1D, 0x48, heightBytes[0], heightBytes[1] }; // GS H
            await SendCommand(port, heightCommand);
        }

        private async Task PrintImage(SerialPort port, byte[] imageData, PrintSettings settings)
        {
            using var stream = new MemoryStream(imageData);
            using var image = Image.FromStream(stream);
            
            // Resize image to fit label
            var targetWidth = settings.Width * 8; // Convert mm to dots
            var targetHeight = settings.Height * 8;
            
            using var resizedImage = ResizeImage(image, targetWidth, targetHeight);
            
            // Convert to black and white
            using var bwImage = ConvertToBlackAndWhite(resizedImage);
            
            // Convert to printer format
            var printerData = ConvertImageToPrinterFormat(bwImage);
            
            // Send image data
            await SendCommand(port, printerData);
        }

        private Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }

            return destImage;
        }

        private Bitmap ConvertToBlackAndWhite(Image image)
        {
            var bwImage = new Bitmap(image.Width, image.Height);
            
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var pixel = ((Bitmap)image).GetPixel(x, y);
                    var gray = (int)((pixel.R * 0.299) + (pixel.G * 0.587) + (pixel.B * 0.114));
                    var bw = gray < 128 ? Color.Black : Color.White;
                    bwImage.SetPixel(x, y, bw);
                }
            }
            
            return bwImage;
        }

        private byte[] ConvertImageToPrinterFormat(Bitmap image)
        {
            var width = image.Width;
            var height = image.Height;
            
            // ESC * command for bitmap printing
            var command = new List<byte> { 0x1B, 0x2A, 33 }; // ESC * 33
            
            // Width bytes (little endian)
            command.Add((byte)(width & 0xFF));
            command.Add((byte)((width >> 8) & 0xFF));
            
            // Height bytes (little endian)
            command.Add((byte)(height & 0xFF));
            command.Add((byte)((height >> 8) & 0xFF));
            
            // Image data
            for (int y = 0; y < height; y += 24)
            {
                for (int x = 0; x < width; x++)
                {
                    byte column = 0;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        if (y + bit < height)
                        {
                            var pixel = image.GetPixel(x, y + bit);
                            if (pixel.R < 128) // Black pixel
                            {
                                column |= (byte)(1 << (7 - bit));
                            }
                        }
                    }
                    command.Add(column);
                }
            }
            
            return command.ToArray();
        }

        private async Task SendCommand(SerialPort port, byte[] command)
        {
            port.Write(command, 0, command.Length);
            await Task.Delay(50); // Small delay between commands
        }

        public async Task<bool> CancelJobAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("Cancelling print job {JobId}", jobId);
                
                // Close all connections
                foreach (var printer in _connectedPrinters.Values)
                {
                    if (printer.IsOpen)
                    {
                        printer.Close();
                    }
                }
                _connectedPrinters.Clear();
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
                return false;
            }
        }

        public async Task<PrinterStatus> GetPrinterStatusAsync(string printerName)
        {
            try
            {
                var portName = ExtractPortName(printerName);
                if (string.IsNullOrEmpty(portName))
                {
                    return new PrinterStatus { IsOnline = false, ErrorMessage = "Invalid printer name" };
                }

                using var port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                port.ReadTimeout = 3000;
                port.WriteTimeout = 3000;
                
                port.Open();
                
                // Send status request command
                var statusCommand = new byte[] { 0x10, 0x04, 0x01 }; // DLE EOT SOH
                port.Write(statusCommand, 0, statusCommand.Length);
                
                await Task.Delay(100);
                
                var response = new byte[1];
                var bytesRead = port.Read(response, 0, 1);
                
                port.Close();
                
                if (bytesRead > 0)
                {
                    var status = response[0];
                    return new PrinterStatus
                    {
                        IsOnline = true,
                        IsReady = (status & 0x08) == 0x08,
                        HasPaper = (status & 0x20) == 0x20,
                        HasRibbon = (status & 0x40) == 0x40
                    };
                }
                
                return new PrinterStatus { IsOnline = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get printer status for {PrinterName}", printerName);
                return new PrinterStatus { IsOnline = false, ErrorMessage = ex.Message };
            }
        }

        public async Task DisposeAsync()
        {
            foreach (var printer in _connectedPrinters.Values)
            {
                if (printer.IsOpen)
                {
                    printer.Close();
                }
                printer.Dispose();
            }
            _connectedPrinters.Clear();
        }

        private string? ExtractPortName(string printerName)
        {
            if (printerName.StartsWith("Munbyn-"))
            {
                return printerName.Substring(7);
            }
            return null;
        }
    }
} 