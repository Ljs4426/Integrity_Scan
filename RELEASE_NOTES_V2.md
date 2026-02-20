# V2 Release Notes

## What's New in V2

### Custom Borderless Window UI
- **Removed white window borders** - Clean, modern borderless design
- **Custom title bar** with logo, title, and window control buttons
- **Blue accent border** (#0078D4) around the entire window
- **Interactive window controls:**
  - Drag title bar to move window
  - Double-click title bar to maximize/restore
  - Minimize, maximize/restore, and close buttons
  - Red hover effect on close button

### Customizable Scan Results Panel
- **Dynamic scan results** - Each unit can define custom scan result items in `unit.json`
- **Flexible configuration** - Add as many or as few scan items as needed per unit
- **Examples included:**
  - Sol Imperialis & 327th Hawkbat: 8 scan result items
  - LOCAL: 2 scan result items

### Fully Offline Operation
- **Removed all version checking** - No more network calls on startup
- **No update prompts** - Application runs completely offline
- **Faster startup** - Removed unnecessary GitHub API calls

### Other Improvements
- Removed all code documentation comments (AI telltales)
- Professional README documentation
- Removed wizard page, replaced with drag-and-drop zip installation
- Units folder now properly included in published builds

## Technical Details

- **Platform:** Windows x64
- **Framework:** .NET 8.0
- **File Size:** 147.21 MB (self-contained, single executable)
- **Dependencies:** None (fully self-contained)
- **Network Requirements:** None (fully offline)

## What's Changed from V1

- Custom borderless window with no white borders
- Customizable scan results per unit configuration
- Removed version checking and update system
- Improved build process with Units folder bundling
- Cleaner codebase with removed AI comments

## Installation

1. Download `327TH_HB_AC.exe`
2. Run the executable
3. Optionally drag-and-drop unit pack ZIP files to install additional units

## Breaking Changes

- None - V2 is fully compatible with existing unit packs

---

**Full Changelog:** https://github.com/Ljs4426/327TH_HB_AC/compare/v1.0.5...v2.0.0
