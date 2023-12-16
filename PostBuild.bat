@echo off
copy /Y %1 "../ModBuilt/BepinEx/Plugins/%~nx1"
robocopy /E "../UnityFiles/Bundles" "../ModBuilt/BepinEx/Plugins/Bundles"

::echo Patching Networking...
::call "..\ILNetPatcher.bat" %1

echo Creating mod
cd ../ModBuilt

IF EXIST "LC_CosmicAPI.zip" (
    DEL "LC_CosmicAPI.zip"
)

"C:\Program Files\7-Zip\7z" a -tzip -mx=9 -r LC_CosmicAPI.zip .

echo Done

