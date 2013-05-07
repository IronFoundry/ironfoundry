$proto_dir = '.\warden\warden-protocol\lib\warden\protocol\pb'
$protogen_exe = Get-ChildItem -Recurse -Include protogen.exe | Select-Object -First 1 -ExpandProperty FullName

if (!(Test-Path $protogen_exe))
{
    Write-Error "Could not find 'protogen.exe', exiting."
    exit 1
}

if (!(Test-Path $proto_dir))
{
    Write-Error "Directory '$proto_dir' containing *.proto files does not exist, exiting."
    exit 1
}

Push-Location -Verbose $proto_dir

$protogen_args = @()
$proto_files = Get-ChildItem -File -Filter '*.proto'
foreach ($proto_file in $proto_files)
{
    $in_name = $proto_file.Name
    $protogen_args += "-i:$in_name"
}

$protogen_output = & $protogen_exe -q $protogen_args

Pop-Location -Verbose

Set-Content Messages.cs $protogen_output.Replace('namespace warden','namespace IronFoundry.WardenProtocol').Replace('warden.','')
