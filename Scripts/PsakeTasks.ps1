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
	Run { ..\Source\packages\xunit.runners.1.9.1\tools\xunit.console.exe ..\Source\TimeTxt.ApprovalTests\bin\Debug\TimeTxt.ApprovalTests.dll }
}

task CheckFacts -depends BuildDebug {
	
	Run { ..\Source\packages\xunit.runners.1.9.1\tools\xunit.console.exe ..\Source\TimeTxt.Facts\bin\Debug\TimeTxt.Facts.dll }
}

task Test -depends CheckFacts,ApproveOutput

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

$msBuildPath = (GetRegistryKeyValue hklm:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0 "MSBuildToolsPath")
