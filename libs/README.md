# SDK Libraries Directory

This directory contains third-party SDK libraries for printer integration.

## Directory Structure

```
libs/
├── munbyn/          # Munbyn printer SDK files
│   ├── MunbynSDK.dll
│   ├── MunbynSDK.xml
│   └── README.md
├── epson/           # Epson printer SDK files
│   ├── ePOS2.dll
│   ├── ePOS2.xml
│   └── README.md
└── README.md        # This file
```

## Munbyn SDK

**Place your Munbyn SDK files in:** `libs/munbyn/`

Files to include:
- `MunbynSDK.dll` - Main SDK library
- `MunbynSDK.xml` - Documentation (if available)
- Any other DLL dependencies
- Sample code or documentation

## Epson SDK

**Place your Epson SDK files in:** `libs/epson/`

Files to include:
- `ePOS2.dll` - Main SDK library
- `ePOS2.xml` - Documentation (if available)
- Any other DLL dependencies
- Sample code or documentation

## Adding SDK References

After placing the SDK files, you'll need to:

1. Add references to the project files
2. Update the using statements in the code
3. Implement the actual SDK calls

See the individual README files in each SDK directory for specific instructions. 