msbuild ..\Core\Moth.Core.csproj /p:Configuration=Release /tv:3.5

ilmerge.exe /out:Output/Moth.dll "..\Core\bin\release\Moth.dll" "..\Core\bin\release\BoneSoft.CSS.dll" "..\Core\bin\release\EcmaScript.NET.modified.dll" "..\Core\bin\release\Yahoo.Yui.Compressor.dll"