# Quality policy

## Definition of done

A change is ready when:

1. behavior and acceptance criteria are satisfied;
2. relevant automated tests exist and pass;
3. lint and static checks pass;
4. durable documentation reflects the new state;
5. no credentials, local environment files, generated dependencies, or build output are tracked;
6. `./harness.cmd check` succeeds on Windows (or `pwsh ./scripts/harness.ps1 check` on macOS/Linux).

## Verification layers

- **Repository contract:** required agent context and harness files exist; secret-bearing `.env` files are not tracked.
- **Lint:** formatting, static analysis, type checks, and policy checks configured for the selected stack.
- **Test:** the smallest deterministic suite that covers the change, expanding to integration or end-to-end checks when boundaries are affected.
- **CI:** executes the same `check` command used locally.

Until an application stack is selected, lint and test step lists are intentionally empty. Adding application code without wiring those steps is incomplete.

The KPI feature extends the harness with locked .NET restore, formatting, analyzer build, all test projects, and the repository branch-policy test. Manual usability and guide-following evidence remain explicit gates; automated checks do not fabricate human results.
