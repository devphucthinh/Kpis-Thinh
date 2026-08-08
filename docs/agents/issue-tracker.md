# Issue tracker: Local Markdown

Issues and specs for this repository live as Markdown files in `.scratch/`.

## Conventions

- One feature per directory: `.scratch/<feature-slug>/`.
- The feature specification is `.scratch/<feature-slug>/spec.md`.
- Implementation issues are separate files at `.scratch/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01`.
- Record triage state in a `Status:` line near the top of each issue file.
- Append discussion history under a `## Comments` heading.

## Publishing and fetching

When a skill says to publish work to the issue tracker, create the appropriate file and parent directory under `.scratch/`. When it says to fetch a ticket, read the referenced local Markdown file.

## Wayfinding operations

`$wayfinder` uses `.scratch/<effort>/map.md` as its map and `.scratch/<effort>/issues/NN-<slug>.md` as child tickets. Each child ticket records `Type:`, `Status:`, and, when applicable, `Blocked by:`. Claim a ticket before work; resolve it by adding an `## Answer` section, setting `Status: resolved`, and linking the result from the map's decisions section.
