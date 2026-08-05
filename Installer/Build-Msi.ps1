[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$ProductVersion = "0.0.5",

    [ValidatePattern('^[0-9A-Za-z][0-9A-Za-z._-]*$')]
    [string]$DisplayVersion = "0.0.2",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("x64")]
    [string]$Platform = "x64",

    [ValidateSet("win-x64")]
    [string]$RuntimeIdentifier = "win-x64",

    [switch]$IncludeSymbols,

    [string]$CertificateThumbprint,

    [ValidateSet("CurrentUser", "LocalMachine")]
    [string]$CertificateStoreLocation = "CurrentUser",

    [ValidatePattern('^https?://')]
    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [switch]$RequireSignature
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

function Get-SignToolPath {
    $signToolCommand = Get-Command "signtool.exe" -ErrorAction SilentlyContinue

    if ($signToolCommand -ne $null) {
        return $signToolCommand.Source
    }

    $windowsKitsBin = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"

    if (Test-Path -LiteralPath $windowsKitsBin) {
        $signTool = Get-ChildItem -LiteralPath $windowsKitsBin -Filter "signtool.exe" -File -Recurse |
            Where-Object { $_.DirectoryName -like "*\x64" } |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($signTool -ne $null) {
            return $signTool.FullName
        }
    }

    throw "signtool.exe was not found. Install the Windows SDK Signing Tools component."
}

function Assert-CodeSigningCertificate {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Thumbprint,

        [Parameter(Mandatory = $true)]
        [ValidateSet("CurrentUser", "LocalMachine")]
        [string]$StoreLocation
    )

    $normalizedThumbprint = $Thumbprint.Replace(" ", "").ToUpperInvariant()
    $resolvedStoreLocation = [System.Enum]::Parse(
        [System.Security.Cryptography.X509Certificates.StoreLocation],
        $StoreLocation)
    $certificateStore = [System.Security.Cryptography.X509Certificates.X509Store]::new(
        "My",
        $resolvedStoreLocation)

    try {
        $certificateStore.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadOnly)
        $certificate = $certificateStore.Certificates |
            Where-Object { $_.Thumbprint -eq $normalizedThumbprint } |
            Select-Object -First 1

        if ($certificate -eq $null) {
            throw "Code-signing certificate $normalizedThumbprint was not found in $StoreLocation\My."
        }

        if (-not $certificate.HasPrivateKey) {
            throw "Code-signing certificate $normalizedThumbprint does not have an accessible private key."
        }

        if ($certificate.NotAfter -le [DateTime]::Now) {
            throw "Code-signing certificate $normalizedThumbprint expired on $($certificate.NotAfter)."
        }
    }
    finally {
        $certificateStore.Dispose()
    }
}

function Invoke-CodeSigning {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SignToolPath,

        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Thumbprint,

        [Parameter(Mandatory = $true)]
        [ValidateSet("CurrentUser", "LocalMachine")]
        [string]$StoreLocation,

        [Parameter(Mandatory = $true)]
        [string]$TimestampServer
    )

    $signArguments = @(
        "sign",
        "/sha1",
        $Thumbprint.Replace(" ", ""),
        "/fd",
        "SHA256",
        "/tr",
        $TimestampServer,
        "/td",
        "SHA256"
    )

    if ($StoreLocation -eq "LocalMachine") {
        $signArguments += "/sm"
    }

    $signArguments += $Path
    & $SignToolPath @signArguments

    if ($LASTEXITCODE -ne 0) {
        throw "Code signing failed for $Path with exit code $LASTEXITCODE."
    }

    $signature = Get-AuthenticodeSignature -LiteralPath $Path

    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Signature verification failed for $Path. Status: $($signature.Status)."
    }
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
$signToolPath = $null

if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
    if ($RequireSignature) {
        throw "A certificate thumbprint is required when -RequireSignature is used."
    }

    Write-Warning "Building an unsigned development MSI. Public releases should use -CertificateThumbprint and -RequireSignature."
}
else {
    Assert-CodeSigningCertificate -Thumbprint $CertificateThumbprint -StoreLocation $CertificateStoreLocation
    $signToolPath = Get-SignToolPath
}

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

if ($signToolPath -ne $null) {
    Write-Host "Signing application executables..."
    Invoke-CodeSigning -SignToolPath $signToolPath `
        -Path (Join-Path $publishDirectory "VistaSoftUI.exe") `
        -Thumbprint $CertificateThumbprint `
        -StoreLocation $CertificateStoreLocation `
        -TimestampServer $TimestampUrl
    Invoke-CodeSigning -SignToolPath $signToolPath `
        -Path (Join-Path $publishDirectory "VistaSoftIsoMounter.exe") `
        -Thumbprint $CertificateThumbprint `
        -StoreLocation $CertificateStoreLocation `
        -TimestampServer $TimestampUrl
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

if (-not (Test-Path -LiteralPath $expectedMsiPath)) {
    throw "MSI build completed, but the expected file was not created: $expectedMsiPath"
}

if ($signToolPath -ne $null) {
    Write-Host "Signing MSI package..."
    Invoke-CodeSigning -SignToolPath $signToolPath `
        -Path $expectedMsiPath `
        -Thumbprint $CertificateThumbprint `
        -StoreLocation $CertificateStoreLocation `
        -TimestampServer $TimestampUrl
}

Write-Host "MSI created: $expectedMsiPath"
