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

	# Delete startup shortcut
	$currentUser = (Get-WMIObject -class Win32_ComputerSystem | select username).username
	if ($currentUser -match "\\") {
		$currentUser = $currentUser.Substring($currentUser.IndexOf("\") + 1)
	}
	$usersDir = Split-Path $env:USERPROFILE -Parent
	$currentUserDir = Join-Path $usersDir $currentUser
	$currentUserStartupDir = Join-Path $currentUserDir "AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup"
	$startupLinkPath = Join-Path $currentUserStartupDir "Time.txt.lnk"
	if (Test-Path $startupLinkPath) {
		Write-Host "Deleting startup shortcut link..."
		Remove-Item $startupLinkPath
	}

	# Remove application binaries
	Write-Host "Removing application files..."
	$targetDir = "$programFiles86\Time.txt"
	Remove-Item $targetDir -Recurse -Force | Out-Null

	write-host "Time.txt is now uninstalled."

	Write-ChocolateySuccess 'time.txt.install'
} catch {
	Write-ChocolateyFailure 'time.txt.install' "$($_.Exception.Message)"
	throw 
}
