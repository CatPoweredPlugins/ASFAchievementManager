@echo off
rem getting current dir name by Tamara Wijsman, https://superuser.com/questions/160702
for %%I in (.) do set CurrDirName=%%~nxI

rem download submodule
if not exist ArchiSteamFarm\ArchiSteamFarm (git submodule update --init)

if [%1]==[] goto noarg
rem update submodule to required tag, if specified...
git submodule foreach "git fetch origin; git checkout %1;"
goto continue
:noarg
rem ...otherwise update submodule to latest tag 
git submodule foreach "git fetch origin; git checkout $(git rev-list --tags --max-count=1);"
:continue
rem print what version we are building for
git submodule foreach "git describe --tags;"

rem wipe out old build
if exist out rmdir /Q /S out

rem release generic version

dotnet publish -c "Release" -f "net5.0" -o "out/generic" "/p:LinkDuringPublish=false"
mkdir .\out\%CurrDirName%
copy .\out\generic\%CurrDirName%.dll .\out\%CurrDirName%
7z a -tzip -mx7 .\out\%CurrDirName%.zip .\out\%CurrDirName%
rmdir /Q /S out\%CurrDirName%

rem release generic-netf version
rem comment section below if you don't target netf ASF version

dotnet publish -c "Release" -f "net48" -o "out/generic-netf"
mkdir .\out\%CurrDirName%
copy .\out\generic-netf\%CurrDirName%.dll .\out\%CurrDirName%
7z a -tzip -mx7 .\out\%CurrDirName%-netf.zip .\out\%CurrDirName%
rmdir /Q /S out\%CurrDirName%