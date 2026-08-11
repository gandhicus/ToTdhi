@echo off
setlocal

set "SCRIPT_DIR=%~dp0"

echo Starting local database, server, and client.
echo.
echo This opens separate windows for DynamoDB and the server.
echo Leave both windows open while playing.
echo.

start "Trials DynamoDB Local" cmd /k ""%SCRIPT_DIR%StartDatabase.bat""

echo Waiting for DynamoDB Local...
timeout /t 5 /nobreak >nul

start "Trials Local Server" cmd /k ""%SCRIPT_DIR%StartServer.bat""

echo Waiting for local server...
timeout /t 8 /nobreak >nul

call "%SCRIPT_DIR%StartClient.bat"
