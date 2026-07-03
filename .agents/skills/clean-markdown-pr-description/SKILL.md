# Clean Markdown PR Description

## Purpose
Ensure pull request descriptions render correctly in GitHub and never contain escaped newline sequences like `\n`.

## Rules
1. Always write PR body as real multiline markdown, not a single escaped string.
2. Use this section order:
   - `## Summary`
   - `## Scope`
   - `## Validation`
3. In `Summary`, use short bullet points with concrete changes.
4. In `Scope`, reference linked issue closure keywords when appropriate (e.g., `Closes #123`).
5. In `Validation`, list exact commands that were run.

## Safe CLI Pattern
Prefer `--body-file` over inline `--body`:

```powershell
$body = @'
## Summary
- change 1
- change 2

## Scope
Closes #123

## Validation
- dotnet test OmniVibe.sln
'@
$tmp = Join-Path $env:TEMP 'pr-body.md'
Set-Content -Path $tmp -Value $body -NoNewline
gh pr edit 123 --body-file $tmp
Remove-Item $tmp -ErrorAction SilentlyContinue
```

## Quality Check
After creating/updating PR body, verify render content:

```powershell
gh pr view <number> --json body
```

The body must contain actual line breaks, not literal `\n`.
