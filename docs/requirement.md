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

### Logging, Diagnostics & CLI Documentation Requirements

| ID | Category | Requirement Description | Status |
| :--- | :--- | :--- | :--- |
| **REQ-021** | Internal Process Logging & Severity Levels | The application shall log internal processes across configuration loading, REST client requests, ViewModel refreshes, and scheduled polling using severity levels: `ERROR` for all errors and exceptions, `INFO` for major lifecycle and operational events (default minimum level), and `DEBUG` for detailed diagnostic information during troubleshooting. | Completed |
| **REQ-022** | Configurable Console & File Logging | The logging subsystem shall support writing formatted log entries to the console and/or to a configurable file path. Log level (`DEBUG`, `INFO`, `ERROR`), console logging (`EnableConsole`), and file logging (`EnableFile`, `FilePath`) shall be configurable via the `LoggingSettings` section in `secret.json` (and `secret.example.json`) and overrideable via command-line switches (`--log-level`, `--debug`, `--log-file`, `--console-log`, `--no-console-log`). | Completed |
| **REQ-023** | Execution & Logging Guide (`README.md`) | A root `README.md` file written in English shall document all execution modes, command-line switches, configuration file structure, and instructions for configuring log levels (`ERROR`, `INFO`, `DEBUG`) and console/file outputs. | Completed |
| **REQ-036** | Development Container (`.devcontainer`) | The repository shall provide a VS Code / GitHub Codespaces Development Container configuration (`.devcontainer/devcontainer.json` and `.devcontainer/Dockerfile`) based on `.NET 8.0` that includes `gcc` (for the Linux native stub build), Avalonia X11 runtime dependencies, C# / Avalonia VS Code extensions, and automatic solution restore/build on container creation. | Completed |

### Telemetry Dashboard Widget (Modern HUD Card UI & MVVM) Requirements

