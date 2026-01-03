#!/usr/bin/env pwsh

# Setup script for development environment
# Run this once after cloning the repo

Write-Host "Setting up FluentRegex development environment..." -ForegroundColor Green

# Check if pre-commit is installed
if (!(Get-Command "pre-commit" -ErrorAction SilentlyContinue)) {
    Write-Host "Installing pre-commit..." -ForegroundColor Yellow

    # Try pip first
    if (Get-Command "pip" -ErrorAction SilentlyContinue) {
        pip install pre-commit
    }
    # Try pipx if available
    elseif (Get-Command "pipx" -ErrorAction SilentlyContinue) {
        pipx install pre-commit
    }
    # Try conda if available
    elseif (Get-Command "conda" -ErrorAction SilentlyContinue) {
        conda install -c conda-forge pre-commit
    }
    else {
        Write-Host "Please install pre-commit manually:" -ForegroundColor Red
        Write-Host "  pip install pre-commit" -ForegroundColor Red
        Write-Host "  or visit: https://pre-commit.com/#installation" -ForegroundColor Red
        exit 1
    }
}

# Install pre-commit hooks
Write-Host "Installing pre-commit hooks..." -ForegroundColor Yellow
pre-commit install

# Install commit-msg hook for conventional commits
pre-commit install --hook-type commit-msg

Write-Host "Development environment setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run 'dotnet restore' to restore packages" -ForegroundColor White
Write-Host "  2. Run 'dotnet build' to build the project" -ForegroundColor White
Write-Host "  3. Run 'dotnet test' to run tests" -ForegroundColor White
Write-Host "  4. Make your first commit - hooks will run automatically" -ForegroundColor White
