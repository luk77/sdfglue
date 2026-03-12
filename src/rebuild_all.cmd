SET "MSBUILD_EXE=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
SET "SOLUTION=SdfGlue.sln"

:: Restore nuget references
"%MSBUILD_EXE%" -t:restore

:: Build debug
"%MSBUILD_EXE%" -t:Clean,Build "%SOLUTION%" -p:Configuration=Debug -p:Platform="Any CPU"

:: Build release
"%MSBUILD_EXE%" -t:Clean,Build "%SOLUTION%" -p:Configuration=Release -p:Platform="Any CPU"
