@echo off
setlocal enabledelayedexpansion

:: Make all local tools visible using PATH
SET PATH=%PATH%;%~dp0

:: Add all global Yeoman generators
SET NODE_PATH=%AppData%\npm\node_modules;!NODE_PATH!

:: Find all Yeoman generators delivered by NuGet
for /f "tokens=*" %%i in ('dir "%~dp0..\..\packages\Yeoman*" /b /o-n ^| findstr "^Yeoman\."') do (
    SET NODE_PATH=%~dp0..\..\packages\%%i\node_modules;!NODE_PATH!
)

:: Run yo tool
"%~dp0node" "%~dp0..\..\packages\Yeoman.1.1.2\node_modules\yo\cli.js" %*