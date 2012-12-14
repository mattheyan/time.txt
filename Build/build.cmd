powershell -Command "& {Import-Module ..\Tools\PSake\psake.psm1; Invoke-psake .\PsakeTasks.ps1 Build}"
pause