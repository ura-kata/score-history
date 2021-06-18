Param([ValidateSet("build")]$Task="build")

$WorkDir = Split-Path $MyInvocation.MyCommand.Path -Parent

$OldDir = Get-Location

Set-Location $WorkDir

try{
    yarn install
    yarn build
}
finally{
    Set-Location $OldDir
}
