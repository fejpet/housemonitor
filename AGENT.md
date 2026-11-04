# Agentic Development Guidelines (AGENT.md)

## 1. Project Language
- **Mandatory Language**: All code, comments, commit messages, documentation (including `docs/`), requirement definitions, and test names **must be written in English**.
- Conversations with the user may take place in the user's preferred language (e.g., Hungarian), but all repository artifacts must remain strictly in English.

## 2. Requirements Management
- All requirements are tracked in [`docs/requirement.md`](docs/requirement.md).
- Every requirement must have a **unique identifier** (e.g., `REQ-001`, `REQ-002`).
- When a new requirement is introduced:
  1. Assign a new unique `REQ-XXX` identifier in `docs/requirement.md` before or during implementation.
  2. Reference the requirement ID in related tests, code documentation, or commit messages where applicable.
  3. Update the requirement status (`Planned` -> `In Progress` -> `Completed`) as work progresses.

## 3. Agentic Workflow & Test-Driven Development (TDD)
All new features, requirements, and bug fixes must strictly follow the **Red-Green-Refactor** TDD cycle:
1. **Specify**: Ensure the requirement is clearly documented with a unique ID in `docs/requirement.md`.
2. **Red (Write Failing Tests First)**: Write automated unit/integration tests in `tests/HouseMonitor.Tests` that verify the target requirement before writing production implementation code. Verify that the new tests fail as expected.
3. **Green (Implement Minimum Viable Code)**: Implement the minimal production code in `src/HouseMonitor.Core` (or `src/HouseMonitor.App`) required to make the failing tests pass.
4. **Refactor**: Clean up and improve the code structure, readability, and design while keeping all tests green.
5. **Verify**: Run the full test suite (`dotnet test`) to ensure zero regressions before completing the task.

## 4. Solution Architecture
- `docs/`: Project specifications and requirements (`docs/requirement.md`).
- `src/HouseMonitor.Core/`: Core domain models, REST client services, JSON parsing, and presentation logic (UI-agnostic and fully unit-testable).
- `src/HouseMonitor.App/`: Application entry point and UI host responsible for rendering the bottom-right monitor overlay.
- `tests/HouseMonitor.Tests/`: Automated xUnit test suite covering `HouseMonitor.Core` and application logic.

