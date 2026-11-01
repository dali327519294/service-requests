# Creates labels, milestones and issues for this repository from issues.json.
# Run once from the repository root:  powershell -ExecutionPolicy Bypass -File tools/github/setup-project.ps1
# Requires: gh CLI, signed in (gh auth login).

$ErrorActionPreference = 'Stop'

# A terminal opened before gh was installed has a stale PATH; reload it from the system settings
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'GitHub CLI (gh) was not found. Install it: winget install GitHub.cli' }

$data = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'issues.json') | ConvertFrom-Json
$repo = gh repo view --json nameWithOwner --jq .nameWithOwner
Write-Host "Repository: $repo"

# gh reads --body-file as UTF-8; temp files keep Hebrew text intact on Windows PowerShell 5.1
$utf8 = New-Object System.Text.UTF8Encoding $false
function Write-TempBody([string] $text) {
    $path = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($path, $text, $utf8)
    return $path
}

Write-Host "`n== Labels"
foreach ($label in $data.labels) {
    gh label create $label.name --color $label.color --description $label.description --force | Out-Null
    Write-Host "  $($label.name)"
}

Write-Host "`n== Milestones"
$existing = gh api "repos/$repo/milestones?state=all&per_page=100" | ConvertFrom-Json
foreach ($m in $data.milestones) {
    if ($existing | Where-Object { $_.title -eq $m.title }) { Write-Host "  exists: $($m.title)"; continue }
    $payload = Write-TempBody (@{ title = $m.title; due_on = $m.due } | ConvertTo-Json -Compress)
    gh api "repos/$repo/milestones" --method POST --input $payload | Out-Null
    Remove-Item $payload
    Write-Host "  $($m.title)"
}

Write-Host "`n== Issues"
foreach ($issue in $data.issues) {
    $bodyFile = Write-TempBody $issue.body
    $ghArgs = @('issue', 'create', '--title', $issue.title, '--body-file', $bodyFile, '--assignee', '@me')
    foreach ($l in $issue.labels) { $ghArgs += @('--label', $l) }
    if ($issue.milestone) { $ghArgs += @('--milestone', $issue.milestone) }
    $url = & gh @ghArgs
    Remove-Item $bodyFile
    $number = ($url -split '/')[-1]
    Write-Host "  #$number  $($issue.title)"

    # Planning issues were already delivered by merged PRs; close them with a link
    if ($issue.closedBy) {
        gh issue close $number --comment "Done in #$($issue.closedBy)." | Out-Null
        Write-Host "        closed (done in PR #$($issue.closedBy))"
    }
}

Write-Host "`nDone. Open: https://github.com/$repo/issues"
