SET "MSBUILD_EXE=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
SET "SOLUTION=SdfGlue.sln"

:: Clean debug
"%MSBUILD_EXE%" -t:Clean "%SOLUTION%" -p:Configuration=Debug -p:Platform="Any CPU"

:: Clean release
"%MSBUILD_EXE%" -t:Clean "%SOLUTION%" -p:Configuration=Release -p:Platform="Any CPU"