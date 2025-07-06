using PrintServer.Core.Interfaces;
using PrintServer.Core.Models;
using PrintServer.Core.Printers;
using Microsoft.Extensions.Logging;

namespace PrintServer.Core.Services
{
    public class PrintService : IPrintService
    {
        private readonly ILogger<PrintService> _logger;
        private readonly List<IPrinterDriver> _printerDrivers;
        private readonly Dictionary<string, PrintJob> _printJobs = new();
        private readonly Dictionary<string, IPrinterDriver> _printerDriverMap = new();

        public PrintService(ILogger<PrintService> logger, IEnumerable<IPrinterDriver> printerDrivers)
        {
            _logger = logger;
            _printerDrivers = printerDrivers.ToList();
        }

        public async Task<PrintJob> SubmitPrintJobAsync(PrintJob job)
        {
            try
            {
                _logger.LogInformation("Submitting print job {JobId} for printer {PrinterName}", 
                    job.Id, job.Settings.PrinterName);

                // Store the job
                _printJobs[job.Id] = job;
                job.Status = PrintJobStatus.Processing;

                // Find the appropriate printer driver
                var driver = await GetPrinterDriverForPrinter(job.Settings.PrinterName);
                if (driver == null)
                {
                    job.Status = PrintJobStatus.Failed;
                    job.ErrorMessage = $"No driver found for printer: {job.Settings.PrinterName}";
                    return job;
                }

                // Execute the print job
                var success = await driver.PrintAsync(job);
                
                if (success)
                {
                    job.Status = PrintJobStatus.Completed;
                    _logger.LogInformation("Print job {JobId} completed successfully", job.Id);
                }
                else
                {
                    job.Status = PrintJobStatus.Failed;
                    job.ErrorMessage = "Print operation failed";
                    _logger.LogError("Print job {JobId} failed", job.Id);
                }

                return job;
            }
            catch (Exception ex)
            {
                job.Status = PrintJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error submitting print job {JobId}", job.Id);
                return job;
            }
        }

        public async Task<PrintJob?> GetPrintJobAsync(string jobId)
        {
            _printJobs.TryGetValue(jobId, out var job);
            return job;
        }

        public async Task<List<PrintJob>> GetPrintJobsAsync()
        {
            return _printJobs.Values.OrderByDescending(j => j.CreatedAt).ToList();
        }

        public async Task<bool> CancelPrintJobAsync(string jobId)
        {
            try
            {
                if (!_printJobs.TryGetValue(jobId, out var job))
                {
                    _logger.LogWarning("Print job {JobId} not found", jobId);
                    return false;
                }

                if (job.Status == PrintJobStatus.Completed || job.Status == PrintJobStatus.Failed)
                {
                    _logger.LogWarning("Cannot cancel completed or failed job {JobId}", jobId);
                    return false;
                }

                // Find the printer driver and cancel the job
                var driver = await GetPrinterDriverForPrinter(job.Settings.PrinterName);
                if (driver != null)
                {
                    await driver.CancelJobAsync(jobId);
                }

                job.Status = PrintJobStatus.Cancelled;
                _logger.LogInformation("Print job {JobId} cancelled successfully", jobId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling print job {JobId}", jobId);
                return false;
            }
        }

        public async Task<List<string>> GetAvailablePrintersAsync()
        {
            var allPrinters = new List<string>();

            foreach (var driver in _printerDrivers)
            {
                try
                {
                    if (driver.IsSupported)
                    {
                        var printers = await driver.GetAvailablePrintersAsync();
                        allPrinters.AddRange(printers);
                        
                        // Map printers to drivers
                        foreach (var printer in printers)
                        {
                            _printerDriverMap[printer] = driver;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting printers from driver {DriverName}", driver.DriverName);
                }
            }

            _logger.LogInformation("Found {Count} total printers", allPrinters.Count);
            return allPrinters;
        }

        public async Task<PrinterStatus> GetPrinterStatusAsync(string printerName)
        {
            try
            {
                var driver = await GetPrinterDriverForPrinter(printerName);
                if (driver == null)
                {
                    return new PrinterStatus 
                    { 
                        IsOnline = false, 
                        ErrorMessage = $"No driver found for printer: {printerName}" 
                    };
                }

                return await driver.GetPrinterStatusAsync(printerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting printer status for {PrinterName}", printerName);
                return new PrinterStatus 
                { 
                    IsOnline = false, 
                    ErrorMessage = ex.Message 
                };
            }
        }

        public async Task<bool> TestPrinterAsync(string printerName)
        {
            try
            {
                var driver = await GetPrinterDriverForPrinter(printerName);
                if (driver == null)
                {
                    _logger.LogWarning("No driver found for printer {PrinterName}", printerName);
                    return false;
                }

                return await driver.TestConnectionAsync(printerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing printer {PrinterName}", printerName);
                return false;
            }
        }

        private async Task<IPrinterDriver?> GetPrinterDriverForPrinter(string printerName)
        {
            // Check if we already have a mapping for this printer
            if (_printerDriverMap.TryGetValue(printerName, out var driver))
            {
                return driver;
            }

            // Try to find a driver that supports this printer
            foreach (var printerDriver in _printerDrivers)
            {
                if (printerDriver.IsSupported)
                {
                    try
                    {
                        var printers = await printerDriver.GetAvailablePrintersAsync();
                        if (printers.Contains(printerName))
                        {
                            _printerDriverMap[printerName] = printerDriver;
                            return printerDriver;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking printer support in driver {DriverName}", 
                            printerDriver.DriverName);
                    }
                }
            }

            return null;
        }
    }
} 