| ID | Category | Requirement Description | Status |
| :--- | :--- | :--- | :--- |
| **REQ-024** | HUD Card Container & Ergonomic Dimensions | The overlay UI shall be rendered as a modern dark-themed HUD dashboard card (`CornerRadius="12"`, `Padding="16"`, `BoxShadow="0 8 24 0 #40000000"`) with an ergonomic width of `360–400 px` (default `380 px`) and auto-sized height (`SizeToContent.Height`, `~280–300 px`) so that all cards and the bottom footer timestamp are fully visible without vertical clipping, built purely on Avalonia 11.x. | Completed |
| **REQ-025** | Dark Theme Color Palette & Semantic State Colors | The dashboard card shall use a dark slate palette: main background `#0F172A` (or `#111827`), inner metric cards `#1E293B` (60–80% opacity or solid), and subtle borders `#334155` / `#1E293B`. Semantic state colors shall be: Cyan `#38BDF8` (header & house power), Emerald Green `#10B981` (active solar production, normal battery `> 50%`, online LED), Amber `#F59E0B` (battery discharging, medium battery `20–50%`), Red `#EF4444` (critical battery `< 20%`, error/offline state), and Slate Gray `#64748B` / `#94A3B8` (inactive `0 W` solar, stopped generator, secondary labels). | Completed |
| **REQ-026** | Typography Hierarchy | Card section labels (`BATTERY`, `SOLAR`, `HOUSE`, `GENERATOR`) shall use `11–12 px` uppercase `Medium`/`SemiBold` font (`#94A3B8`); primary metric values shall use `22–26 px` `Bold`/`SemiBold` font (`18 px` for generator state); units shall use `13–14 px` with subtle contrast; daily summary lines (`Today: ...`) shall use `11–12 px` muted slate text; and the footer timestamp shall use `10 px` monospaced font (`Consolas, Monospace`, `#475569`). | Completed |
| **REQ-027** | HUD Header Bar | The top header bar shall display a lightning icon and `"HOUSE MONITOR"` title (`#38BDF8`, `13 px Bold`, letter spacing `1`) on the left, and an online status indicator (`8x8` `Ellipse` filled with `#10B981` when online or `#EF4444` on error), the formatted update time (`HH:mm:ss`, `11 px`, `#94A3B8`), and a minimalist close/hide button on the right. | Completed |
| **REQ-028** | 2×2 Telemetry Grid – Battery Card | Cell `(0,0)` shall display the `"BATTERY"` card containing the State of Charge percentage (`{0}%`, `22 px Bold`, `#F8FAFC`), a directional state indicator (`▼ discharging`, `▲ charging`, or `idle`), and a horizontal `ProgressBar` (`Maximum="100"`, `MinWidth="0"`, `HorizontalAlignment="Stretch"`, `Height="4"`, `CornerRadius="2"`, background `#334155`, and dynamically color-coded foreground) constrained strictly within the card's inner padding. | Completed |
| **REQ-029** | 2×2 Telemetry Grid – Solar Card | Cell `(0,1)` shall display the `"SOLAR"` card containing instantaneous solar production (`{0} W` or `kW`, `22 px Bold`) with dynamic foreground styling (muted gray `#64748B` when `0 W`, active green `#10B981` when `> 0 W`) and the daily yield badge (`Today: {0:F2} kWh`, `11 px`, `#64748B`). | Completed |
| **REQ-030** | 2×2 Telemetry Grid – House Consumption Card | Cell `(1,0)` shall display the `"HOUSE"` card containing instantaneous house consumption (`{0} W` or `kW`, `22 px Bold`, `#38BDF8`) and the daily consumption summary (`Today: {0:F2} kWh`, `11 px`, `#94A3B8`). | Completed |
| **REQ-031** | 2×2 Telemetry Grid – Generator Card | Cell `(1,1)` shall display the `"GENERATOR"` card containing the uppercase/formatted operational state badge (`STOPPED`, `STARTING`, `RUNNING`, `FAULT`, `18 px Bold`, `#64748B` when stopped) and daily production summary (`Today: {0:F1} kWh`, `11 px`, `#64748B`). | Completed |
| **REQ-032** | HUD Footer Bar | The bottom footer shall be separated by a `1 px` top border (`#1E293B` / `#334155`) and display the centered full telemetry timestamp (`Timestamp: yyyy-MM-dd HH:mm:ss`) in `10 px` monospaced font (`#475569`). | Completed |
| **REQ-033** | Strongly-Typed State Enumerations | The domain/presentation layer shall define `BatteryState` (`Idle`, `Charging`, `Discharging`) and `GeneratorState` (`Stopped`, `Starting`, `Running`, `Fault`) enumerations mapped case-insensitively from the raw JSON state strings. | Completed |
| **REQ-034** | Reactive Dashboard ViewModel Properties | The ViewModel (`OverlayViewModel` / `HouseMonitorViewModel` implementing `INotifyPropertyChanged`) shall expose reactive properties for card binding: `BatteryPercentage` (`double`), `BatteryStatus` (`BatteryState`), `SolarPowerWatts` (`double`), `SolarTodayKWh` (`double`), `IsSolarProducing` (`bool => SolarPowerWatts > 0`), `HousePowerWatts` (`double`), `HouseTodayKWh` (`double`), `GeneratorStatus` (`GeneratorState`), `GeneratorTodayKWh` (`double`), `IsOnline` (`bool`), and `LastUpdated` (`DateTimeOffset?`), updating flicker-free upon each poll. | Completed |
| **REQ-035** | Value Converters & Display Formatters | The presentation layer shall provide converters/formatters for UI binding: `BatteryColorConverter` (maps SoC and `BatteryState` to Red `< 20%`, Amber `20–50%` or `Discharging`, and Green `> 50%`), `StateToIconOrArrowConverter` (maps `Discharging` to `▼ discharging`, `Charging` to `▲ charging`, `Idle` to `idle`), `ActiveSolarBrushConverter` (maps `IsSolarProducing` to `#10B981` when `true` and `#64748B` when `false`), and `WattsToDisplayConverter` (formats power as `{0:N0} W` or `kW` when `> 1000 W`). | Completed |

## Reference HUD Card Design & MVVM Specification

### ViewModel & Enumerations (`REQ-033`, `REQ-034`)
```csharp
public enum BatteryState
{
    Idle,
    Charging,
    Discharging
}

public enum GeneratorState
{
    Stopped,
    Starting,
    Running,
    Fault
}

public class HouseMonitorViewModel : ObservableObject
{
    // Battery
    public double BatteryPercentage { get; set; } // e.g. 83.0
    public BatteryState BatteryStatus { get; set; } // Discharging

    // Solar
    public double SolarPowerWatts { get; set; } // e.g. 0
    public double SolarTodayKWh { get; set; } // e.g. 11.63
    public bool IsSolarProducing => SolarPowerWatts > 0;

    // House Consumption
    public double HousePowerWatts { get; set; } // e.g. 454
    public double HouseTodayKWh { get; set; } // e.g. 6.69

    // Generator
    public GeneratorState GeneratorStatus { get; set; } // Stopped
    public double GeneratorTodayKWh { get; set; } // e.g. 0.0

    // System / Connection
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
}
```

