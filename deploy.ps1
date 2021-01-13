$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$workDir = Join-Path $scriptDir 'backend/PracticeManagerApi/src/PracticeManagerApi'

Set-Location $workDir

dotnet lambda deploy-serverless -sn "pm-stack" -sb "pm-stack-bucket"
