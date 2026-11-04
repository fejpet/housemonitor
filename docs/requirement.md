# HouseMonitor Specification & Requirements

## Overview
HouseMonitor is a .NET application designed to fetch structured JSON data from a REST endpoint and display key metrics in a widget/window positioned at the bottom-right corner of the monitor.

## Functional & Technical Requirements

| ID | Category | Requirement Description | Status |
| :--- | :--- | :--- | :--- |
| **REQ-001** | Platform | The application shall be implemented as a .NET (`.NET 8.0+`) application. | Planned |
| **REQ-002** | Data Source | The application shall retrieve telemetry/status data from a configurable REST API endpoint returning a JSON payload. | Planned |
| **REQ-003** | Data Parsing | The application shall parse and validate the downloaded JSON structure into strongly-typed domain models. | Planned |
| **REQ-004** | UI Placement | The application shall display the retrieved data in a window/overlay positioned in the bottom-right corner of the monitor. | Planned |
| **REQ-005** | UI Display | The application shall render selected data fields from the JSON payload in a compact, readable layout. | Planned |
| **REQ-006** | Documentation & Language | All project documentation, source code, identifiers, comments, and tests shall be written in English. | Active |
| **REQ-007** | Development Methodology | All new requirements and features shall be developed using an agentic workflow following strict Test-Driven Development (TDD). | Active |

