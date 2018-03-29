# Create variable for program files directory
# NOTE: Borrowed from BoxStarter.Azure
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
Install-ChocolateyShortcut `
	-ShortcutFilePath $startupLinkPath `
	-TargetPath $exePath `
	-WorkingDirectory $env:USERPROFILE

# Start application before exiting
Write-Host "Starting application..."
& $exePath

Write-Host "Time.txt is now installed."

$LASTEXITCODE = 0
