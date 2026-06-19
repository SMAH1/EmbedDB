set shell := ["powershell", "-Command"]

[private]
default:
    #! powershell
    $output = just --list | Select-Object -Skip 1 | fzf
    if (-not $output) { exit 0 }
    $recipe = $output.Trim().Split(' ')[0]
    Clear-Host
    just $recipe

_final:
    #! powershell
    Write-Host ""
    Write-Host -ForegroundColor yellow "Press any key to exit ..."
    Write-Host ""
    [System.Console]::ReadKey($true) | Out-Null

# Publish AOT (Windows)
Publish: _restore _publish_win _final

# Publish AOT (Windows)
Example: _restore _publish_example_win _final

# Restor All project
Restore: _restore _final

# Format C# code and other file (use eclint)
CodeFormat: _code_formatting _final

_code_formatting:
    #! powershell
    $root = "{{justfile_directory()}}"
    Write-Host "Formatting C# code ..."
    dotnet format
    Write-Host "Formatting other file (by eclint) ..."
    eclint fix "**/*" "!node_modules/**" "!.git/**" "!dist/**" "!.vs/**" "!**/*.cs"
    git add --renormalize .

_restore:
    #! powershell
    $root = "{{justfile_directory()}}"
    dotnet restore

_publish_win:
    #! powershell
    $root = "{{justfile_directory()}}"
    New-Item -ItemType Directory -Force -Path "$root\publish" | Out-Null
    Push-Location "$root\src"; dotnet publish -c Release -r win-x64 -o "$root\publish"; Pop-Location

_publish_example_win:
    #! powershell
    $root = "{{justfile_directory()}}"
    New-Item -ItemType Directory -Force -Path "$root\publish" | Out-Null
    Push-Location "$root\example"; dotnet publish -c Release -r win-x64 -o "$root\publish"; Pop-Location
