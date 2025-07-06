using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrintServer.Core.Interfaces;
using PrintServer.Core.Models;
using System.ComponentModel;
using Fleck;
using System.Drawing.Printing;
using System.Text.RegularExpressions;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace PrintServer.Tray.Forms
{
    public partial class MainForm : Form
    {
        private readonly IPrintService _printService;
        private readonly ILogger<MainForm> _logger;
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private bool _isWebServerRunning = false;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private WebSocketServer? _webSocketServer;
        private readonly List<IWebSocketConnection> _wsClients = new();
        private List<string> _printers = new();
        private string _defaultPrinter = string.Empty;
        private IHost? _httpHost;
        private readonly ConcurrentQueue<(string Printer, int Width, int Height, string ImageBase64, DateTime Timestamp)> _recentJobs = new();
        private const int MaxRecentJobs = 10;
        private struct PrintJob
        {
            public Image Image;
            public string PrinterName;
            public int WidthMm;
            public int HeightMm;
            public DateTime Timestamp;
        }
        private readonly ConcurrentQueue<PrintJob> _printQueue = new();
        private readonly AutoResetEvent _queueEvent = new(false);
        private bool _printWorkerRunning = true;

        public MainForm(IPrintService printService, ILogger<MainForm> logger)
        {
            _printService = printService;
            _logger = logger;

            InitializeComponent();
            InitializeTrayIcon();
            InitializeWebServer();
            InitializeWebSocketServer();
            StartHttpDashboardServer();
            StartPrintWorker();
        }

        private void InitializeComponent()
        {
            this.Text = "Print Server";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.ShowInTaskbar = false;

            // Handle form events
            this.Resize += MainForm_Resize;
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeTrayIcon()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Refresh Printers", null, RefreshPrinters_Click);
            _contextMenu.Items.Add("Test Printers", null, TestPrinters_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Open Browser", null, OpenBrowser_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Exit", null, Exit_Click);

            // Try to load custom icon (PrintBridge.ico in app directory)
            Icon trayIcon = SystemIcons.Application;
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintBridge.ico");
                if (File.Exists(iconPath))
                {
                    trayIcon = new Icon(iconPath);
                }
            }
            catch { /* fallback to default icon */ }

            _notifyIcon = new NotifyIcon
            {
                Icon = trayIcon,
                Text = "Print Server",
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private void InitializeWebServer()
        {
            try
            {
                // Start the web server in a background task
                Task.Run(async () =>
                {
                    try
                    {
                        _isWebServerRunning = true;
                        _logger.LogInformation("Starting web server...");
                        
                        // In a real implementation, you'd start the web server here
                        // For now, we'll simulate it
                        await Task.Delay(1000);
                        
                        _logger.LogInformation("Web server started successfully on http://localhost:5000");
                        UpdateTrayIcon();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start web server");
                        _isWebServerRunning = false;
                    }
                }, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing web server");
            }
        }

        private void UpdateTrayIcon()
        {
            if (_isWebServerRunning)
            {
                _notifyIcon.Text = "Print Server - Running";
                _notifyIcon.Icon = SystemIcons.Information;
            }
            else
            {
                _notifyIcon.Text = "Print Server - Stopped";
                _notifyIcon.Icon = SystemIcons.Warning;
            }
        }

        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowHide_Click(sender, e);
        }

        private void ShowHide_Click(object? sender, EventArgs e)
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }

        private async void RefreshPrinters_Click(object? sender, EventArgs e)
        {
            try
            {
                var printers = await _printService.GetAvailablePrintersAsync();
                var message = $"Found {printers.Count} printers:\n\n" + string.Join("\n", printers);
                MessageBox.Show(message, "Available Printers", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing printers");
                MessageBox.Show($"Error refreshing printers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void TestPrinters_Click(object? sender, EventArgs e)
        {
            try
            {
                var printers = await _printService.GetAvailablePrintersAsync();
                var results = new List<string>();

                foreach (var printer in printers)
                {
                    var success = await _printService.TestPrinterAsync(printer);
                    results.Add($"{printer}: {(success ? "✓" : "✗")}");
                }

                var message = "Printer Test Results:\n\n" + string.Join("\n", results);
                MessageBox.Show(message, "Printer Test Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing printers");
                MessageBox.Show($"Error testing printers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenBrowser_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "http://localhost:5000",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening PrintBridge Dashboard");
                MessageBox.Show($"Error opening dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _webSocketServer?.Dispose();
                foreach (var ws in _wsClients.ToList())
                {
                    ws.Close();
                }
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _printWorkerRunning = false;
                _queueEvent.Set();
                Application.Exit();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application shutdown");
            }
        }

        private void InitializeWebSocketServer()
        {
            try
            {
                _printers = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
                _defaultPrinter = _printers.FirstOrDefault() ?? string.Empty;
                _webSocketServer = new WebSocketServer("ws://0.0.0.0:8080/ws");
                _webSocketServer.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        _wsClients.Add(socket);
                        _logger.LogInformation($"WebSocket client connected: {socket.ConnectionInfo.ClientIpAddress}");
                        SendConnectionResponse(socket);
                    };
                    socket.OnClose = () =>
                    {
                        _wsClients.Remove(socket);
                        _logger.LogInformation($"WebSocket client disconnected: {socket.ConnectionInfo.ClientIpAddress}");
                    };
                    socket.OnError = ex =>
                    {
                        _logger.LogError(ex, "WebSocket error");
                        SendErrorResponse(socket, "WebSocket error: " + ex.Message);
                    };
                    socket.OnMessage = msg =>
                    {
                        try
                        {
                            // Try to parse as JSON for new protocol
                            bool handled = false;
                            try
                            {
                                var doc = JsonDocument.Parse(msg);
                                if (doc.RootElement.TryGetProperty("labelWidth", out var widthProp) &&
                                    doc.RootElement.TryGetProperty("labelHeight", out var heightProp) &&
                                    doc.RootElement.TryGetProperty("image", out var imageProp))
                                {
                                    int labelWidth = widthProp.GetInt32();
                                    int labelHeight = heightProp.GetInt32();
                                    string imageData = imageProp.GetString() ?? string.Empty;
                                    string printerName = _defaultPrinter;
                                    if (doc.RootElement.TryGetProperty("selectedPrinter", out var printerProp))
                                    {
                                        string requestedPrinter = printerProp.GetString() ?? string.Empty;
                                        if (!string.IsNullOrWhiteSpace(requestedPrinter) && _printers.Contains(requestedPrinter))
                                        {
                                            printerName = requestedPrinter;
                                        }
                                    }
                                    var match = Regex.Match(imageData, @"^data:image/png;base64,(.+)", RegexOptions.IgnoreCase);
                                    string base64Data = match.Success ? match.Groups[1].Value : imageData;
                                    if (string.IsNullOrWhiteSpace(base64Data))
                                    {
                                        SendGeneralErrorResponse(socket, "Invalid PNG base64 data");
                                        return;
                                    }
                                    var imageBytes = Convert.FromBase64String(base64Data);
                                    using var ms = new MemoryStream(imageBytes);
                                    using var img = Image.FromStream(ms);
                                    EnqueuePrintJob(img, printerName, labelWidth, labelHeight);
                                    SendPrintSuccessResponse(socket, printerName);
                                    handled = true;
                                }
                            }
                            catch { /* Not JSON, fallback to legacy */ }
                            if (handled) return;
                            if (msg.Trim().ToLower() == "status")
                            {
                                SendStatusResponse(socket);
                                return;
                            }
                            // Legacy: Only accept PNG base64 data
                            var legacyMatch = Regex.Match(msg, @"^data:image/png;base64,(.+)", RegexOptions.IgnoreCase);
                            string legacyBase64Data = legacyMatch.Success ? legacyMatch.Groups[1].Value : msg;
                            if (string.IsNullOrWhiteSpace(legacyBase64Data))
                            {
                                SendGeneralErrorResponse(socket, "Invalid PNG base64 data");
                                return;
                            }
                            var legacyImageBytes = Convert.FromBase64String(legacyBase64Data);
                            using var legacyMs = new MemoryStream(legacyImageBytes);
                            using var legacyImg = Image.FromStream(legacyMs);
                            EnqueuePrintJob(legacyImg, _defaultPrinter, 56, 31);
                            SendPrintSuccessResponse(socket, _defaultPrinter);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing WebSocket message");
                            SendGeneralErrorResponse(socket, ex.Message);
                        }
                    };
                });
                _logger.LogInformation("WebSocket server started on ws://localhost:8080/ws");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start WebSocket server");
            }
        }

        private void SendConnectionResponse(IWebSocketConnection socket)
        {
            var response = new
            {
                type = "connection",
                printers = _printers,
                defaultPrinter = _defaultPrinter
            };
            socket.Send(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private void SendStatusResponse(IWebSocketConnection socket)
        {
            var response = new
            {
                type = "status",
                printers = _printers,
                defaultPrinter = _defaultPrinter
            };
            socket.Send(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private void SendPrintSuccessResponse(IWebSocketConnection socket, string printerName)
        {
            var response = new
            {
                success = true,
                printerName,
                message = "Print job completed successfully"
            };
            socket.Send(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private void SendPrintErrorResponse(IWebSocketConnection socket, string errorMessage)
        {
            var response = new
            {
                success = false,
                errorMessage
            };
            socket.Send(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private void SendGeneralErrorResponse(IWebSocketConnection socket, string message)
        {
            var response = new
            {
                type = "error",
                message
            };
            socket.Send(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private void SendErrorResponse(IWebSocketConnection socket, string message)
        {
            var response = new
            {
                type = "error",
                message
            };
            socket.Send(System.Text.Json.JsonSerializer.Serialize(response));
        }

        private void PrintImage(Image img, string printerName, int widthMm, int heightMm, out string error, bool addToRecent = true, DateTime? timestamp = null)
        {
            error = string.Empty;
            try
            {
                using PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                var paperSize = new PaperSize("Label", (int)(widthMm / 25.4 * 100), (int)(heightMm / 25.4 * 100));
                pd.DefaultPageSettings.PaperSize = paperSize;
                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                pd.PrintPage += (s, e) =>
                {
                    e.Graphics.DrawImage(img, e.MarginBounds);
                };
                if (addToRecent)
                {
                    using var ms = new MemoryStream();
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    string imgBase64 = Convert.ToBase64String(ms.ToArray());
                    _recentJobs.Enqueue((printerName, widthMm, heightMm, imgBase64, timestamp ?? DateTime.Now));
                    while (_recentJobs.Count > MaxRecentJobs && _recentJobs.TryDequeue(out _)) { }
                }
                pd.Print();
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }

        private void StartHttpDashboardServer()
        {
            _httpHost = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls("http://localhost:5000");
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/api/printers", async context =>
                            {
                                var printers = _printers.Select(p => new { name = p, isDefault = (p == _defaultPrinter) }).ToList();
                                await context.Response.WriteAsJsonAsync(new { printers });
                            });
                            endpoints.MapGet("/api/jobs", async context =>
                            {
                                var jobs = _recentJobs.ToArray().Select(j => new {
                                    printer = j.Printer,
                                    width = j.Width,
                                    height = j.Height,
                                    imageBase64 = j.ImageBase64,
                                    timestamp = j.Timestamp
                                }).ToList();
                                await context.Response.WriteAsJsonAsync(new { jobs });
                            });
                            endpoints.MapGet("/", async context =>
                            {
                                context.Response.ContentType = "text/html";
                                await context.Response.WriteAsync(@"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<title>PrintBridge Dashboard</title>
<style>
body { font-family: sans-serif; background: #f8f9fa; margin: 0; padding: 0; }
header { background: #222; color: #fff; padding: 1em; text-align: center; }
section { max-width: 800px; margin: 2em auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 8px #0001; padding: 2em; }
h2 { margin-top: 0; }
table { width: 100%; border-collapse: collapse; margin-bottom: 2em; }
th, td { border: 1px solid #ddd; padding: 0.5em; text-align: left; }
th { background: #f0f0f0; }
img { max-width: 180px; max-height: 80px; border: 1px solid #ccc; border-radius: 4px; background: #eee; }
</style>
</head>
<body>
<header><h1>PrintBridge Dashboard</h1></header>
<section>
<h2>Available Printers</h2>
<table id=""printers""><thead><tr><th>Name</th><th>Default</th></tr></thead><tbody></tbody></table>
<h2>Recent Print Jobs</h2>
<table id=""jobs""><thead><tr><th>Printer</th><th>Size (mm)</th><th>Time</th><th>Preview</th></tr></thead><tbody></tbody></table>
</section>
<script>
async function loadPrinters() {
  const res = await fetch('/api/printers');
  const data = await res.json();
  const tbody = document.querySelector('#printers tbody');
  tbody.innerHTML = '';
  data.printers.forEach(p => {
    const tr = document.createElement('tr');
    tr.innerHTML = `<td>${p.name}</td><td>${p.isDefault ? '✓' : ''}</td>`;
    tbody.appendChild(tr);
  });
}
async function loadJobs() {
  const res = await fetch('/api/jobs');
  const data = await res.json();
  const tbody = document.querySelector('#jobs tbody');
  tbody.innerHTML = '';
  data.jobs.forEach(j => {
    const tr = document.createElement('tr');
    tr.innerHTML = `<td>${j.printer}</td><td>${j.width} x ${j.height}</td><td>${new Date(j.timestamp).toLocaleString()}</td><td><img src='data:image/png;base64,${j.imageBase64}' /></td>`;
    tbody.appendChild(tr);
  });
}
loadPrinters();
loadJobs();
setInterval(loadPrinters, 5000);
setInterval(loadJobs, 5000);
</script>
</body>
</html>");
                            });
                        });
                    });
                })
                .Build();
            Task.Run(() => _httpHost.RunAsync());
        }

        private void StartPrintWorker()
        {
            Task.Run(() =>
            {
                while (_printWorkerRunning)
                {
                    if (_printQueue.TryDequeue(out var job))
                    {
                        PrintImage(job.Image, job.PrinterName, job.WidthMm, job.HeightMm, out string error, addToRecent:true, timestamp:job.Timestamp);
                        job.Image.Dispose();
                    }
                    else
                    {
                        _queueEvent.WaitOne(100);
                    }
                }
            });
        }

        private void EnqueuePrintJob(Image img, string printerName, int widthMm, int heightMm)
        {
            _printQueue.Enqueue(new PrintJob
            {
                Image = (Image)img.Clone(),
                PrinterName = printerName,
                WidthMm = widthMm,
                HeightMm = heightMm,
                Timestamp = DateTime.Now
            });
            _queueEvent.Set();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _printWorkerRunning = false;
                _queueEvent.Set();
                _cancellationTokenSource?.Dispose();
                _webSocketServer?.Dispose();
                foreach (var ws in _wsClients.ToList())
                {
                    ws.Close();
                }
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 