### Avalonia XAML Layout Blueprint (`REQ-024`–`REQ-032`)
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:HouseMonitor.Core.Presentation"
             x:Class="HouseMonitor.App.Views.HouseMonitorCard"
             x:DataType="vm:OverlayViewModel"
             Width="380">

    <Border Background="#0F172A"
            CornerRadius="12"
            BorderBrush="#1E293B"
            BorderThickness="1"
            Padding="16"
            BoxShadow="0 8 24 0 #40000000">

        <StackPanel Spacing="14">
            <!-- HEADER -->
            <Grid ColumnDefinitions="*, Auto">
                <StackPanel Orientation="Horizontal" Spacing="8" VerticalAlignment="Center">
                    <PathIcon Data="{StaticResource LightningIcon}" Foreground="#38BDF8" Width="14" Height="14"/>
                    <TextBlock Text="HOUSE MONITOR"
                               FontWeight="Bold"
                               FontSize="13"
                               Foreground="#38BDF8"
                               LetterSpacing="1"/>
                </StackPanel>

                <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="6" VerticalAlignment="Center">
                    <Ellipse Width="8" Height="8" Fill="#10B981"/>
                    <TextBlock Text="{Binding LastUpdated, StringFormat='{}{0:HH:mm:ss}'}"
                               FontSize="11"
                               Foreground="#94A3B8"/>
                </StackPanel>
            </Grid>

            <!-- 2x2 TELEMETRY GRID -->
            <Grid ColumnDefinitions="*, *" RowDefinitions="Auto, Auto" Margin="0,4,0,0">
                <!-- BATTERY CARD -->
                <Border Grid.Row="0" Grid.Column="0"
                        Background="#1E293B" CornerRadius="8" Padding="12" Margin="0,0,4,4">
                    <StackPanel Spacing="6">
                        <TextBlock Text="BATTERY" FontSize="11" Foreground="#94A3B8" FontWeight="SemiBold"/>
                        <StackPanel Orientation="Horizontal" Spacing="6" VerticalAlignment="Bottom">
                            <TextBlock Text="{Binding BatteryPercentage, StringFormat='{}{0}%'}"
                                       FontSize="22" FontWeight="Bold" Foreground="#F8FAFC"/>
                            <TextBlock Text="{Binding BatteryStatus}"
                                       FontSize="11" Foreground="#F59E0B" VerticalAlignment="Center"/>
                        </StackPanel>
                        <ProgressBar Value="{Binding BatteryPercentage}" Maximum="100" Height="4"
                                     Foreground="#F59E0B" Background="#334155" CornerRadius="2"/>
                    </StackPanel>
                </Border>

                <!-- SOLAR CARD -->
                <Border Grid.Row="0" Grid.Column="1"
                        Background="#1E293B" CornerRadius="8" Padding="12" Margin="4,0,0,4">
                    <StackPanel Spacing="6">
                        <TextBlock Text="SOLAR" FontSize="11" Foreground="#94A3B8" FontWeight="SemiBold"/>
                        <TextBlock Text="{Binding SolarPowerWatts, StringFormat='{}{0} W'}"
                                   FontSize="22" FontWeight="Bold"
                                   Foreground="{Binding IsSolarProducing, Converter={StaticResource ActiveSolarBrushConverter}}"/>
                        <TextBlock Text="{Binding SolarTodayKWh, StringFormat='{}Today: {0:F2} kWh'}"
                                   FontSize="11" Foreground="#64748B"/>
                    </StackPanel>
                </Border>

                <!-- HOUSE CONSUMPTION CARD -->
                <Border Grid.Row="1" Grid.Column="0"
                        Background="#1E293B" CornerRadius="8" Padding="12" Margin="0,4,4,0">
                    <StackPanel Spacing="6">
                        <TextBlock Text="HOUSE" FontSize="11" Foreground="#94A3B8" FontWeight="SemiBold"/>
                        <TextBlock Text="{Binding HousePowerWatts, StringFormat='{}{0} W'}"
                                   FontSize="22" FontWeight="Bold" Foreground="#38BDF8"/>
                        <TextBlock Text="{Binding HouseTodayKWh, StringFormat='{}Today: {0:F2} kWh'}"
                                   FontSize="11" Foreground="#94A3B8"/>
                    </StackPanel>
                </Border>

                <!-- GENERATOR CARD -->
                <Border Grid.Row="1" Grid.Column="1"
                        Background="#1E293B" CornerRadius="8" Padding="12" Margin="4,4,0,0">
                    <StackPanel Spacing="6">
                        <TextBlock Text="GENERATOR" FontSize="11" Foreground="#94A3B8" FontWeight="SemiBold"/>
                        <TextBlock Text="{Binding GeneratorStatus}"
                                   FontSize="18" FontWeight="Bold" Foreground="#64748B"/>
                        <TextBlock Text="{Binding GeneratorTodayKWh, StringFormat='{}Today: {0:F1} kWh'}"
                                   FontSize="11" Foreground="#64748B"/>
                    </StackPanel>
                </Border>
            </Grid>

            <!-- FOOTER -->
            <Border BorderBrush="#1E293B" BorderThickness="0,1,0,0" Padding="0,8,0,0">
                <TextBlock Text="{Binding LastUpdated, StringFormat='{}Timestamp: {0:yyyy-MM-dd HH:mm:ss}'}"
                           FontSize="10"
                           FontFamily="Consolas, Monospace"
                           Foreground="#475569"
                           HorizontalAlignment="Center"/>
            </Border>
        </StackPanel>
    </Border>
</UserControl>
```

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
