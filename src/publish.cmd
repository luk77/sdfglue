del ..\bin\*.dll
del ..\bin\*.exe
del ..\bin\SdfGlue.deps.json
del ..\bin\SdfGlue.runtimeconfig.json

dotnet publish SdfGlueEditor\SdfGlueEditor.csproj -c Release -r win-x64 -p DebugType=none -p DebugSymbols=false -o ..\bin

