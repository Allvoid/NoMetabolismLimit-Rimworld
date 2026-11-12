@echo off
rem Builds NoMetabolismLimit.dll twice and copies the mod into the RimWorld Mods folders:
rem   NoMetabolismLimit\1.6\Assemblies - RimWorld 1.6 (MSBuild, .NET Framework 4.7.2, against the 1.6 copy W:\SteamLibrary\RimWorld-1.6),
rem   NoMetabolismLimit\1.7\Assemblies - RimWorld 1.7 (dotnet, .NET 10, against the Steam folder, branch unstable).
rem LoadFolders.xml picks the right one for the running game. The mod folder also holds the sources; only
rem About, Languages, 1.6, 1.7 and LoadFolders.xml are installed.
setlocal
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set MODS=W:\SteamLibrary\steamapps\common\RimWorld\Mods
set MODS16=W:\SteamLibrary\RimWorld-1.6\Mods
set SRC=%~dp0NoMetabolismLimit

%MSBUILD% "%SRC%\NoMetabolismLimit\NoMetabolismLimit.csproj" -p:Configuration=Release -nologo -v:minimal || exit /b 1
dotnet build "%SRC%\NoMetabolismLimit.1.7\NoMetabolismLimit.1.7.csproj" -c Release -nologo -v:minimal || exit /b 1
call :install "%MODS%" || exit /b 1
if exist "%MODS16%" call :install "%MODS16%" || exit /b 1
exit /b 0

:install
set DEST=%~1\NoMetabolismLimit
rem The old layout had the dll in Assemblies at the top; it would load next to the 1.6/1.7 one.
if exist "%DEST%\Assemblies\NoMetabolismLimit.dll" del "%DEST%\Assemblies\NoMetabolismLimit.dll"
if exist "%DEST%\Assemblies" rmdir "%DEST%\Assemblies"
for %%D in (1.6 1.7 Languages) do (
    robocopy "%SRC%\%%D" "%DEST%\%%D" /MIR /NFL /NDL /NJH /NJS /NP /R:3 /W:2
    if errorlevel 8 goto :busy
)
rem About: no purge, the game keeps PublishedFileId.txt there.
robocopy "%SRC%\About" "%DEST%\About" /E /NFL /NDL /NJH /NJS /NP /R:3 /W:2
if errorlevel 8 goto :busy
copy /y "%SRC%\LoadFolders.xml" "%DEST%\LoadFolders.xml" >nul || goto :busy
echo Installed to %DEST%
exit /b 0

:busy
echo Could not copy the mod to %DEST%: close RimWorld, it keeps the dll open.
exit /b 1
