# Contributing Guide

**Document:** `docs/00_Project_Constitution/contributing.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

## 1. Branch Strategy

| Branch | Purpose | Who creates it |
|---|---|---|
| `main` | Always deployable. Protected — no direct pushes. | CI/CD only |
| `develop` | Integration branch. Merges from feature branches. | CI/CD on PR approval |
| `feature/<phase>-<description>` | Feature work. e.g. `feature/phase1-domain-entities` | Developer |
| `fix/<description>` | Bug fixes. e.g. `fix/propertyid-filter-missing` | Developer |
| `chore/<description>` | Non-functional changes. e.g. `chore/update-dependencies` | Developer |

## 2. Commit Message Convention

Format: `<type>(<scope>): <description>`

| Type | When |
|---|---|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code restructure, no behaviour change |
| `test` | Adding or updating tests |
| `chore` | Build scripts, dependencies, tooling |

Examples:
```
feat(complaints): add AssignComplaintCommandHandler with atomic transaction
fix(dispatch): correct idle score division by zero for single candidate pool
docs(api): update v1.yaml with UpdateEta endpoint
test(persistence): add cross-property isolation test for ComplaintRepository
```

## 3. Pull Request Process

1. Branch from `develop` (not `main`).
2. Run `dotnet build backend/ACLS.sln` locally — must compile with zero errors.
3. Run `dotnet test tests/unit/` — all unit tests must pass.
4. Open a PR against `develop` with a description referencing the phase and task.
5. PR requires one reviewer approval before merge.
6. CI runs automatically on PR open — integration tests and contract tests must pass.
7. Squash merge to `develop`.

## 4. AI-Generated Code Policy

All AI-generated code must:
- Pass the phase completion checklist in `docs/00_Project_Constitution/session_prompt_templates.md` Section 5
- Comply with all rules in `docs/00_Project_Constitution/ai_collaboration_rules.md`
- Be reviewed by a human before the PR is approved

AI-generated code that violates a rule from `ai_collaboration_rules.md` must be corrected using the Mid-Session Correction Prompt before the PR is opened.

## 5. Documentation Updates

Any code change that affects an API endpoint, a database column, an entity field, or a domain rule must be accompanied by a documentation update in the same PR. Code and documentation are always kept in sync.

---

*End of Contributing Guide v1.0*
