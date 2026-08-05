[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$ProductVersion = "0.0.3",

    [ValidatePattern('^[0-9A-Za-z][0-9A-Za-z._-]*$')]
    [string]$DisplayVersion = "0.0.1",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64")]
    [string]$Platform = "x64",

    [ValidateSet("win-x64")]
    [string]$RuntimeIdentifier = "win-x64",

    [switch]$IncludeSymbols
)

$ErrorActionPreference = "Stop"

function Get-HashHex {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value,

        [Parameter(Mandatory = $true)]
        [ValidateSet("MD5", "SHA256")]
        [string]$Algorithm
    )

    $algorithmInstance = [System.Security.Cryptography.HashAlgorithm]::Create($Algorithm)

    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
        $hashBytes = $algorithmInstance.ComputeHash($bytes)
        return -join ($hashBytes | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        if ($algorithmInstance -ne $null) {
            $algorithmInstance.Dispose()
        }
    }
}

function Get-WixId {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,

        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $hash = (Get-HashHex -Value $Value.ToLowerInvariant() -Algorithm "SHA256").Substring(0, 32)
    return "$Prefix`_$hash"
}

function Get-StableGuid {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $hash = Get-HashHex -Value "VistaSoftAutomatedInstaller/$($Value.ToLowerInvariant())" -Algorithm "MD5"
    $bytes = New-Object byte[] 16

    for ($index = 0; $index -lt 16; $index++) {
        $bytes[$index] = [Convert]::ToByte($hash.Substring($index * 2, 2), 16)
    }

    $bytes[6] = ($bytes[6] -band 0x0f) -bor 0x30
    $bytes[8] = ($bytes[8] -band 0x3f) -bor 0x80

    return ([Guid]::new($bytes)).ToString()
}

