<#
.SYNOPSIS
    Publishes dotnet packages for supported .NET 5 compatible operating systems
.DESCRIPTION
    Publishes dotnet packages for supported .NET 5 compatible operating systems
.NOTES
    Created by   : asheroto
    Date Coded   : 01/26/2020
    More info:   : https://gist.github.com/asheroto/b8c82ea515e8baa569807108d1d9ed0a
#>

# Change the variables below, then run the script
$DebugOrRelease = "Release"                                                 # Debug or Release
$SolutionOrProjectPath = "C:\Projects\GenerateCerts\GenerateCerts.sln"    # Solution or Project file you want to publish
$PublishPath = "C:\Projects\GenerateCerts\bin\Release\net5.0\publish"      # The directory you want the published files to go in
$BinaryName = "GenerateCerts"                                              # Binary name from build, without the .exe extension
$PublishSingleFile = "true"                                                 # If true, produces a single file
$PublishTrimmed = "true"                                                    # If true, reduces the size of the assembly
$IncludeNativeLibrariesForSelfExtract = "true"                              # If true, compiles the native libraries inside of assembly
$PublishReadyToRun = "false"                                                # If true, compiles with AOT (ahead of time) optimization
# Change the variables above, then run the script

function Publish {
    param (
        [String] $Runtime
    )

    Write-Host $Runtime

    # Publish
    dotnet publish -r $Runtime -c $DebugOrRelease -p:PublishSingleFile=$PublishSingleFile -p:PublishTrimmed=$PublishTrimmed -p:IncludeNativeLibrariesForSelfExtract=$IncludeNativeLibrariesForSelfExtract -p:PublishReadyToRun=$PublishReadyToRun --nologo --output $PublishPath $SolutionOrProjectPath

    if ($Runtime.Contains("win-")) {
        # If architecture is Windows
        $OriginalBinaryName = $BinaryName + ".exe"
        $TargetBinaryName = $BinaryName + "_" + $Runtime + ".exe"
    } else {
        # If architecture not Windows
        $OriginalBinaryName = $BinaryName
        $TargetBinaryName = $BinaryName + "_" + $Runtime
    }

    # Rename original build name to build name + architecture
    Rename-Item ($PublishPath + "\" + $OriginalBinaryName) $TargetBinaryName

    Write-Host "-----------------------------------"
}

# Delete existing files in with binary name in publish path
if(Test-Path ($PublishPath)) {
    try {
        Remove-Item ($PublishPath + "\" + $BinaryName + "*")
    } catch {
        
    }
}

# Publish binaries
# See runtime identifiers here: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
Publish -Runtime win-x64
Publish -Runtime win-x86
Publish -Runtime win-arm
Publish -Runtime win-arm64
Publish -Runtime linux-x64
Publish -Runtime linux-arm
Publish -Runtime linux-arm64
Publish -Runtime osx-x64

# Open folder to publish path
Start-Process explorer.exe -ArgumentList $PublishPath