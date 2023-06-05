$xml = [Xml] (Get-Content ./FSharpMajor.fsproj)
$xml.Project.PropertyGroup.Version