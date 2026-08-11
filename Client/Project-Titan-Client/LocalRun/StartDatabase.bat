@echo off
setlocal

set "CLIENT_DIR=%~dp0.."
set "ROOT_DIR=%CLIENT_DIR%\..\.."
set "DB_ENGINE_DIR=%ROOT_DIR%\Database\DynamoDb"

if "%LOCALAPPDATA%"=="" (
    set "DB_DATA_DIR=%ROOT_DIR%\UserData\DynamoDb"
) else (
    set "DB_DATA_DIR=%LOCALAPPDATA%\TrialsOfTitanLocal\DynamoDb"
)

where java >nul 2>nul
if errorlevel 1 (
    echo Java was not found in PATH.
    echo Install Java, then run this script again.
    pause
    exit /b 1
)

if not exist "%DB_ENGINE_DIR%\DynamoDBLocal.jar" (
    echo DynamoDBLocal.jar was not found:
    echo %DB_ENGINE_DIR%\DynamoDBLocal.jar
    pause
    exit /b 1
)

if not exist "%DB_DATA_DIR%" (
    mkdir "%DB_DATA_DIR%"
)

if not exist "%DB_DATA_DIR%\test_us-east-1.db" if exist "%DB_ENGINE_DIR%\test_us-east-1.db" (
    echo Migrating existing local progress database...
    copy "%DB_ENGINE_DIR%\test_us-east-1.db" "%DB_DATA_DIR%\test_us-east-1.db" >nul
)

echo Starting DynamoDB Local on port 8000...
echo Progress database:
echo %DB_DATA_DIR%
echo Keep this window open while playing.
echo.

pushd "%DB_ENGINE_DIR%"
java -D"java.library.path=./DynamoDBLocal_lib" -jar DynamoDBLocal.jar -dbPath "%DB_DATA_DIR%"
popd

echo.
echo DynamoDB Local stopped.
pause