function Escape-Xml {
    param(
        [AllowEmptyString()]
        [string]$Value
    )

    return [System.Security.SecurityElement]::Escape($Value)
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Root,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $rootPath = (Resolve-Path $Root).Path.TrimEnd("\")
    return $Path.Substring($rootPath.Length).TrimStart("\")
}

function Add-DirectoryAncestors {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativeDirectory,

        [Parameter(Mandatory = $true)]
        [hashtable]$DirectorySet
    )

    $current = $RelativeDirectory

    while (![string]::IsNullOrWhiteSpace($current)) {
        $DirectorySet[$current] = $true
        $current = Split-Path $current -Parent
    }
}

function New-GeneratedFilesWxs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath,

        [switch]$IncludeSymbols
    )

    $resolvedSourceDirectory = (Resolve-Path $SourceDirectory).Path
    $files = Get-ChildItem -Path $resolvedSourceDirectory -File -Recurse |
        Where-Object { $IncludeSymbols -or $_.Extension -ine ".pdb" } |
        Sort-Object FullName

    if (-not ($files | Where-Object { $_.Name -eq "VistaSoftUI.exe" })) {
        throw "Publish output does not contain VistaSoftUI.exe. Cannot build installer."
    }

    $directorySet = @{}
    $fileEntries = @()

    foreach ($file in $files) {
        $relativePath = Get-RelativePath -Root $resolvedSourceDirectory -Path $file.FullName
        $relativeDirectory = Split-Path $relativePath -Parent

        if (![string]::IsNullOrWhiteSpace($relativeDirectory)) {
            Add-DirectoryAncestors -RelativeDirectory $relativeDirectory -DirectorySet $directorySet
        }

        $fileEntries += [pscustomobject]@{
            RelativePath = $relativePath
            Directory = $relativeDirectory
        }
    }

    $directories = $directorySet.Keys |
        Sort-Object @{ Expression = { $_.Split("\").Count } }, @{ Expression = { $_ } }

    $builder = New-Object System.Text.StringBuilder
    [void]$builder.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    [void]$builder.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
    [void]$builder.AppendLine('  <Fragment>')

    foreach ($directory in $directories) {
        $parentDirectory = Split-Path $directory -Parent
        $parentId = if ([string]::IsNullOrWhiteSpace($parentDirectory)) {
            "INSTALLFOLDER"
        }
        else {
            Get-WixId -Prefix "DIR" -Value $parentDirectory
        }

        $directoryId = Get-WixId -Prefix "DIR" -Value $directory
        $directoryName = Escape-Xml (Split-Path $directory -Leaf)

        [void]$builder.AppendLine("    <DirectoryRef Id=""$parentId"">")
        [void]$builder.AppendLine("      <Directory Id=""$directoryId"" Name=""$directoryName"" />")
        [void]$builder.AppendLine('    </DirectoryRef>')
    }

    [void]$builder.AppendLine('  </Fragment>')
    [void]$builder.AppendLine('  <Fragment>')
    [void]$builder.AppendLine('    <ComponentGroup Id="ApplicationFiles">')

    foreach ($fileEntry in $fileEntries) {
        $normalizedPath = $fileEntry.RelativePath.Replace("/", "\")
        $directoryId = if ([string]::IsNullOrWhiteSpace($fileEntry.Directory)) {
            "INSTALLFOLDER"
        }
        else {
            Get-WixId -Prefix "DIR" -Value $fileEntry.Directory
        }

        $componentId = Get-WixId -Prefix "CMP" -Value $normalizedPath
        $fileId = if ($normalizedPath -ieq "VistaSoftUI.exe") {
            "VistaSoftExecutableFile"
        }
        else {
            Get-WixId -Prefix "FIL" -Value $normalizedPath
        }
        $componentGuid = Get-StableGuid -Value $normalizedPath
        $sourcePath = Escape-Xml $normalizedPath

        [void]$builder.AppendLine("      <Component Id=""$componentId"" Directory=""$directoryId"" Guid=""$componentGuid"" Bitness=""always64"">")
        [void]$builder.AppendLine("        <File Id=""$fileId"" Source=""`$(var.PublishDir)\$sourcePath"" KeyPath=""yes"" />")
        [void]$builder.AppendLine('      </Component>')
    }

    [void]$builder.AppendLine('    </ComponentGroup>')
    [void]$builder.AppendLine('  </Fragment>')
    [void]$builder.AppendLine('</Wix>')

    Set-Content -Path $OutputPath -Value $builder.ToString() -Encoding UTF8
}

$installerRoot = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $installerRoot "..")).Path
$publishDirectory = Join-Path $repoRoot "artifacts\publish\$RuntimeIdentifier"
$msiDirectory = Join-Path $repoRoot "artifacts\msi"
$generatedWxsPath = Join-Path $installerRoot "GeneratedFiles.wxs"
$appProject = Join-Path $repoRoot "VistaSoftUI\VistaSoftUI.csproj"
$isoMounterProject = Join-Path $repoRoot "VistaSoftIsoMounter\VistaSoftIsoMounter.csproj"
$wixProject = Join-Path $installerRoot "VistaSoftInstaller.wixproj"

$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "artifacts"))
$fullPublishDirectory = [System.IO.Path]::GetFullPath($publishDirectory)
$artifactsRootWithSeparator = $artifactsRoot.TrimEnd("\") + "\"

if (!$fullPublishDirectory.StartsWith($artifactsRootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean publish directory outside artifacts: $fullPublishDirectory"
}

if (Test-Path -LiteralPath $fullPublishDirectory) {
    Remove-Item -LiteralPath $fullPublishDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $msiDirectory | Out-Null

Write-Host "Publishing VistaSoftUI to $publishDirectory..."

$publishArguments = @(
    "publish",
    $appProject,
    "-c",
    $Configuration,
    "-p:Platform=$Platform",
    "-r",
    $RuntimeIdentifier,
    "--self-contained",
    "true",
    "-p:WindowsAppSDKSelfContained=true",
    "-p:PublishTrimmed=false",
    "-p:PublishProfile=",
    "-o",
    $publishDirectory
)

& dotnet @publishArguments

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Publishing VistaSoft ISO mounter helper..."

$isoMounterPublishArguments = @(
    "publish",
    $isoMounterProject,
    "-c",
    $Configuration,
    "-r",
    $RuntimeIdentifier,
    "--self-contained",
    "true",
    "-p:PublishSingleFile=false",
    "-p:PublishTrimmed=false",
    "-o",
    $publishDirectory
)

& dotnet @isoMounterPublishArguments

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish for VistaSoftIsoMounter failed with exit code $LASTEXITCODE."
}

Write-Host "Generating WiX component list..."
New-GeneratedFilesWxs -SourceDirectory $publishDirectory -OutputPath $generatedWxsPath -IncludeSymbols:$IncludeSymbols

Write-Host "Building MSI..."

$buildArguments = @(
    "build",
    $wixProject,
    "-c",
    $Configuration,
    "-p:InstallerPlatform=x64",
    "-p:ProductVersion=$ProductVersion",
    "-p:ProductDisplayVersion=$DisplayVersion",
    "-p:PublishDir=$publishDirectory",
    "-p:OutputPath=$msiDirectory"
)

& dotnet @buildArguments

if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$expectedMsiPath = Join-Path $msiDirectory "VistaSoftAutomatedInstaller-$DisplayVersion-x64.msi"

if (Test-Path $expectedMsiPath) {
    Write-Host "MSI created: $expectedMsiPath"
}
else {
    $latestMsi = Get-ChildItem -Path $msiDirectory -Filter "*.msi" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if ($latestMsi -eq $null) {
        throw "MSI build completed, but no .msi file was found in $msiDirectory."
    }

    Write-Host "MSI created: $($latestMsi.FullName)"
}
