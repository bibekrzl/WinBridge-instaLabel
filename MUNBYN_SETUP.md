# 🖨️ Munbyn Printer Setup Guide

## 📁 File Structure Created

```
instaLabelSDKMethod/
├── libs/
│   ├── munbyn/              ← Put your Munbyn SDK here
│   │   ├── MunbynSDK.dll    ← Required
│   │   ├── MunbynSDK.xml    ← Optional (documentation)
│   │   └── README.md        ← Setup instructions
│   ├── epson/               ← For future Epson SDK
│   └── README.md            ← SDK overview
├── src/
│   ├── PrintServer.Core/
│   │   └── Printers/
│   │       ├── MunbynSdkPrintSample.cs    ← 56mm x 31mm sample
│   │       ├── MunbynPrinterDriver.cs     ← ESC/POS implementation
│   │       └── EpsonPrinterDriver.cs      ← Future Epson support
│   ├── PrintServer.Web/
│   │   ├── Controllers/
│   │   │   ├── PrintController.cs         ← Universal print API
│   │   │   └── MunbynController.cs        ← Munbyn-specific API
│   │   └── wwwroot/
│   │       ├── index.html                 ← Main web interface
│   │       └── munbyn-test.html           ← Munbyn test page
│   └── PrintServer.Tray/                  ← Windows tray app
├── test-munbyn.ps1          ← Quick test script
├── build.ps1                ← Build and run script
└── MUNBYN_SETUP.md          ← This file
```

## 🚀 Quick Start

### 1. Place Munbyn SDK Files
Copy your Munbyn SDK files to:
```
C:\Users\nup_2\Desktop\instaLabelSDKMethod\libs\munbyn\
```

Required files:
- `MunbynSDK.dll` - Main SDK library
- `MunbynSDK.xml` - Documentation (if available)

### 2. Run the Test Server
```powershell
.\test-munbyn.ps1
```

### 3. Test the Integration
Open your browser and go to:
- **Main Interface**: http://localhost:5000
- **Munbyn Test Page**: http://localhost:5000/munbyn-test.html
- **API Documentation**: http://localhost:5000/swagger

## 🖨️ Available API Endpoints

### Munbyn-Specific Endpoints
- `POST /api/munbyn/print-sample` - Print 56mm x 31mm sample label
- `POST /api/munbyn/print-custom` - Print custom text label
- `GET /api/munbyn/status` - Check printer status

### Universal Print Endpoints
- `POST /api/print/print` - Print any image
- `GET /api/print/printers` - List available printers
- `GET /api/print/jobs` - List print jobs

## 📏 Label Specifications

**Current Configuration:**
- **Width**: 56mm
- **Height**: 31mm
- **DPI**: 203 (8 dots/mm)
- **Resolution**: 448 x 248 pixels

## 🔧 Next Steps After SDK Integration

### 1. Add DLL Reference
Once you place the SDK files, add this to `src/PrintServer.Core/PrintServer.Core.csproj`:

```xml
<ItemGroup>
  <Reference Include="MunbynSDK">
    <HintPath>..\..\..\libs\munbyn\MunbynSDK.dll</HintPath>
  </Reference>
</ItemGroup>
```

### 2. Update Code
In `src/PrintServer.Core/Printers/MunbynSdkPrintSample.cs`:
1. Uncomment: `using Munbyn.SDK;`
2. Replace pseudo-code with actual SDK calls

### 3. Test COM Port
Common Munbyn printer ports:
- COM3
- COM4
- COM5
- COM6

Check Device Manager to find your printer's COM port.

## 🧪 Testing

### Web Interface Testing
1. Open http://localhost:5000/munbyn-test.html
2. Click "Print Sample Label" to test basic functionality
3. Enter custom text and click "Print Custom Label"
4. Use "Check Printer Status" to verify connection

### API Testing
```bash
# Print sample label
curl -X POST http://localhost:5000/api/munbyn/print-sample

# Print custom label
curl -X POST http://localhost:5000/api/munbyn/print-custom \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello World!", "comPort": "COM3"}'

# Check status
curl http://localhost:5000/api/munbyn/status
```

## 🔍 Troubleshooting

### Common Issues
1. **SDK not found**: Ensure `MunbynSDK.dll` is in `libs\munbyn\`
2. **COM port error**: Check Device Manager for correct port
3. **Build errors**: Run `dotnet build` to check for missing references
4. **Web server won't start**: Check if port 5000 is in use

### Debug Steps
1. Check console output for error messages
2. Verify printer is connected and powered on
3. Test COM port with Device Manager
4. Check Windows Event Log for errors

## 📞 Support

If you encounter issues:
1. Check the console output for error messages
2. Verify the SDK files are correctly placed
3. Test the COM port connection
4. Review the API documentation at http://localhost:5000/swagger

## 🎯 Current Status

✅ **Completed:**
- Project structure and build system
- Web API endpoints for Munbyn
- Sample label generation (56mm x 31mm)
- Test web interface
- Universal print server architecture

⏳ **Pending SDK Integration:**
- Add Munbyn SDK DLL reference
- Implement actual SDK calls
- Test with real printer hardware

---

**Ready to integrate your Munbyn SDK!** 🚀 