try {
	# Create variable for program files directory
	# ===========================================
	# Borrowed from BoxStarter.Azure
	if(${env:ProgramFiles(x86)} -ne $null) {
		$programFiles86 = ${env:ProgramFiles(x86)}
	} else {
		$programFiles86 = $env:ProgramFiles
	}

	# Close running application
	if (Get-Process -Name timetxt -ErrorAction SilentlyContinue) {
		Write-Host "Stopping process..."
		taskkill.exe /IM timetxt.exe /F
	}

	# Ensure target directory
	$targetDir = "$programFiles86\Time.txt"
	if (Test-Path $targetDir) {
		Write-Host "Updating application files..."
	}
	else {
		Write-Host "Creating application files..."
	}

	# Extract files
	$toolsDir = Split-Path $MyInvocation.MyCommand.Definition -Parent
	robocopy (Join-Path $toolsDir "bin") (Join-Path $targetDir "bin") /MIR | Out-Null

	$exePath = Join-Path (Join-Path $targetDir "bin") "timetxt.exe"
	$wshShell = New-Object -COMObject WScript.Shell

	# Create startup shortcut
	# http://stackoverflow.com/questions/9701840/how-to-create-a-shortcut-using-powershell-or-cmd
	$currentUser = (Get-WMIObject -class Win32_ComputerSystem | select username).username
	if ($currentUser -match "\\") {
		$currentUser = $currentUser.Substring($currentUser.IndexOf("\") + 1)
	}
	$usersDir = Split-Path $env:USERPROFILE -Parent
	$currentUserDir = Join-Path $usersDir $currentUser
	$currentUserStartupDir = Join-Path $currentUserDir "AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup"
	$startupLinkPath = Join-Path $currentUserStartupDir "Time.txt.lnk"
	$wshShell = New-Object -COMObject WScript.Shell
	$shortcut = $wshShell.CreateShortcut($startupLinkPath)
	$shortcut.TargetPath = $exePath
	$shortcut.Save()

	# Start application before exiting
	Write-Host "Starting application..."
	& $exePath

	write-host "Time.txt is now installed."

	Write-ChocolateySuccess 'time.txt.install'
} catch {
	Write-ChocolateyFailure 'time.txt.install' "$($_.Exception.Message)"
	throw 
}
