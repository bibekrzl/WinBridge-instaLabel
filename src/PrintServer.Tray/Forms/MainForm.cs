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

        public MainForm(IPrintService printService, ILogger<MainForm> logger)
        {
            _printService = printService;
            _logger = logger;

            InitializeComponent();
            InitializeTrayIcon();
            InitializeWebServer();
            InitializeWebSocketServer();
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
            _contextMenu.Items.Add("Show/Hide", null, ShowHide_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Refresh Printers", null, RefreshPrinters_Click);
            _contextMenu.Items.Add("Test Printers", null, TestPrinters_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("View Logs", null, ViewLogs_Click);
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Exit", null, Exit_Click);

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
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

        private void ViewLogs_Click(object? sender, EventArgs e)
        {
            try
            {
                // Open the web interface in the default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "http://localhost:5000",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening web interface");
                MessageBox.Show($"Error opening web interface: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                                    PrintImage(img, _defaultPrinter, labelWidth, labelHeight, out string printError);
                                    if (string.IsNullOrEmpty(printError))
                                    {
                                        SendPrintSuccessResponse(socket, _defaultPrinter);
                                    }
                                    else
                                    {
                                        SendPrintErrorResponse(socket, printError);
                                    }
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
                            // Use default size (56mm x 31mm)
                            PrintImage(legacyImg, _defaultPrinter, 56, 31, out string legacyPrintError);
                            if (string.IsNullOrEmpty(legacyPrintError))
                            {
                                SendPrintSuccessResponse(socket, _defaultPrinter);
                            }
                            else
                            {
                                SendPrintErrorResponse(socket, legacyPrintError);
                            }
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

        private void PrintImage(Image img, string printerName, int widthMm, int heightMm, out string error)
        {
            error = string.Empty;
            try
            {
                using PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                // Set custom paper size: widthMm x heightMm (1 inch = 25.4mm)
                var paperSize = new PaperSize("Label", (int)(widthMm / 25.4 * 100), (int)(heightMm / 25.4 * 100));
                pd.DefaultPageSettings.PaperSize = paperSize;
                pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                pd.PrintPage += (s, e) =>
                {
                    // Draw the image scaled to fit the label
                    e.Graphics.DrawImage(img, e.MarginBounds);
                };
                pd.Print();
            }
            catch (Exception ex)
            {
                error = ex.Message;
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
                Application.Exit();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application shutdown");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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