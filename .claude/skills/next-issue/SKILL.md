# Next Issue

## Purpose

Select and explain the next actionable GitHub Issue for the `WebsiteHeaderAnalyzer` project. The selection must use the repository's current GitHub Issues, their type, dependencies, and the project's priority rules. Do not use a fixed workshop sequence or assume that Issue numbers will follow a particular order.

This Skill only identifies and explains the next task. It does not implement the task or change the repository or GitHub Issues.

## Repository

Use this repository as the canonical source:

- Repository: `Nokk1251/WebsiteHeaderAnalyzer`
- Issues: <https://github.com/Nokk1251/WebsiteHeaderAnalyzer/issues>

Do not substitute another repository, including `WebsiteSecurityHeaderAnalyzer`, unless the user explicitly changes the repository.

## Instructions

### 1. Retrieve the current GitHub Issues

1. Use GitHub CLI (`gh`) to verify access to `Nokk1251/WebsiteHeaderAnalyzer`.
2. Retrieve the repository's Issues, including open and closed Issues, so dependencies can be checked:

   ```bash
   gh issue list --repo Nokk1251/WebsiteHeaderAnalyzer --state all --limit 200 --json number,title,body,state,labels,milestone,createdAt,url
   ```

3. Use the actual Issue details as the source of truth. When the list output is insufficient, retrieve the full Issue:

   ```bash
   gh issue view ISSUE_NUMBER --repo Nokk1251/WebsiteHeaderAnalyzer
   ```

4. Consider only open Issues as possible tasks. Closed Issues may still be relevant when checking whether a dependency has already been completed.
5. Do not rely on remembered Issue numbers, previous conversations, cached statuses, or a hardcoded task sequence.

### 2. Understand each Issue's type and scope

Inspect the Issue's title, description, acceptance criteria, labels, milestone, and dependencies.

The repository uses labels such as:

- `type:bug`, `type:feature`, `type:refactor`, `type:chore`, `type:docs` (and other `type:*` labels that may be introduced).
- `priority:high`, `priority:medium`, `priority:low`.
- `area:*` labels such as `area:http`, `area:headers`, `area:cli`, and `area:ci`.

Use the actual labels that exist on the Issue. Do not invent labels or claim that an Issue has a label when it does not.

### 3. Apply the project's priority rules

Determine the Issue's **calculated priority** from its type using this policy. These rules define the project's default priority and are used to rank Issues:

| Issue type | Default priority | Rule |
|---|---|---|
| Bug (`type:bug`) | **HIGH** | Bugs have the highest normal priority because existing behavior is incorrect or broken. |
| Security defect (`type:security` or a security-related bug) | **HIGH** | Treat confirmed security defects as bugs. Explain the impact; do not exaggerate an unverified risk. |
| Feature (`type:feature`) | **MEDIUM** | New functionality has medium priority by default. |
| Refactor (`type:refactor`) | **MEDIUM** | Internal code improvements have medium priority when they support reliability, testability, or maintainability. |
| Tests (`type:test`) | **MEDIUM** | Tests that protect existing or required behavior have medium priority. Small optional coverage improvements may be LOW. |
| Chore / maintenance (`type:chore`) | **LOW** | Routine maintenance is low priority. Raise it to MEDIUM only when it blocks normal development, build, or test workflows. |
| Documentation (`type:docs`) | **LOW** | Documentation and presentation improvements are low priority unless missing documentation prevents setup or normal use. In that case use MEDIUM. |
| Unknown or missing type | **Unclassified** | Do not guess when the type is unclear. Explain the missing classification and ask for clarification if it affects which Issue should be selected. |

Priority order is `HIGH` → `MEDIUM` → `LOW`.

Important rules:

