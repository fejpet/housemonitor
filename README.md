# HouseMonitor

HouseMonitor is a cross-platform `.NET 8.0` application that periodically fetches structured power and energy telemetry from a remote REST endpoint and displays key metrics (`Battery`, `Solar`, `House`, `Generator`, and payload `Timestamp`) in a compact desktop overlay positioned in the bottom-right corner of the primary monitor.

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.
- On Linux / WSL2 (WSLg): an active X11/Wayland display server for the graphical overlay mode (or use `--console` for headless CLI operation).

---

## Configuration (`secret.json`)

All sensitive API credentials and runtime settings are stored in a local `secret.json` file in the repository root (excluded from version control via `.gitignore`).

1. Copy the template file:
   ```bash
   cp secret.example.json secret.json
   ```
2. Edit `secret.json` with your real endpoint URL, API key, and desired logging preferences:
   ```json
   {
     "ApiSettings": {
       "EndpointUrl": "https://your-endpoint.example.com/api/power",
       "AuthHeaderName": "X-Auth-Key",
       "AuthKey": "your-real-secret-api-key",
       "RefreshIntervalSeconds": 10
     },
     "LoggingSettings": {
       "Level": "INFO",
       "EnableConsole": true,
       "EnableFile": false,
       "FilePath": "logs/housemonitor.log"
     }
   }
   ```

---

## Execution Modes & Command-Line Switches

Run the application from the repository root using `dotnet run --project src/HouseMonitor.App -- [options]` or by executing the compiled binary directly.

### 1. Desktop Overlay Mode (Default)
Launches the borderless, always-on-top Avalonia UI widget at the bottom-right corner of the primary monitor and refreshes telemetry every `RefreshIntervalSeconds`:
```bash
dotnet run --project src/HouseMonitor.App
```

### 2. Console One-Shot Mode (`--console` / `-c`)
Fetches the power telemetry once, prints the formatted summary lines and status timestamp to standard output, and exits with code `0` on success (or `1` on error):
```bash
dotnet run --project src/HouseMonitor.App -- --console
```

### Command-Line Switches Reference

| Switch | Short | Description |
| :--- | :--- | :--- |
| *(none)* | | Starts the graphical desktop overlay window with periodic background polling. |
| `--console` | `-c` | Runs a single telemetry fetch in the terminal, prints the summary, and exits. |
| `--help` | `-h` | Displays command-line usage instructions and available switches, then exits. |
| `--config <path>` | | Specifies an explicit path to the `secret.json` configuration file (alias: `--secret-file <path>`). |
| `--log-level <LEVEL>` | | Overrides the minimum log severity level (`DEBUG`, `INFO`, or `ERROR`). |
| `--debug` | `-d` | Shortcut for `--log-level DEBUG` to enable verbose diagnostic logging for troubleshooting. |
| `--log-file <path>` | | Enables file logging and appends formatted log entries to `<path>`. |
| `--console-log [bool]` | | Explicitly enables (`true`) or disables (`false`) logging to the console. |
| `--no-console-log` | | Disables logging to the console (useful when logging exclusively to a file). |

> **Precedence Rule:** Command-line switches always override the corresponding values configured in `secret.json`.

---

## Logging & Log Level Configuration Guide

HouseMonitor includes built-in structured logging (`REQ-021`, `REQ-022`) across configuration loading (`SecretConfigurationLoader`), HTTP telemetry queries (`PowerStatusClient`), presentation updates (`OverlayViewModel`), and background polling (`PowerStatusPollingService`).

### Supported Log Levels

| Level | Purpose & Logged Events |
| :--- | :--- |
| `ERROR` | Logs all errors, unhandled or caught exceptions, configuration validation failures, HTTP non-success responses, network timeouts, and JSON deserialization failures. |
| `INFO` *(Default)* | Logs major lifecycle and operational events (e.g., configuration validated, application/polling service started or stopped, telemetry refresh succeeded) plus all `ERROR` entries. |
| `DEBUG` | Logs detailed diagnostic traces for troubleshooting (e.g., configuration file path resolution, outgoing HTTP `GET` request metadata, HTTP status codes, raw JSON payload length, polling sleep intervals) plus all `INFO` and `ERROR` entries. *(Note: The secret `AuthKey` value is never written to logs at any level.)* |

### How to Configure Log Levels & Outputs

#### Option A: Via `secret.json` (Persistent Configuration)
Update the `LoggingSettings` section in `secret.json`:

- **Standard Operation (`INFO` to Console only — Default):**
  ```json
  "LoggingSettings": {
    "Level": "INFO",
    "EnableConsole": true,
    "EnableFile": false,
    "FilePath": "logs/housemonitor.log"
  }
  ```

- **Troubleshooting Mode (`DEBUG` to both Console and File):**
  ```json
  "LoggingSettings": {
    "Level": "DEBUG",
    "EnableConsole": true,
    "EnableFile": true,
    "FilePath": "logs/housemonitor.log"
  }
  ```

- **Quiet / Production Error-Only Logging to File:**
  ```json
  "LoggingSettings": {
    "Level": "ERROR",
    "EnableConsole": false,
    "EnableFile": true,
    "FilePath": "logs/errors.log"
  }
  ```

#### Option B: Via Command-Line Switches (Ad-Hoc Override)
You can change the log level or output destination for a single run without editing `secret.json`:

- **Enable `DEBUG` logging on the console:**
  ```bash
  dotnet run --project src/HouseMonitor.App -- --debug
  ```

- **Run in console mode with `DEBUG` logs written to both console and a custom log file:**
  ```bash
  dotnet run --project src/HouseMonitor.App -- --console --debug --log-file logs/debug-session.log
  ```

- **Log only `ERROR` events to a file while suppressing console log output:**
  ```bash
  dotnet run --project src/HouseMonitor.App -- --log-level ERROR --log-file logs/error.log --no-console-log
  ```

### Log Entry Format
Each log line includes an ISO-like timestamp with timezone offset, severity tag, source component category, message, and optional exception details:
```text
2026-11-05 21:30:00.123 +01:00 [INFO] [PowerStatusPollingService] Starting scheduled telemetry polling loop with interval 10s.
2026-11-05 21:30:00.125 +01:00 [DEBUG] [PowerStatusClient] Sending HTTP GET request to 'https://api.example.test/power' using header 'X-Auth-Key'.
2026-11-05 21:30:00.240 +01:00 [INFO] [PowerStatusClient] Successfully retrieved power telemetry (Timestamp=2026-11-05T21:29:58.0000000+01:00, BatterySoc=65%).
```

---

## Running Automated Tests

Run the full xUnit test suite from the repository root:
```bash
dotnet test
```
