# Munbyn SDK

## File Placement

**Put your Munbyn SDK files here:** `libs/munbyn/`

## Required Files

Copy these files from your Munbyn SDK download:

- `MunbynSDK.dll` - Main SDK library
- `MunbynSDK.xml` - Documentation (if available)
- Any other DLL dependencies
- Sample code or documentation

## Project Integration

After placing the files, update the project references:

1. **Add DLL reference to PrintServer.Core.csproj:**
   ```xml
   <ItemGroup>
     <Reference Include="MunbynSDK">
       <HintPath>..\..\..\libs\munbyn\MunbynSDK.dll</HintPath>
     </Reference>
   </ItemGroup>
   ```

2. **Update MunbynSdkPrintSample.cs:**
   - Uncomment the `using Munbyn.SDK;` line
   - Replace the pseudo-code with actual SDK calls

## Example Usage

```csharp
using Munbyn.SDK;

// Initialize printer
var printer = new MunbynPrinter();
printer.Open("COM3"); // Use your actual COM port

// Print image
printer.PrintImage("path/to/image.bmp");

// Close connection
printer.Close();
```

## COM Port Configuration

Common COM ports for Munbyn printers:
- COM3
- COM4
- COM5
- COM6

Check Device Manager to find the correct port for your printer. 