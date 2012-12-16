powershell -Command "& {Import-Module .\Tools\PSake\psake.psm1; Invoke-psake .\Scripts\PsakeTasks.ps1 BuildRelease}"
pause