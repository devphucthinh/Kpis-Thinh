# Triage labels

The repository uses these canonical triage roles for local Markdown issues.

| Role | Local status | Meaning |
| --- | --- | --- |
| `needs-triage` | `needs-triage` | Maintainer needs to evaluate the issue. |
| `needs-info` | `needs-info` | Waiting for more information. |
| `ready-for-agent` | `ready-for-agent` | Fully specified and ready for an agent. |
| `ready-for-human` | `ready-for-human` | Requires human implementation or decision. |
| `wontfix` | `wontfix` | Will not be actioned. |

When a skill assigns a role, write the matching value in the issue's `Status:` line. Keep this mapping stable unless the repository adopts a different tracker.
