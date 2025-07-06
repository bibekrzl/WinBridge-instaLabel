using PrintServer.Core.Models;

namespace PrintServer.Core.Interfaces
{
    public interface IPrintService
    {
        Task<PrintJob> SubmitPrintJobAsync(PrintJob job);
        Task<PrintJob?> GetPrintJobAsync(string jobId);
        Task<List<PrintJob>> GetPrintJobsAsync();
        Task<bool> CancelPrintJobAsync(string jobId);
        Task<List<string>> GetAvailablePrintersAsync();
        Task<PrinterStatus> GetPrinterStatusAsync(string printerName);
        Task<bool> TestPrinterAsync(string printerName);
    }
} 