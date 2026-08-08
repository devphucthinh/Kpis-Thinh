# Install Spec Kit and Matt Pocock Skills Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Install both upstream skill collections locally in this repository, document one combined development loop, configure their one-time project context, and verify the existing harness.

**Architecture:** Codex discovers every installed skill below `.agents/skills`. Spec Kit owns specification artifacts; Matt Pocock skills own interviewing, TDD implementation, diagnosis, and review; `harness.cmd` remains the executable quality gate.

**Tech Stack:** Codex Agent Skills, GitHub Spec Kit `specify` CLI, Matt Pocock Skills, PowerShell, repository harness

## Global Constraints

- Install skills only under `D:\IDEA\.agents\skills`.
- Fetch only from `github/spec-kit` and `mattpocock/skills`.
- Preserve all pre-existing staged and untracked files.
- Do not modify a global Codex skill directory.
- Use the installed `SKILL.md` metadata as the source of truth for invocation names.
- Do not commit until repository-local Git author identity is configured by the user.

---

### Task 1: Install both repository-scoped skill collections

**Files:**
- Create: `.agents/skills/speckit-*/SKILL.md`
- Create: `.agents/skills/<matt-skill>/SKILL.md`
- Modify: `.gitignore`

**Interfaces:**
- Consumes: GitHub repositories `github/spec-kit` and `mattpocock/skills`
- Produces: repository-scoped skills discoverable by Codex from `.agents/skills`

- [x] **Step 1: Add the temporary tool directory to `.gitignore`**

Add this exact entry:

```gitignore
# Repository-local installer tools
.tools/
```

- [x] **Step 2: Install a repository-local `uv` runtime**

Run the official installer with `UV_INSTALL_DIR=D:\IDEA\.tools\uv` and `UV_NO_MODIFY_PATH=1`. Expected result: `.tools/uv/uv.exe` exists and no global PATH is changed.

- [x] **Step 3: Initialize Spec Kit for Codex skills**

Run:

```powershell
./.tools/uv/uvx.exe --from git+https://github.com/github/spec-kit.git specify init --here --force --integration codex --integration-options="--skills" --script ps --ignore-agent-tools
```

Expected result: Spec Kit creates its `.specify` project files and `speckit-*` directories under `.agents/skills`.

- [x] **Step 4: Install the complete Matt Pocock skill set with `skill-installer`**

Use `install-skill-from-github.py --repo mattpocock/skills --dest D:\IDEA\.agents\skills` with these skill directories:

```text
skills/engineering/ask-matt
skills/engineering/codebase-design
skills/engineering/code-review
skills/engineering/diagnosing-bugs
skills/engineering/domain-modeling
skills/engineering/grill-with-docs
skills/engineering/implement
skills/engineering/improve-codebase-architecture
skills/engineering/prototype
skills/engineering/research
skills/engineering/resolving-merge-conflicts
skills/engineering/setup-matt-pocock-skills
skills/engineering/tdd
skills/engineering/to-spec
skills/engineering/to-tickets
skills/engineering/triage
skills/engineering/wayfinder
skills/engineering/wizard
skills/in-progress/claude-handoff
skills/in-progress/loop-me
skills/in-progress/setup-ts-deep-modules
skills/in-progress/writing-beats
skills/in-progress/writing-fragments
skills/in-progress/writing-shape
skills/misc/git-guardrails-claude-code
skills/misc/migrate-to-shoehorn
skills/misc/scaffold-exercises
skills/misc/setup-pre-commit
skills/productivity/grilling
skills/productivity/grill-me
skills/productivity/handoff
skills/productivity/teach
skills/productivity/to-questionnaire
skills/productivity/wait-what
skills/productivity/writing-for-agents
```

Expected result: 35 Matt Pocock skill directories are installed without modifying global skill locations.

- [x] **Step 5: Verify installed skill structure**

Run a repository scan for `SKILL.md`. Expected result: every direct child installed under `.agents/skills` has a readable `SKILL.md`; the set includes `setup-matt-pocock-skills`, `implement`, `tdd`, `code-review`, `diagnosing-bugs`, and the Spec Kit core skills.

### Task 2: Write the combined Vietnamese workflow guide

**Files:**
- Create: `HUONG_DAN_SPECKIT_MATTPOCOCK.txt`

**Interfaces:**
- Consumes: installed skill names and the responsibility split in `docs/superpowers/specs/2026-08-08-speckit-mattpocock-loop-design.md`
- Produces: a standalone Vietnamese guide for the complete development loop

- [x] **Step 1: Extract exact skill names from installed metadata**

Read every relevant `SKILL.md` frontmatter and use its `name` value in the guide. Do not translate or invent invocation names.

- [x] **Step 2: Write the guide**

Include one-time setup, per-feature flow, copy-ready prompts, human approval checkpoints, ownership rules, the diagnosis path, update instructions, and a daily checklist.

- [x] **Step 3: Check guide invocations against installed metadata**

Extract every `$skill-name` token from the guide and verify that the corresponding skill name exists below `.agents/skills`.

### Task 3: Configure project preferences and verify the repository

**Files:**
- Create or modify: files selected by `setup-matt-pocock-skills`
- Create or modify: `.specify/memory/constitution.md`

**Interfaces:**
- Consumes: installed `setup-matt-pocock-skills` and `speckit-constitution` instructions
- Produces: repository-specific Matt preferences and governing Spec Kit principles

- [ ] **Step 1: Run `setup-matt-pocock-skills` in a new Codex turn**

Follow its installed instructions, discover repository context, confirm preferences with the user, and record them in the paths required by that skill.

- [ ] **Step 2: Run `speckit-constitution` in the same or a later Codex turn**

Base the constitution on `AGENTS.md`, `docs/architecture.md`, and `docs/quality.md`. Preserve the existing harness as the canonical setup and verification interface.

- [x] **Step 3: Run full verification**

Run:

```powershell
./harness.cmd check
```

Expected result: `All harness checks passed.`

- [x] **Step 4: Inspect final repository changes**

Run `git status --short` and confirm that no global skill location changed, no secret-bearing file was added, and all prior user changes remain present.
