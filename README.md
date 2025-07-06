# 🖨️ Universal Print Server

A powerful local print server that integrates with your web app to print images as labels using various printer SDKs. Currently supports Munbyn and Epson printers with an extensible architecture for adding more printer types.

## ✨ Features

- **Universal Printer Support**: Extensible architecture supporting multiple printer types
- **Web Interface**: Modern, responsive web UI for managing print jobs
- **Windows Tray App**: System tray integration for easy access
- **REST API**: Full HTTP API for integration with any web application
- **Real-time Status**: Monitor printer status and job progress
- **Image Processing**: Automatic image resizing and optimization for labels
- **Multiple Formats**: Support for various image formats (PNG, JPG, BMP, etc.)

## 🏗️ Architecture

The project consists of three main components:

1. **PrintServer.Core** - Core business logic and printer drivers
2. **PrintServer.Web** - Web API and interface
3. **PrintServer.Tray** - Windows Forms tray application

## 🚀 Quick Start

### Prerequisites

- .NET 7.0 SDK or later
- Windows 10/11
- Visual Studio 2022 or VS Code
- Munbyn or Epson label printer

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd instaLabelSDKMethod
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run the web server**
   ```bash
   cd src/PrintServer.Web
   dotnet run
   ```

4. **Run the tray application**
   ```bash
   cd src/PrintServer.Tray
   dotnet run
   ```

### Using the Web Interface

1. Open your browser and navigate to `http://localhost:5000`
2. Select your printer from the dropdown
3. Upload an image file
4. Configure print settings (width, height, copies, etc.)
5. Click "Print Image"

### Using the API

The print server exposes a REST API for integration:

#### Print an Image
```http
POST /api/print/print
Content-Type: multipart/form-data

imageFile: [image file]
printerName: "Munbyn-COM3"
jobName: "My Label"
width: 100
height: 50
copies: 1
autoCut: true
```

#### Get Available Printers
```http
GET /api/print/printers
```

#### Get Print Job Status
```http
GET /api/print/jobs/{jobId}
```

#### Test Printer Connection
```http
POST /api/print/printers/{printerName}/test
```

## 🖨️ Supported Printers

### Munbyn Label Printers
- **Connection**: Serial (COM ports)
- **Protocol**: ESC/POS commands
- **Features**: Auto-cut, variable label sizes, image printing

### Epson Label Printers
- **Connection**: USB, Network (IP)
- **Protocol**: ePOS SDK
- **Features**: High-resolution printing, status monitoring

## 🔧 Configuration

### Printer Settings

Each printer type can be configured through the web interface or API:

- **Width/Height**: Label dimensions in millimeters
- **Copies**: Number of copies to print
- **Auto Cut**: Automatic label cutting after printing
- **Label Type**: Specific label type for optimal printing

### Web Server Configuration

The web server runs on `http://localhost:5000` by default. You can modify the port in `appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

## 🛠️ Development

### Adding New Printer Support

1. Create a new printer driver class implementing `IPrinterDriver`
2. Add the driver to the dependency injection container
3. Implement the required methods for your printer type

Example:
```csharp
public class MyPrinterDriver : IPrinterDriver
{
    public string DriverName => "My Printer";
    public string DriverVersion => "1.0.0";
    public bool IsSupported => true;

    public async Task<bool> InitializeAsync() { /* ... */ }
    public async Task<List<string>> GetAvailablePrintersAsync() { /* ... */ }
    public async Task<bool> PrintAsync(PrintJob job) { /* ... */ }
    // ... implement other methods
}
```

### Building for Production

1. **Publish the web application**
   ```bash
   dotnet publish src/PrintServer.Web -c Release -o ./publish/web
   ```

2. **Publish the tray application**
   ```bash
   dotnet publish src/PrintServer.Tray -c Release -o ./publish/tray
   ```

3. **Create Windows executable**
   ```bash
   dotnet publish src/PrintServer.Tray -c Release -r win-x64 --self-contained -o ./publish/tray-exe
   ```

## 🔌 Integration Examples

### JavaScript/HTML
```javascript
async function printLabel(imageFile, printerName) {
    const formData = new FormData();
    formData.append('imageFile', imageFile);
    formData.append('printerName', printerName);
    formData.append('width', 100);
    formData.append('height', 50);

    const response = await fetch('http://localhost:5000/api/print/print', {
        method: 'POST',
        body: formData
    });

    const result = await response.json();
    console.log('Print job ID:', result.jobId);
}
```

### Python
```python
import requests

def print_label(image_path, printer_name):
    with open(image_path, 'rb') as f:
        files = {'imageFile': f}
        data = {
            'printerName': printer_name,
            'width': 100,
            'height': 50
        }
        response = requests.post('http://localhost:5000/api/print/print', 
                               files=files, data=data)
        return response.json()
```

### C#
```csharp
using var client = new HttpClient();
using var formData = new MultipartFormDataContent();

var imageContent = new ByteArrayContent(File.ReadAllBytes("label.png"));
formData.Add(imageContent, "imageFile", "label.png");
formData.Add(new StringContent("Munbyn-COM3"), "printerName");
formData.Add(new StringContent("100"), "width");
formData.Add(new StringContent("50"), "height");

var response = await client.PostAsync("http://localhost:5000/api/print/print", formData);
var result = await response.Content.ReadAsStringAsync();
```

## 🐛 Troubleshooting

### Common Issues

1. **Printer not detected**
   - Check USB/Serial connection
   - Verify printer drivers are installed
   - Try refreshing the printer list

2. **Print job fails**
   - Check printer status (paper, ribbon, etc.)
   - Verify image format is supported
   - Check printer settings (width/height)

3. **Web interface not accessible**
   - Ensure the web server is running
   - Check firewall settings
   - Verify port 5000 is not in use

### Logs

The application logs are available in:
- Console output (when running from command line)
- Windows Event Log (when running as service)
- Application logs directory (configurable)

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📞 Support

For support and questions:
- Create an issue in the repository
- Check the troubleshooting section
- Review the API documentation

---

**Made with ❤️ for universal printing solutions** 