# Run KPI Batch Launcher Design

## Goal

Provide a single Windows entry point that a contributor can double-click after setup to bootstrap and run the local KPI web prototype.

## Behavior

`run-kpi.bat` resolves the repository root from its own location, invokes the canonical `harness.cmd bootstrap`, starts `src/Kpi.Web` in a separate console at `http://localhost:5080`, waits for the host to accept connections, and opens the default browser. It keeps the web console visible so the operator can inspect logs and stop the host with `Ctrl+C`.

The launcher reports missing prerequisites or failed bootstrap steps and does not start a partially prepared application. It does not run the full verification suite; contributors use `harness.cmd check` for that.

## Constraints

- Windows batch entry point; no alternate application bootstrap path.
- No secrets, database destruction, or destructive Git operations.
- Fixed local port avoids ambiguity when opening the browser.
- The launcher must work when the repository path contains spaces.
