Param([ValidateSet("build", "clear")]$task="build")

$buildDir  = "./build"

$archiveFileName = "set-cookie-lambda.zip"


function BuildTask(){
    $archiveFilePath = Join-Path $buildDir $archiveFileName
    $archiveDir = Join-Path $buildDir "archive"
    if(!(Test-Path -Path "${archiveDir}")){
        New-Item -ItemType Directory -Path "${archiveDir}" -Force
    }

    Copy-Item -Destination "${archiveDir}" -Path ./node_modules -Recurse -Force
    Copy-Item -Destination "${archiveDir}" -Path ./index.js
    Copy-Item -Destination "${archiveDir}" -Path ./package.json
    Copy-Item -Destination "${archiveDir}" -Path ./yarn.lock
    7z a "${archiveFilePath}" (Join-Path "${archiveDir}" "*")
}

function ClearTask(){
    if (Test-Path -Path "${buildDir}"){
        Remove-Item -Path "${buildDir}" -Force -Recurse
    }
}

if($task -eq "build"){
    ClearTask
    BuildTask
}
elseif($task -eq "clear"){
    ClearTask
}
