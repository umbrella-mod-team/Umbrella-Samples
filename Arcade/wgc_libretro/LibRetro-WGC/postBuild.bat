set PACKAGE_DIR="%~p1\..\Staging"
set OUTPUT_DIR="%~p1\.."
set RETROARCH_DIR="F:\games\emulators\RetroArch-Win64"

mkdir %PACKAGE_DIR%
mkdir %PACKAGE_DIR%\cores
mkdir %PACKAGE_DIR%\info

copy %1 %PACKAGE_DIR%\cores
copy %1 %RETROARCH_DIR%\cores
copy %2*.info %PACKAGE_DIR%\info
copy %2*.info %RETROARCH_DIR%\info
copy %3\README.md %PACKAGE_DIR%
copy %3\partials-example.txt %PACKAGE_DIR%
"C:\Program Files\7-Zip\7z.exe" a "%OUTPUT_DIR%\LibRetro-WindowCast.7z" -r "%PACKAGE_DIR%\*.*"