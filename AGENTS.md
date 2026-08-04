# Visible Subtask Governance

This repository is managed through a visible manager-and-subtask workflow in the Codex desktop project `White_Happy_Room`.

## Mandatory workflow

1. Every development stage must create visible Codex project tasks in the `White_Happy_Room` sidebar before implementation begins.
2. Visible tasks must be separated by responsibility, such as implementation, scene integration, and QA.
3. Feature code and feature tests are implemented by the assigned visible tasks. The manager coordinates scope and ownership.
4. The manager reviews diffs, resolves only necessary integration conflicts, orders final Unity tests/builds, approves results, commits, and pushes.
5. Internal collaboration agents may assist with analysis, but they never replace the visible project tasks required above.
6. Each visible task reports changed files, tests attempted, remaining issues, external assets, and license status.
7. Subtasks do not commit or push unless the manager explicitly instructs them. Final Git approval belongs to the manager.
8. A stage is not complete until the manager verifies compilation, relevant automated tests, Windows build, runtime smoke, and a clean Git scope.
9. The manager reports completion to the user and waits for approval before starting the next stage.

## Cost and asset rule

- Prototype development cost remains zero.
- Use only project-owned content or assets explicitly verified for free commercial use.
- Record every external asset and its license before integration.
