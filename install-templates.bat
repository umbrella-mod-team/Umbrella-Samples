@echo off
echo Copying project template to the templates folder...
copy ".\vs-templates\WIGUx Project.zip" "%USERPROFILE%\Documents\Visual Studio 2022\Templates\ProjectTemplates"
echo The file has been copied successfully.
pause >nul