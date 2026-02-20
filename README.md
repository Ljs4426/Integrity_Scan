# 327TH HB AC

Modular anticheat application with dynamic theming and unit-based configuration.

## Features

- **Multiple Units**: Support for different anticheat configurations (327th Hawkbat, Sol Imperialis, LOCAL)
- **Dynamic Themes**: Each unit has its own colors and branding
- **PowerShell Scripts**: Run custom PowerShell-based checks with full interactivity
- **Modular System**: Add new units by dropping zip files

## Installation

1. Download the latest release
2. Run `327TH_HB_AC.exe` as administrator
3. Accept the terms of service
4. Select a unit to run

## Adding Custom Units

Drop unit zip files into the application or browse to select them. Each unit package should contain:

- `unit.json` - Configuration (name, colors, version)
- `script.ps1` - PowerShell script to execute
- `tos.txt` - Terms of service text
- `logo.png` (optional) - Unit logo image

### Unit Configuration Example

```json
{
  "name": "Unit Name",
  "subtitle": "Description",
  "accentColor": "#0078D4",
  "textColor": "#FFFFFF",
  "backgroundColor": "#000000",
  "hasLogo": true,
  "requireUsername": true,
  "version": "1.0.0"
}
```

## Tech Stack

- .NET 8.0 Windows WPF
- PowerShell integration
- Single-file executable

## Credits

Thanks to SOL for the recording policy script!