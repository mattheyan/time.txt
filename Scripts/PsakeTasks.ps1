# Clean solution tasks
######################

task CleanDebug {
	Clean "..\Source\TimeTxt.sln" -config "Debug"
}

task CleanRelease {
	Clean "..\Source\TimeTxt.sln" -config "Release"
}

task Clean -depends CleanDebug

task CleanAll -depends CleanDebug,CleanRelease

# Build solution tasks
######################

task BuildDebug {
	Compile "..\Source\TimeTxt.sln" -config "Debug"
}

task BuildRelease {
	Compile "..\Source\TimeTxt.sln" -config "Release"
}

task Build -depends BuildDebug

task BuildAll -depends BuildDebug,BuildRelease

# Testing tasks
###############

task ApproveOutput -depends BuildDebug {
	Run { & "$xunitToolsPath\xunit.console.exe" ..\Source\TimeTxt.ApprovalTests\bin\Debug\TimeTxt.ApprovalTests.dll }
}

task CheckFacts -depends BuildDebug {
	Run { & "$xunitToolsPath\xunit.console.exe" ..\Source\TimeTxt.Facts\bin\Debug\TimeTxt.Facts.dll }
}

task Test -depends CheckFacts,ApproveOutput

# Deploy tasks
##############

task Deploy -depends BuildAll,Test {
	$x86Dir = "C:\Program Files (x86)"
	if (Test-Path $x86Dir) {
		$deployPath = $x86Dir + "\Time.txt"
	}
	else {
		$deployPath = "C:\Program Files\Time.txt"
	}

	write-host "Detected deploy path: '$deployPath'."

	if (Test-Path $deployPath) {
		# Empty existing deploy directory
		write-host "Deleting existing files..."
		Get-ChildItem -Path $deployPath | foreach { Remove-Item -Path $_.FullName }
	}
	else {
		# Create new deploy directory
		write-host "Creating deploy directory..."
		New-Item $deployPath -type directory
	}

	write-host "Copying new files..."
	Get-ChildItem -Path ..\Source\TimeTxt.Exe\bin\Debug -Exclude *.vshost* | foreach { Copy-Item -Path $_.FullName -Destination $deployPath }

	write-host "Done!"
}

# Package tasks
###############

task Package -depends BuildAll,Test {
	$tempFile = [System.IO.Path]::GetTempFileName()
	$packageDir = $tempFile.Substring(0, $tempFile.Length - 4)

	Write-Host "Packaging in '$($packageDir)'..."
	
	if (Test-Path $packageDir) {
		Write-Host "Deleting existing artifacts..."
		Remove-Item $packageDir -Recurse -Force | Out-Null
		Remove-Item $packageDir -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
	}

	# Create .pack folders
	New-Item $packageDir\tools -Type Directory -Force | Out-Null

	# Copy the application binaries into the pack directory
	Write-Host "Copying binaries to the pack directory..."
	$targetDir = Resolve-Path $packageDir\tools
	$binDir = Join-Path $targetDir "bin"
	New-Item $binDir -Type Directory | Out-Null
	robocopy (Resolve-Path ..\Source\Timetxt.Exe\bin\Release) $binDir /xf *vshost* /MIR | Out-Null

	# Temporarily move to the pack directory and run the package command
	Write-Host "Moving files for $PackageType package..."
	Copy-Item ..\Time.txt.Install.nuspec $packageDir | Out-Null
	Write-Host "Copying installer script..."
	Copy-Item ..\chocolateyInstall.ps1 $packageDir\tools\chocolateyInstall.ps1 | Out-Null
	Write-Host "Copying uninstaller script..."
	Copy-Item ..\chocolateyUninstall.ps1 $packageDir\tools\chocolateyUninstall.ps1 | Out-Null

	# Take from 'Test-Admin' from Boxstarter...
	$identity  = [System.Security.Principal.WindowsIdentity]::GetCurrent()
	$principal = New-Object System.Security.Principal.WindowsPrincipal( $identity )
	if (!($principal.IsInRole( [System.Security.Principal.WindowsBuiltInRole]::Administrator ))) {
		throw "Command `choco pack` must be run as administrator."
	}

	Write-Host "Packing..."
	Push-Location $packageDir
	try {
		cpack
	}
	finally {
		Pop-Location
	}

	# Copy the resulting package into the root directory
	Move-Item $packageDir\*.nupkg ..\ -Force | Out-Null
}

# Common helper functions
#########################

function Run ([scriptblock]$block)
{
	& $block
	if ($LastExitCode -ne 0) {
		write-output "Process failed"
		exit 1
	}
}

function Compile ([string]$target, [string]$config="Debug")
{
	Run { & "$msBuildPath\msbuild.exe" /property:Configuration=$config $target }
}

function Clean ([string]$target, [string]$config="Debug")
{
	Run { & "$msBuildPath\msbuild.exe" /t:Clean /property:Configuration=$config $target }
}

function GetRegistryKeyValue ([string]$path, [string]$name)
{
	(Get-ItemProperty -Path $path -Name $name).$name
}

# Common variables
##################

$msBuildPath = (GetRegistryKeyValue hklm:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0 "MSBuildToolsPath")

$xunitToolsPath = (resolve-path ..\Source\packages\xunit.runners.1.9.1\tools\).Path
