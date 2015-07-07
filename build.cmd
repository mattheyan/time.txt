powershell -Command "& {Import-Module %~dp0\Tools\PSake\psake.psm1; Invoke-psake %~dp0\Scripts\PsakeTasks.ps1 Build}"
pause