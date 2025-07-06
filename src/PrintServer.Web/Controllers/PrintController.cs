using Microsoft.AspNetCore.Mvc;
using PrintServer.Core.Interfaces;
using PrintServer.Core.Models;

namespace PrintServer.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintController : ControllerBase
    {
        private readonly IPrintService _printService;
        private readonly ILogger<PrintController> _logger;

        public PrintController(IPrintService printService, ILogger<PrintController> logger)
        {
            _printService = printService;
            _logger = logger;
        }

        [HttpPost("print")]
        public async Task<IActionResult> Print([FromForm] PrintRequest request)
        {
            try
            {
                if (request.ImageFile == null || request.ImageFile.Length == 0)
                {
                    return BadRequest("No image file provided");
                }

                if (string.IsNullOrEmpty(request.PrinterName))
                {
                    return BadRequest("Printer name is required");
                }

                // Read image data
                using var memoryStream = new MemoryStream();
                await request.ImageFile.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                // Create print job
                var printJob = new PrintJob
                {
                    JobName = request.JobName ?? $"Print Job {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    ImageData = imageData,
                    ImageFormat = Path.GetExtension(request.ImageFile.FileName).TrimStart('.') ?? "PNG",
                    Settings = new PrintSettings
                    {
                        PrinterName = request.PrinterName,
                        Copies = request.Copies ?? 1,
                        Width = request.Width ?? 100,
                        Height = request.Height ?? 50,
                        AutoCut = request.AutoCut ?? true,
                        LabelType = request.LabelType
                    }
                };

                // Submit print job
                var result = await _printService.SubmitPrintJobAsync(printJob);

                return Ok(new
                {
                    JobId = result.Id,
                    Status = result.Status,
                    Message = result.Status == PrintJobStatus.Completed ? "Print job submitted successfully" : result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing print request");
                return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
            }
        }

        [HttpGet("jobs")]
        public async Task<IActionResult> GetPrintJobs()
        {
            try
            {
                var jobs = await _printService.GetPrintJobsAsync();
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting print jobs");
                return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
            }
        }

        [HttpGet("jobs/{jobId}")]
        public async Task<IActionResult> GetPrintJob(string jobId)
        {
            try
            {
                var job = await _printService.GetPrintJobAsync(jobId);
                if (job == null)
                {
                    return NotFound(new { Error = "Print job not found" });
                }

                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting print job {JobId}", jobId);
                return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
            }
        }

        [HttpPost("jobs/{jobId}/cancel")]
        public async Task<IActionResult> CancelPrintJob(string jobId)
        {
            try
            {
                var success = await _printService.CancelPrintJobAsync(jobId);
                if (!success)
                {
                    return BadRequest(new { Error = "Failed to cancel print job" });
                }

                return Ok(new { Message = "Print job cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling print job {JobId}", jobId);
                return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
            }
        }

        [HttpGet("printers")]
        public async Task<IActionResult> GetAvailablePrinters()
        {
            try
            {
                var printers = await _printService.GetAvailablePrintersAsync();
                return Ok(printers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available printers");
                return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
            }
        }

        [HttpGet("printers/{printerName}/status")]
        public async Task<IActionResult> GetPrinterStatus(string printerName)
        {
            try
            {
                var status = await _printService.GetPrinterStatusAsync(printerName);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printer status for {PrinterName}", printerName);
                return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
            }
        }

        [HttpPost("printers/{printerName}/test")]
        public async Task<IActionResult> TestPrinter(string printerName)
        {
            try
            {
                var success = await _printService.TestPrinterAsync(printerName);
                return Ok(new { Success = success, Message = success ? "Printer test successful" : "Printer test failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing printer {PrinterName}", printerName);
                return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
            }
        }
    }

    public class PrintRequest
    {
        public IFormFile? ImageFile { get; set; }
        public string? JobName { get; set; }
        public string? PrinterName { get; set; }
        public int? Copies { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public bool? AutoCut { get; set; }
        public string? LabelType { get; set; }
    }
} 