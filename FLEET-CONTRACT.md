# Agent Fleet Contract

Use this checklist when creating or updating any agent in this fleet.

## Core Contract

- Frontmatter is valid YAML and includes `name`, `description`, `model`, and `tools`.
- Tool names use the current capability names. Use `vscode/memory`, not `memory`.
- Instructions clearly separate role, boundaries, workflow, and completion signals.
- Completion signals match the role-specific contract below.
- Escalation guidance is based on observable conditions, not internal model heuristics.
- Skill loading prefers repo-local files in `.github/skills/<skill-name>/SKILL.md` first, with user-level fallback only when the repo-local skill is unavailable.

## Completion Signals By Role

- Clarifier: `Status: CLEAR`, `Status: CLARIFIED`, `Status: NEEDS_REVIEW`, `Status: BLOCKED`
- Planner: `DONE`, `NEEDS_REVIEW`, `BLOCKED`
- Implementation agents: `DONE`, `REVIEW_REQUESTED`, `ESCALATION_NEEDED`, `BLOCKED`
- Designer: `DONE`, `REVIEW_REQUESTED`, `ESCALATION_NEEDED`, `BLOCKED`
- Data Engineer: `DONE`, `REVIEW_REQUESTED`, `ESCALATION_NEEDED`, `BLOCKED`
- DevOps: `DONE`, `REVIEW_REQUESTED`, `ESCALATION_NEEDED`, `BLOCKED`
- Prompt Writer: `DONE`, `REVIEW_REQUESTED`, `BLOCKED`
- Reviewer: `PASS`, `ISSUES_FOUND`, `BLOCKED`
- Test Engineer: `DONE`, `FAILURES_FOUND`, `ESCALATION_NEEDED`, `BLOCKED`

## Observable Escalation Rules

- Escalate for scope boundaries, security-sensitive work, architectural decisions, repeated failed attempts, or a true mismatch in specialist domain.
- Do not escalate based on internal model state such as context-window percentage, token pressure, or similar non-observable heuristics.
- Use `BLOCKED` only when external input, missing access, or missing artifacts prevent progress.

## Planning Requirements

- Every Planner step must include a concrete outcome.
- Every Planner step must include at least one file assignment.
- The Planner must emit a dependency graph, risk level, advisory tier, and open questions section.
- Open questions must be surfaced before execution continues.

## Review And Test Handshake

- Reviewer findings use `ISSUES_FOUND`, not `NEEDS_REVIEW`.
- Test regressions use `FAILURES_FOUND`.
- Pre-existing test failures are reported separately and do not count as regressions.

## Publishing Checklist

- Install links in `agents/README.md` point to `.github/agents/<agent-file>.md` in the raw GitHub URL.
- Agent descriptions in the roster match the actual agent role.
- New repo guidance documents should be added only when they define durable fleet behavior, not task summaries.
