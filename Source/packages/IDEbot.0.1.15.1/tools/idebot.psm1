function Set-AssemblyVersion {
	param(
		[string]$Project=$null,
		[Parameter(Mandatory=$true)]
		[int]$Major,
		[Parameter(Mandatory=$true)]
		[int]$Minor,
		[Parameter(Mandatory=$true)]
		[int]$Build,
		[Parameter(Mandatory=$true)]
		[int]$Revision
	)
	
	if ($Major -lt 0) {
		throw "Major version number cannot be negative: $Major."
	}
	if ($Minor -lt 0) {
		throw "Minor version number cannot be negative: $Minor."
	}
	if ($Build -lt 0) {
		throw "Build number cannot be negative: $Build."
	}
	if ($Revision -lt 0) {
		throw "Revision number cannot be negative: $Revision."
	}
	
	if ($Project) {	
		if (!(Test-Path $Project)) {
			throw "The path $Project is not a valid path."
		}
	
		$Project = (Resolve-Path $Project)
	
		# TODO: Assumes C# projects, need to get the list of valid project extensions.
		if (!$Project.EndsWith(".csproj")) {
			throw "The path $Project is not a project file."
		}
	}
	else {
		$Project = (Join-Path (Split-Path $MyInvocation.MyCommand.Path) Find-Project.ps1)
	}
	
	$assemblyInfoPath = (Join-Path (Split-Path $Project -Parent) Properties\AssemblyInfo.cs)
	
	if (!(Test-Path $assemblyInfoPath)) {
		throw "Could not find file assembly info file for project $assemblyInfoPath."
	}
	
	$assemblyInfoContent = Get-Content $assemblyInfoPath
	$versionAttribute = $assemblyInfoContent -match '\[\s*assembly\s*:\s*AssemblyVersion\s*\(\s*\"(\d+\.\d+\.\d+\.\d+)\"\s*\)\s*\]'
	if (!$versionAttribute) {
		throw "Could not find AssemblyVersion attribute in file $assemblyInfoPath."
	}
	else {
		$replaceExpr = "[assembly: AssemblyVersion(""$($Major).$($Minor).$($Build).$($Revision)"")]"
		$assemblyInfoContent -replace '\[\s*assembly\s*:\s*AssemblyVersion\s*\(\s*\".*\"\s*\)\s*\]', $replaceExpr | out-file $assemblyInfoPath
	}
}

function Remove-Project {
	param(
		[Parameter(Mandatory=$true)]
		[string]$ProjectName
	)
	
	if (!$DTE -or !$DTE.ExecuteCommand) {
		throw "Must be run from within the package manager console."
	}
	
	write-host "Moving focus from current window to solution explorer..."
	$currentWindow = $DTE.ActiveWindow
	$DTE.ExecuteCommand("View.SolutionExplorer")
	$currentlySelectedItem = $DTE.ActiveWindow.Object.SelectedItems[0]
	Start-Sleep -s 1
	
	write-host "Searching for project matching name $($ProjectName)..."
	$DTE.ActiveWindow.Object.UIHierarchyItems.UIHierarchyItems | foreach {
		if ($_.Name -eq $ProjectName) {
			write-host "Selecting source project $($_.Name)..."
			$_.Select("vsUISelectionTypeSelect")
			Start-Sleep -s 1
			write-host "Removing project $($ProjectName)..."
			$DTE.ExecuteCommand("Edit.Delete")
		}
	}
	
	write-verbose "Restoring previously selected item and window..."
	$currentlySelectedItem.Select("vsUISelectionTypeSelect")
	$currentWindow.Activate()
}

