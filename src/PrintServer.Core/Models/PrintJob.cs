using System.Drawing;

namespace PrintServer.Core.Models
{
    public class PrintJob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string JobName { get; set; } = string.Empty;
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string ImageFormat { get; set; } = "PNG";
        public PrintSettings Settings { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;
        public string? ErrorMessage { get; set; }
    }

    public class PrintSettings
    {
        public string PrinterName { get; set; } = string.Empty;
        public int Copies { get; set; } = 1;
        public int Width { get; set; } = 100; // mm
        public int Height { get; set; } = 50; // mm
        public bool AutoCut { get; set; } = true;
        public string? LabelType { get; set; }
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public enum PrintJobStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
} 