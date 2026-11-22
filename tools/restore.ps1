$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
foreach ($package in @('NUnit','NUnitLite')) {
    $target = Join-Path $repo "packages\$package.3.14.0"
    if (-not (Test-Path "$target\lib\net45")) {
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        $url = 'https://api.nuget.org/v3-flatcontainer/' + $package.ToLowerInvariant() + '/3.14.0/' + $package.ToLowerInvariant() + '.3.14.0.nupkg'
        $archive = Join-Path $repo ".runtime\$package.zip"
        New-Item -ItemType Directory -Path (Split-Path $archive) -Force | Out-Null
        Invoke-WebRequest -Uri $url -OutFile $archive
        Expand-Archive -LiteralPath $archive -DestinationPath $target -Force
    }
}
