using PrintServer.Core.Models;

namespace PrintServer.Core.Interfaces
{
    public interface IPrinterDriver
    {
        string DriverName { get; }
        string DriverVersion { get; }
        bool IsSupported { get; }
        
        Task<bool> InitializeAsync();
        Task<List<string>> GetAvailablePrintersAsync();
        Task<bool> TestConnectionAsync(string printerName);
        Task<bool> PrintAsync(PrintJob job);
        Task<bool> CancelJobAsync(string jobId);
        Task<PrinterStatus> GetPrinterStatusAsync(string printerName);
        Task DisposeAsync();
    }

    public class PrinterStatus
    {
        public bool IsOnline { get; set; }
        public bool IsReady { get; set; }
        public bool HasPaper { get; set; }
        public bool HasRibbon { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }
} 