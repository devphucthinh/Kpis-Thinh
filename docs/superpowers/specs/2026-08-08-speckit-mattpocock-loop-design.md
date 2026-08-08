# Spec Kit + Matt Pocock Development Loop

- Status: Approved for review
- Date: 2026-08-08
- Scope: Repository-local installation in `D:\IDEA`

## Outcome

Install GitHub Spec Kit and the complete Matt Pocock skill collection as repository-scoped Codex skills. Add a Vietnamese plain-text guide that explains how to use both collections as one repeatable development loop.

## Sources

- Spec Kit: `https://github.com/github/spec-kit`
- Matt Pocock skills: `https://github.com/mattpocock/skills`

Only content from these upstream repositories will be installed. No similarly named third-party package will be used.

## Installation boundary

Both collections will be installed under `D:\IDEA\.agents\skills` so they are discoverable by Codex only when working in this repository and can be version-controlled with the project.

No skill will be installed into the user's global Codex directories. Any temporary installer runtime or download cache must remain untracked and must not become an application dependency.

## Responsibility split

Spec Kit owns the durable Spec-Driven Development artifacts:

1. project constitution;
2. feature specification;
3. ambiguity clarification;
4. technical plan;
5. actionable tasks;
6. cross-artifact consistency analysis.

Matt Pocock skills own engineering feedback loops:

1. interviewing and alignment through `ask-matt` or `grill-with-docs`;
2. domain terminology and decision capture;
3. implementation through `implement` and `tdd`;
4. debugging through `diagnosing-bugs`;
5. final diff review through `code-review`.

The repository harness owns the final executable gate through `./harness.cmd check`.

This split prevents both collections from trying to orchestrate the same implementation phase. Spec Kit's generated tasks are the input to Matt Pocock's implementation workflow; Matt Pocock's implementation and review evidence determines whether the Spec Kit task is complete.

## Development loop

### One-time repository setup

1. Run `setup-matt-pocock-skills` and record repository-specific preferences.
2. Run `speckit-constitution` to establish governing project principles.
3. Confirm the repository harness succeeds.

### Per-feature loop

1. Use `ask-matt` or `grill-with-docs` to resolve intent, vocabulary, boundaries, and important decisions.
2. Use `speckit-specify` to create the feature specification.
3. Use `speckit-clarify` until material ambiguity is removed.
4. Use `speckit-plan` to produce the technical plan.
5. Use `speckit-tasks` to create ordered work items.
6. Use `speckit-analyze` to check the constitution, specification, plan, and tasks for contradictions or gaps.
7. Stop for human approval before implementation.
8. Use Matt Pocock's `implement`; it should consume the approved Spec Kit artifacts and execute vertical slices using `tdd`.
9. Use `code-review` against both repository standards and the originating specification.
10. Run `./harness.cmd check`.
11. If a defect remains, use `diagnosing-bugs`, add a regression test, update affected Spec Kit artifacts, and repeat from the earliest invalidated phase.
12. Stop for human approval before considering the feature complete.

## Guide file

Create `D:\IDEA\HUONG_DAN_SPECKIT_MATTPOCOCK.txt` in Vietnamese. It will include:

- installation verification;
- one-time setup;
- the per-feature loop with exact Codex skill invocations;
- copy-ready example prompts;
- approval checkpoints;
- ownership rules that prevent duplicate orchestration;
- failure and debugging paths;
- instructions for updating the installed skills;
- a short daily-use checklist.

The guide must be understandable without reading this design document.

## Error handling

- If an upstream repository or required skill path cannot be fetched, stop and report the exact missing source rather than substituting a different package.
- If a skill name differs in the installed upstream version, use the installed metadata as the source of truth and update the guide accordingly.
- If Spec Kit initialization would overwrite existing repository files, inspect the proposed changes first and preserve unrelated work.
- If the two collections provide overlapping orchestration, follow the responsibility split above and document which entrypoint should not be used in the combined loop.

## Verification

Installation is complete when:

1. every installed skill directory contains a readable `SKILL.md`;
2. Spec Kit's core Codex skills are present under `.agents/skills`;
3. the complete selected Matt Pocock skill set is present, including its repository setup skill;
4. the guide names only invocations that exist in the installed metadata;
5. no global Codex skill directory was modified;
6. `./harness.cmd check` succeeds;
7. `git status --short` shows only the intended repository-scoped additions and pre-existing user changes.
