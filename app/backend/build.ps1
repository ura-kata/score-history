Param([ValidateSet("build")]$Task="build")

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path -Parent

$BuildDir = (Join-Path $ScriptDir "build")

function Task-Clear(){
    if(Test-Path $BuildDir){
        Remove-Item -Recurse $BuildDir
    }
}
function Task-Build(){
    Task-Clear

    $projectPath = (Join-Path $ScriptDir "src/ScoreHistoryApi/ScoreHistoryApi.csproj")


    # TODO PublishReadyToRun=true にしたときに Lambda が高速化するか検証する
    dotnet publish $projectPath `
        -o $BuildDir `
        -c Release `
        -r linux-x64 `
        -p:PublishReadyToRun=false `
        --self-contained false `
        -p:DebugType=None
}

if($Task -eq "build"){
    Task-Build
}
else{
    Write-Out "'$Task' is not found."
}
