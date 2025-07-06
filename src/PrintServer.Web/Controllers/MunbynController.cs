using Microsoft.AspNetCore.Mvc;
using PrintServer.Core.Printers;
using PrintServer.Core.Services;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;

namespace PrintServer.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MunbynController : ControllerBase
    {
        private readonly ILogger<MunbynController> _logger;

        public MunbynController(ILogger<MunbynController> logger)
        {
            _logger = logger;
        }

        [HttpPost("print-sample")]
        public IActionResult PrintSampleLabel([FromQuery] string? printerName = null)
        {
            try
            {
                _logger.LogInformation("Starting Munbyn sample label print");

                // Find Munbyn printer if not specified
                if (string.IsNullOrWhiteSpace(printerName))
                {
                    var installedPrinters = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
                    printerName = installedPrinters.FirstOrDefault(p => p.ToLower().Contains("munbyn"));
                    if (printerName == null)
                        return BadRequest(new { Success = false, Error = "No Munbyn printer found. Please specify a printer name." });
                }

                // 56mm x 31mm at 203 DPI
                int widthMm = 56;
                int heightMm = 31;
                int dpi = 203;
                int widthPx = (int)(widthMm * dpi / 25.4);
                int heightPx = (int)(heightMm * dpi / 25.4);

                // Generate sample image
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

                // Print using Windows print system
                WindowsLabelPrinter.PrintLabel(bmp, printerName, widthMm, heightMm);

                return Ok(new {
                    Success = true,
                    Message = $"Sample label print sent to {printerName}",
                    LabelSize = "56mm x 31mm",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing Munbyn sample label");
                return StatusCode(500, new {
                    Success = false,
                    Error = "Failed to print sample label",
                    Message = ex.Message
                });
            }
        }

        [HttpPost("print-custom")]
        public IActionResult PrintCustomLabel([FromBody] CustomLabelRequest request)
        {
            try
            {
                _logger.LogInformation("Starting Munbyn custom label print: {Text}", request.Text);
                
                // TODO: Implement custom label printing with Munbyn SDK
                // This will be implemented once the SDK is integrated
                
                return Ok(new { 
                    Success = true, 
                    Message = "Custom label print initiated successfully",
                    Text = request.Text,
                    LabelSize = "56mm x 31mm",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing Munbyn custom label");
                return StatusCode(500, new { 
                    Success = false, 
                    Error = "Failed to print custom label", 
                    Message = ex.Message 
                });
            }
        }

        [HttpGet("status")]
        public IActionResult GetPrinterStatus()
        {
            try
            {
                // TODO: Implement printer status check with Munbyn SDK
                return Ok(new { 
                    PrinterType = "Munbyn",
                    Status = "Ready",
                    LabelSize = "56mm x 31mm",
                    LastCheck = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Munbyn printer status");
                return StatusCode(500, new { 
                    Success = false, 
                    Error = "Failed to get printer status", 
                    Message = ex.Message 
                });
            }
        }
    }

    public class CustomLabelRequest
    {
        public string Text { get; set; } = "Sample Text";
        public string? ComPort { get; set; } = "COM3";
    }
} 