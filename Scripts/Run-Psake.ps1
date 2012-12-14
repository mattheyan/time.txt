$psakePath = (Join-Path (Split-Path $MyInvocation.MyCommand.Path) ..\Tools\PSake\psake.psm1)

Import-Module $psakePath

try
{
	$tasksPath = (Join-Path (Split-Path $MyInvocation.MyCommand.Path) PsakeTasks.ps1)

	if ($args.Length -gt 0) {
		# Invoke a specific task
		Invoke-psake $tasksPath ($args -join " ")
	}
	else {
		# Invoke the default task
		Invoke-psake $tasksPath
	}
}
finally
{
	Remove-Module psake
}
