#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Deletes all GitHub workflow runs except the most recent one for the current repository.

.DESCRIPTION
    This script uses the GitHub CLI to fetch all workflow runs for the current repository
    and deletes all runs except the most recent one. It handles pagination to ensure
    all workflow runs are processed.

.PARAMETER DryRun
    If specified, the script will only show what would be deleted without actually deleting anything.

.PARAMETER Force
    If specified, skips confirmation prompts.

.EXAMPLE
    .\cleanup-workflow-runs.ps1
    Interactively delete all workflow runs except the most recent one.

.EXAMPLE
    .\cleanup-workflow-runs.ps1 -DryRun
    Show what would be deleted without actually deleting anything.

.EXAMPLE
    .\cleanup-workflow-runs.ps1 -Force
    Delete all workflow runs except the most recent one without confirmation.

.NOTES
    Requires GitHub CLI (gh) to be installed and authenticated.
    Run 'gh auth login' first if not already authenticated.
#>

[CmdletBinding()]
param(
    [switch]$DryRun,
    [switch]$Force
)

# Check if GitHub CLI is available
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed or not in PATH. Please install it from https://cli.github.com/"
    exit 1
}

# Check if authenticated
try {
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Not authenticated with GitHub CLI. Please run 'gh auth login' first."
        exit 1
    }
}
catch {
    Write-Error "Failed to check GitHub CLI authentication status: $($_.Exception.Message)"
    exit 1
}

# Get all workflow runs
Write-Host "Fetching all workflow runs..." -ForegroundColor Cyan
$allRuns = gh api "repos/{owner}/{repo}/actions/runs" --paginate --jq '.workflow_runs' | ConvertFrom-Json

if ($allRuns.Count -eq 0) {
    Write-Host "No workflow runs found." -ForegroundColor Yellow
    exit 0
}

# Sort runs by creation date (most recent first)
$sortedRuns = $allRuns | Sort-Object created_at -Descending

Write-Host "Found $($sortedRuns.Count) workflow runs" -ForegroundColor Green

if ($sortedRuns.Count -le 1) {
    Write-Host "Only one or no workflow runs found. Nothing to delete." -ForegroundColor Yellow
    exit 0
}

# Keep the most recent run, delete the rest
$mostRecentRun = $sortedRuns[0]
$runsToDelete = $sortedRuns[1..($sortedRuns.Count - 1)]

Write-Host "`nMost recent run (will be kept):" -ForegroundColor Green
Write-Host "  ID: $($mostRecentRun.id)" -ForegroundColor White
Write-Host "  Workflow: $($mostRecentRun.name)" -ForegroundColor White
Write-Host "  Status: $($mostRecentRun.status)" -ForegroundColor White
Write-Host "  Created: $($mostRecentRun.created_at)" -ForegroundColor White
Write-Host "  Branch: $($mostRecentRun.head_branch)" -ForegroundColor White

Write-Host "`nWorkflow runs to delete ($($runsToDelete.Count)):" -ForegroundColor Red
foreach ($run in $runsToDelete | Select-Object -First 10) {
    Write-Host "  ID: $($run.id) | Workflow: $($run.name) | Status: $($run.status) | Created: $($run.created_at) | Branch: $($run.head_branch)" -ForegroundColor Gray
}

if ($runsToDelete.Count -gt 10) {
    Write-Host "  ... and $($runsToDelete.Count - 10) more runs" -ForegroundColor Gray
}

if ($DryRun) {
    Write-Host "`n[DRY RUN] Would delete $($runsToDelete.Count) workflow runs" -ForegroundColor Yellow
    exit 0
}

# Confirmation
if (-not $Force) {
    $confirmation = Read-Host "`nDo you want to delete $($runsToDelete.Count) workflow runs? (y/N)"
    if ($confirmation -notmatch '^[Yy]$') {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Delete workflow runs
Write-Host "`nDeleting workflow runs..." -ForegroundColor Red
$deletedCount = 0
$failedCount = 0

foreach ($run in $runsToDelete) {
    try {
        Write-Host "Deleting run $($run.id) ($($run.name) - $($run.created_at))..." -ForegroundColor Yellow
        gh api "repos/{owner}/{repo}/actions/runs/$($run.id)" --method DELETE
        
        if ($LASTEXITCODE -eq 0) {
            $deletedCount++
            Write-Host "  ✓ Deleted" -ForegroundColor Green
        } else {
            $failedCount++
            Write-Host "  ✗ Failed to delete" -ForegroundColor Red
        }
    }
    catch {
        $failedCount++
        Write-Host "  ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Small delay to avoid rate limiting
    Start-Sleep -Milliseconds 100
}

# Summary
Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "  Successfully deleted: $deletedCount" -ForegroundColor Green
Write-Host "  Failed to delete: $failedCount" -ForegroundColor Red
Write-Host "  Kept most recent run: 1" -ForegroundColor Yellow

if ($failedCount -eq 0) {
    Write-Host "`nCleanup completed successfully!" -ForegroundColor Green
} else {
    Write-Host "`nCleanup completed with some errors. Check the output above for details." -ForegroundColor Yellow
}