function Use-PackageSource {
	param(
		[string]$Package="Chronos",
		[string]$PackageSourceProject="..\..\Chronos\Chronos.csproj",
		[switch]$AllProjects
	)
	
	if (!$DTE -or !$DTE.ExecuteCommand) {
		throw "Must be run from within the package manager console."
	}
	
	if (!(Test-Path $PackageSourceProject)) {
		throw "The path $PackageSourceProject is not a valid project path."
	}
	
	$PackageSourceProject = (Resolve-Path $PackageSourceProject)
	
	# TODO: Assumes C# projects, need to get the list of valid project extensions.
	if (!$PackageSourceProject.EndsWith(".csproj")) {
		throw "The path $PackageSourceProject is not a project file."
	}
	
	write-host "Verifying that assembly name matches package name..."
	$projectXml = [xml](Get-Content $PackageSourceProject)
	$assemblyName = ($projectXml.Project.PropertyGroup.AssemblyName | ? { $_ -ne $null }).Trim()
	if ($assemblyName -ne $Package) {
		throw "Package name $Package does not match assembly name $assemblyName."
	}
	
	write-host "Getting the current version of the source project..."
	$sourceVersion = Get-AssemblyVersion -Project $PackageSourceProject
	write-host "Current version is $sourceVersion."
	write-host
	
	write-host "Searching for project(s) that will reference the source project..."
	if ($AllProjects.IsPresent) {
		$targetProjects = Get-Project -All | ? { (Get-Package -ProjectName $_.ProjectName | ? { $_.Id -eq $Package }) } | % { $_.ProjectName }
		write-host "Will update all project(s) that currently reference the package: $($targetProjects -join ', ')."
	}
	else {
		$currentProject = (Get-Project).ProjectName
		if (Get-Package -ProjectName $currentProject | ? { $_.Id -eq $Package }) {
			$targetProjects = @($currentProject)
			write-host "Will update the currently selected project: $($currentProject)."
		}
		else {
			throw "Project $currentProject does not reference package $Package."
		}
	}
	
	if (!$targetProjects) {
		throw "Package $Package is not used by any of the projects in the solution."
	}
	
	write-host
	
	write-host "Searching for version conflicts..."
	$targetProjects | foreach {
		$projectName = $_
		Get-Package -ProjectName $projectName | ? { $_.Id -eq $Package } | foreach {
			if ($_.Version -ne $sourceVersion) {
				write-warning "Project $projectName references version $($_.Version) of package $Package, which is different than the current version $sourceVersion."
				
				$choices = New-Object System.Collections.ObjectModel.Collection[System.Management.Automation.Host.ChoiceDescription]
				$choices.Add((New-Object "System.Management.Automation.Host.ChoiceDescription" -ArgumentList @("&Yes", "Confirms that the proposed action will be taken.")))
				$choices.Add((New-Object "System.Management.Automation.Host.ChoiceDescription" -ArgumentList @("&No", "The proposed action will not be taken.")))
	
				$result = $host.ui.PromptForChoice("", "Do you want to continue?", $choices, 1)
	
				if ($result -ne 0) {
					exit
				}
			}
		}
	}
	
	write-host
	
	write-host "Attempting to add the source project to the solution..."
	$currentWindow = $DTE.ActiveWindow
	
	write-host "Moving focus to solution explorer..."
	$DTE.ExecuteCommand("View.SolutionExplorer")
	$currentlySelectedItem = $DTE.ActiveWindow.Object.SelectedItems[0]
	
	write-host "Selecting the solution node..."
	$solutionName = $DTE.Solution.FullName.Split("\")[-1].Split(".")[0]
	$DTE.ActiveWindow.Object.GetItem($solutionName).Select("vsUISelectionTypeSelect")
	Start-Sleep -s 1
	
	write-host "Adding project $Package to the solution..."
	$DTE.ExecuteCommand("File.AddExistingProject", $PackageSourceProject)
	
	write-verbose "Restoring previously selected item and window..."
	$currentlySelectedItem.Select("vsUISelectionTypeSelect")
	$currentWindow.Activate()
	
	write-host
	
	$targetProjects | foreach {
		$projectName = $_
		write-host "Updating $($projectName)..."
		write-host "Uninstalling package $($Package)..."
		Uninstall-Package -ProjectName $projectName -Id $Package
		write-host "Adding project reference to $($projectName)..."
		Add-ProjectReference -ProjectName $projectName -ReferenceName $Package
	}
	
	write-host
	
	write-host "Saving all changes..."
	$DTE.ExecuteCommand("File.SaveAll")
	
	write-host "DONE!"
	write-host
}

function Update-AssemblyVersion {
	param(
		[string]$Project=$null,
		[ValidateSet("BuildNumber", "BuildAndRevisionNumbers")]
		[string]$Type="BuildAndRevisionNumbers"
	)
	
	if ($Project) {
		if (!(Test-Path $Project)) {
			throw "The path $Project is not a valid path."
		}
	
		$Project = (Resolve-Path $Project)
	
		# TODO: Assumes C# projects, need to get the list of valid project extensions.
		if (!$Project.EndsWith(".csproj")) {
			throw "The path $Project is not a project file."
		}
	}
	else {
		$Project = Find-Project
	}
	
	$version = Get-AssemblyVersion -Project $Project
	
	$targetBuild = $version.Build + 1
	$targetRevision = $version.Revision
	
	if ($Type -eq "BuildAndRevisionNumbers") {
		$targetRevision += 1
	}
	
	Set-AssemblyVersion -Project $Project -Major $version.Major -Minor $version.Minor -Build $targetBuild -Revision $targetRevision
}

function Remove-PackageSource {
	param(
		[string]$Package="Chronos",
		[string]$Source=$null
	)
	
	if (!$DTE -or !$DTE.ExecuteCommand) {
		throw "Must be run from within the package manager console."
	}
	
	$packageSourceProject = Get-Project -Name $Package
	write-host "Removing source project located at $($packageSourceProject.FullName)..."
	
	write-host "Verifying that assembly name matches package name..."
	$projectXml = [xml](Get-Content $packageSourceProject.FullName)
	$assemblyName = ($projectXml.Project.PropertyGroup.AssemblyName | ? { $_ -ne $null }).Trim()
	write-host "Assembly name is $assemblyName."
	
	if ($assemblyName -ne $Package) {
		throw "Package name $Package does not match assembly name $assemblyName."
	}
	
	write-host
	
	write-host "Getting the current version of the source project..."
	$sourceVersion = Get-AssemblyVersion -Project $packageSourceProject.FullName
	write-host "Current version is $sourceVersion."
	write-host
	
	write-host "Searching for projects that reference project $($Package)..."
	$packageProjectObject = Get-ProjectObject -Name $Package
	$targetProjects = Get-Project -All | ? {
		(Get-ProjectObject -Name $_.ProjectName).Object.References | ? { $_.SourceProject -eq $packageProjectObject }
	} | % {
		write-host "Project $($_.ProjectName) will be altered to reference NuGet package."
		$_.ProjectName
	}
	
	if (!$targetProjects) {
		throw "Package $Package is not used by any of the projects in the solution."
	}
	
	write-host
	
	write-host "Attempting to remove the source project from the solution..."
	Remove-Project -ProjectName $Package
	write-host
	
	$targetProjects | foreach {
		write-host "Installing package $Package in project $($_.ProjectName)..."
		if ($Source) {
			Install-Package $Package -ProjectName $_ -Source $Source
		}
		else {
			Install-Package $Package -ProjectName $_
		}
	}
	
	write-host
	
	write-host "Saving all changes..."
	$DTE.ExecuteCommand("File.SaveAll")
	
	write-host "DONE!"
	write-host
}

function Find-Project {
	param(
		[string]$StartIn=$null
	)
	
	if (!$StartIn) {
		$StartIn = (Get-Location)
	}
	
	$location = $StartIn
	$lastLocation = $null
	
	while (!(Get-ChildItem $location "*.csproj")) {
		if (!$location -or $location -eq $lastLocation) {
			throw "Could not find project file from $StartIn."
		}
		$lastLocation = $location
		$location = (Split-Path $location -Parent)
	}
	
	$projects = [array](Get-ChildItem $location "*.csproj" | % { $_.FullName })
	
	if ($projects.Length -eq 1) {
		$projects[0]
	}
	elseif ($projects.Length -gt 1) {
		throw "Found multiple projects: $projects."
	}
}

function Add-ProjectReference {
	param(
		[Parameter(Mandatory=$true)]
		[string]$ProjectName,
		
		[Parameter(Mandatory=$true)]
		[string]$ReferenceName
	)
	
	if (!$DTE -or !$DTE.ExecuteCommand) {
		throw "Must be run from within the package manager console."
	}
	
	if ($ProjectName -eq $Reference) {
		throw "Cannot add a reference to self."
	}
	
	$referenceProject = Get-ProjectObject -Name $ReferenceName
	$project = Get-ProjectObject -Name $ProjectName
	
	Write-Verbose "Adding project reference from $($ProjectName) to $($ReferenceName)..."
	$project.Object.References.AddProject($referenceProject) | out-null
}

function Get-ProjectObject {
	param(
		[Parameter(Mandatory=$true)]
		[string]$Name
	)
	
	if (!$DTE -or !$DTE.ExecuteCommand) {
		throw "Must be run from within the package manager console."
	}
	
	$DTE.Solution.Projects | ? { $_.ProjectName -eq $Name } | foreach {
		$project = $_
	}
	
	if (!$project) {
		throw "Project $Name could not be found."
	}
	
	return $project
}

function Get-AssemblyVersion {
	param(
		[string]$Project=$null
	)
	
	if ($Project) {	
		if (!(Test-Path $Project)) {
			throw "The path $Project is not a valid path."
		}
	
		$Project = (Resolve-Path $Project)
	
		# TODO: Assumes C# projects, need to get the list of valid project extensions.
		if (!$Project.EndsWith(".csproj")) {
			throw "The path $Project is not a project file."
		}
	}
	else {
		$Project = Find-Project
	}
	
	$assemblyInfoPath = (Join-Path (Split-Path $Project -Parent) Properties\AssemblyInfo.cs)
	
	if (!(Test-Path $assemblyInfoPath)) {
		throw "Could not find file assembly info file for project $Project."
	}
	
	$assemblyInfoContent = Get-Content $assemblyInfoPath
	$versionAttribute = $assemblyInfoContent -match '\[\s*assembly\s*:\s*AssemblyVersion\s*\(\s*\"(\d+\.\d+\.\d+\.\d+)\"\s*\)\s*\]'
	if (!$versionAttribute) {
		throw "Could not find AssemblyVersion attribute in file $assemblyInfoPath."
	}
	else {
		$versionText = $versionAttribute -replace '\[\s*assembly\s*:\s*AssemblyVersion\s*\(\s*\"(\d+\.\d+\.\d+\.\d+)\"\s*\)\s*\]', '$1'
		$parts = $versionText.Split(".")
		
		$major = [int]::Parse($parts[0])
		$minor = [int]::Parse($parts[1])
		$build = [int]::Parse($parts[2])
		$revision = [int]::Parse($parts[3])
		
		New-Object -Type System.Version -ArgumentList @($major, $minor, $build, $revision)
	}
}

export-modulemember -function Add-ProjectReference
export-modulemember -function Find-Project
export-modulemember -function Get-AssemblyVersion
export-modulemember -function Get-ProjectObject
export-modulemember -function Remove-PackageSource
export-modulemember -function Remove-Project
export-modulemember -function Set-AssemblyVersion
export-modulemember -function Update-AssemblyVersion
export-modulemember -function Use-PackageSource
