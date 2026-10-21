# Privacy Notice for InputFlow

**Version 1.0 — October 2026**

## Overview

InputFlow is a privacy-by-design Windows application that displays aggregate statistics about your keyboard activity and cursor movement on your own computer. This notice explains what the app accesses, what it retains, and why.

## Data Collection

### What InputFlow collects (transiently)
- **Initial key-down transitions** from all connected keyboards (including modifier keys like Shift, Ctrl, Alt, and navigation keys)
- **Mouse button presses** (left, right, middle click)  
- **Scroll events** (wheel and horizontal wheel)
- **Pointer position samples** used only to calculate cursor distance between consecutive samples

### What InputFlow does NOT collect
- Individual key names, scan codes, virtual-key codes, or typed characters
- Text input, passwords, or any character sequences
- Clipboard contents, screenshots, window titles, document names
- URLs, browser history, email content, or any application data
- Network activity, personal identifiers, or device fingerprints
- Coordinate trails, replayable paths, or positional histories beyond calculation

### Why each data point is collected
| Data | Why | Retention |
|------|-----|-----------|
| Key press count | Aggregate input activity metric | In 30-second buckets; daily summaries for 12 months |
| Mouse click count | Supporting metric alongside keyboard | Same as key presses |
| Scroll event count | Supporting metric | Same as key presses |
| Cursor distance estimate | Primary display metric | Same as key presses |
| Display calibration info | Convert pixels to physical distance | Stored permanently; no personal identifiers |

## Data Storage

- **Format**: SQLite database using SQLCipher encryption at rest
- **Encryption**: Per-user key derived from Windows DPAPI (Data Protection API)
- **Location**: `%LOCALAPPDATA%\InputFlow\data.db`
- **Journal/Temp files**: Wiped with the database; no residual data
- **Logs**: Local structured logs via Serilog written to the same directory; contain no input data

## User Controls

1. **Consent required before tracking starts** — you control when collection begins
2. **Pause immediately** at any time from the dashboard or tray menu
3. **Choose retention period** for 30-second buckets (default: 7 days) and daily summaries (default: 12 months)
4. **Export data** as JSON for any selected range
5. **Delete all data** instantly — removes all app-managed copies from the database

## No Network, No Account

InputFlow operates entirely offline with no account, cloud sync, analytics, crash reporting, or remote monitoring features. The application does not connect to the internet under normal operation.

## Permissions & Privileges

- Runs at standard user privileges — no administrator access required
- Uses documented Windows APIs (Raw Input, GetPhysicalCursorPos) for passive observation
- Respects secure desktops, protected applications, and organizational restrictions
- Collection is limited to the consenting user's own session only

## Distribution Considerations

For public distribution through the Microsoft Store or other channels:
- A privacy policy (this document) will be submitted with the listing
- Store policies regarding privacy disclosures and cryptographic protection apply
- No GDPR/CCPA compliance certifications are claimed without formal assessment

## Contact

For privacy concerns or questions about this notice, open a GitHub Issue on this repository.

---

*This privacy notice accurately describes the implementation in version 1.0 of InputFlow.*