- A feature remains MEDIUM by default, even if an existing `priority:high` label says otherwise. Do not silently change the GitHub label. Report the mismatch between the label and the calculated priority so the user can decide whether the label should be corrected.
- Apply the same mismatch check to every type. The calculated priority comes from this policy; the existing `priority:*` label is repository metadata and must be reported separately.
- Do not create or apply a `critical` priority label unless that label already exists and the user has explicitly established its meaning. In this repository, HIGH is the highest normal priority in the policy.
- Do not assign HIGH merely because an Issue is in the MVP milestone or is described as important. Use the type-based rules above and explain any exceptional blocker relationship separately.
- Do not confuse severity of a review finding with the priority of a GitHub Issue. This Skill prioritizes work items, not individual code-review findings.

### 4. Respect dependencies and blockers

1. Read dependency information in the Issue body, especially sections such as `Dependencies`, `Depends on`, and `Blocks`.
2. If an open Issue depends on another open Issue, it is blocked and is not yet the next actionable task.
3. Prefer the open prerequisite that unblocks dependent work. A prerequisite may therefore be selected before a higher-ranked Issue that cannot be started until that prerequisite is complete. Explain this dependency-based decision explicitly.
4. If the dependency is closed, treat that prerequisite as completed and continue evaluating the dependent Issue.
5. If a referenced dependency cannot be found or its state cannot be verified, report the uncertainty. Do not assume that it is complete.
6. Do not select an Issue whose only next step is blocked by an unresolved prerequisite when that prerequisite can be identified and is still open.

### 5. Select the next actionable Issue

After retrieving the Issues and checking their dependencies:

1. Exclude closed Issues.
2. Identify open Issues that are actionable (their required dependencies are closed or absent).
3. Rank actionable Issues by calculated priority: HIGH first, then MEDIUM, then LOW.
4. If multiple actionable Issues have the same calculated priority, prefer the Issue that unblocks the greatest number of other open Issues. If still tied, prefer the lower Issue number (the older Issue) for deterministic selection.
5. If there are no actionable Issues because all open work is blocked, identify the blocking dependency and explain what prevents selection.
6. If there are no open Issues, report that the repository currently has no open Issues. Do not invent a new task.
7. If the next candidate has an unclassified type and its classification could change the selection, do not guess. Explain the ambiguity and ask the user to classify it.

Do not skip a HIGH-priority actionable bug to select a MEDIUM-priority feature. Do not skip an actionable prerequisite that blocks the work selected by the priority rules.

### 6. Explain the selected Issue in Bulgarian

The explanation must include:

- The actual GitHub Issue number and a direct link.
- The exact title from GitHub.
- The Issue type and `area:*` labels, if present.
- The existing `priority:*` label, if present.
- The calculated priority under Section 3.
- A clear note if the existing priority label conflicts with the calculated priority.
- The context and goal, summarized faithfully from the Issue description.
- The acceptance criteria, preserving their meaning and not adding requirements.
- Dependencies, blockers, and relevant prerequisites.
- Why this Issue is the next task under the priority and dependency rules.

Use the actual GitHub Issue content as the source of truth. If acceptance criteria are missing, state that they are not explicitly defined. Distinguish facts from interpretation and recommendations.

### 7. Do not implement the selected Issue

This Skill only selects and explains the next Issue. Do not:

- Edit project files.
- Create or switch Git branches.
- Modify, create, close, label, or comment on GitHub Issues.
- Commit or push changes.
- Create or merge Pull Requests.
- Start implementation automatically.

End by waiting for the user to decide whether implementation should begin.

## Error handling

- If GitHub CLI (`gh`) is unavailable, report the problem and explain that live Issue status could not be checked.
- If GitHub authentication fails, report the problem.
- If the repository cannot be accessed or identified, report the exact repository and ask the user to resolve access or repository context.
- If retrieving Issues or a specific Issue fails, report the command failure and do not claim that the Issue data was verified.
- If the repository contains more Issues than were retrieved, retrieve the remaining pages or increase the limit before deciding. Do not select from an incomplete list.
- Never fall back to guessing based on previous conversations or cached Issue statuses.

## Output

Clearly announce the selected Issue and its calculated priority. Then explain its requirements, labels, dependencies, and why it is next.

If the selected Issue's GitHub priority label conflicts with the type-based policy, show both values separately and point out the discrepancy without modifying the Issue.

End by waiting for the user to decide when implementation should begin. Do not start implementation automatically.
