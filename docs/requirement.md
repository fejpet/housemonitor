# HouseMonitor Specification & Requirements

## Overview
HouseMonitor is a .NET application designed to fetch structured JSON telemetry data from a remote REST endpoint and display key power/energy metrics in a desktop overlay widget positioned at the bottom-right corner of the monitor.

## Functional & Technical Requirements

### Core & General Requirements

| ID | Category | Requirement Description | Status |
| :--- | :--- | :--- | :--- |
| **REQ-001** | Platform | The application shall be implemented as a .NET (`.NET 8.0+`) application. | Completed |
| **REQ-006** | Documentation & Language | All project documentation, source code, identifiers, comments, and tests shall be written in English. | Active |
| **REQ-007** | Development Methodology | All new requirements and features shall be developed using an agentic workflow following strict Test-Driven Development (TDD). | Active |

### REST Client & Secret Configuration Requirements

| ID | Category | Requirement Description | Status |
| :--- | :--- | :--- | :--- |
| **REQ-002** | REST Client | The REST client shall fetch telemetry data via an HTTP `GET` request from the endpoint URL configured in `secret.json`. | Completed |
| **REQ-008** | Authentication | The REST client shall authenticate requests by sending the `X-Auth-Key` HTTP request header containing the configured secret API key. | Completed |
| **REQ-009** | Secret Storage | Both the REST endpoint URL and the secret API key shall be stored exclusively in a local `secret.json` file that is excluded from version control via `.gitignore`. Neither the real endpoint URL nor the secret API key may appear in any tracked repository file. | Completed |
| **REQ-010** | Secret Template | A `secret.example.json` file shall be tracked in the repository, containing the exact configuration structure with placeholder values (`<YOUR_ENDPOINT_URL>`, `<YOUR_SECRET_API_KEY>`) instead of actual secrets. | Completed |

### Domain Model & JSON Deserialization Requirements

| ID | Category | Requirement Description | Status |
| :--- | :--- | :--- | :--- |
| **REQ-003** | Root Domain Model | The application shall deserialize the root JSON payload into a strongly-typed `PowerStatus` domain object containing `battery`, `generator`, `solar`, `house`, and `timestamp` fields. | Completed |
| **REQ-011** | Battery Domain Model | The `battery` JSON object shall map to a `BatteryStatus` model with properties: `Soc` (`double`, mapped from `"soc"`, representing state of charge in %) and `State` (`string`, mapped from `"state"`, e.g., `"discharging"`). | Completed |
| **REQ-012** | Generator Domain Model | The `generator` JSON object shall map to a `GeneratorStatus` model with properties: `State` (`string`, mapped from `"state"`, e.g., `"stopped"`) and `Today` (`double`, mapped from `"today"`). | Completed |
| **REQ-013** | Solar Domain Model | The `solar` JSON object shall map to a `SolarStatus` model with properties: `Production` (`double`, mapped from `"production"`) and `Today` (`double`, mapped from `"today"`). | Completed |
| **REQ-014** | House Consumption Domain Model | The `house` JSON object shall map to a `HouseStatus` model with properties: `Consumption` (`double`, mapped from `"consumption"`) and `TodayConsumption` (`double`, mapped from the exact JSON property `"today_consumtion"`). | Completed |
| **REQ-019** | Telemetry Timestamp | The root `"timestamp"` JSON property (in ISO 8601 format, e.g., `"2026-11-04T22:20:35.014353+01:00"`) shall map to a `DateTimeOffset Timestamp` property on `PowerStatus` representing the data generation time, and shall be exposed by the ViewModel as the telemetry timestamp. | Completed |

### Graphical Desktop Overlay & Scheduled Refresh Requirements

| ID | Category | Requirement Description | Status |
| :--- | :--- | :--- | :--- |
| **REQ-004** | UI Placement | The application shall display the retrieved data in a window/overlay positioned in the bottom-right corner of the primary monitor's working area. | Completed |
| **REQ-005** | UI Display | The application shall render selected data fields from the JSON payload (`Battery`, `Solar`, `House`, `Generator`) along with update status in a compact, readable layout. | Completed |
| **REQ-015** | Graphical UI Framework | The desktop overlay window shall be implemented using the cross-platform Avalonia UI framework in `HouseMonitor.App`, bound to an observable `OverlayViewModel` in `HouseMonitor.Core`. | Completed |
| **REQ-016** | Scheduled Refresh | The application shall automatically and periodically fetch updated telemetry from the REST endpoint at a configurable interval (`RefreshIntervalSeconds` in `ApiSettings`, defaulting to `10` seconds) and notify the UI on each update. | Completed |
| **REQ-017** | Resilient Polling & Error State | When a refresh attempt fails (e.g., network or HTTP error), the polling service and ViewModel shall capture the error state without crashing, retain the last known valid telemetry values if present, and continue subsequent scheduled refreshes. | Completed |
| **REQ-018** | Overlay Window Styling | The desktop window shall be borderless or compactly framed, always-on-top (`Topmost = true`), non-resizable, and positioned at the bottom-right screen coordinates calculated by `OverlayPositionCalculator`. | Completed |
| **REQ-020** | Linux / WSL2 X11 Compatibility | The application shall disable legacy X11 session management (`EnableSessionManagement = false`) and provide a fallback native stub (`libICE.so.6` / `libSM.so.6`) via `NativeLibrary.SetDllImportResolver` so the application runs on Linux / WSLg environments without requiring `libice6` or `libsm6` system packages. | Completed |

## Reference API Specification

### Request
```bash
curl -X GET <YOUR_ENDPOINT_URL> \
  -H "X-Auth-Key: <YOUR_SECRET_API_KEY>"
```

### Sample JSON Response
```json
{
  "battery": {
    "soc": 65.0,
    "state": "discharging"
  },
  "generator": {
    "state": "stopped",
    "today": 0
  },
  "solar": {
    "production": 0.0,
    "today": 17.98
  },
  "house": {
    "consumption": 415.0,
    "today_consumtion": 13.38
  },
  "timestamp": "2026-11-04T22:20:35.014353+01:00"
}
```
