# FlashFix Pro

RashiTec edition.

FlashFix Pro is a Windows desktop storage diagnostic starter application for removable USB and SD-card media. It is built with .NET 10 and WPF and is designed around recovery-first, read-only operation.

## Current implemented functionality

- Enumerates physical disks and mounted volumes through read-only Windows CIM queries.
- Protects Windows system disks and non-removable disks.
- Shows physical disk number, model, interface, reported capacity, mounted file-system volumes, and safety status.
- Runs a read-only diagnostic pass.
- Blocks recovery and destructive actions on protected disks.
- Provides a clearly marked capacity-engine verification against an isolated test image.
- Records operations in the live application log.

The bundled recovery engine identifies integration points and honestly reports that a licensed third-party recovery executable has not been bundled. No destructive repair or partition operation is advertised or exposed by this milestone.

## Requirements

- Windows 10 or Windows 11 x64.
- .NET 10 Desktop Runtime.
- PowerShell 5.1 for Windows device discovery.

## Build

```powershell
dotnet restore FlashFixPro.slnx
dotnet build FlashFixPro.slnx -c Release
dotnet test FlashFixPro.slnx -c Release
dotnet publish FlashFixPro.App/FlashFixPro.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false
```

## Safety

- Recovery output must never be written to the source disk.
- Keep source media read-only until a disk image exists and recovery results are verified.
- System disks, boot disks, and fixed disks are protected.
- Capacity verification of physical media is destructive and is not implemented in this safety-focused milestone.
- The test-image capacity check only verifies the local capacity engine's write/read behavior.
