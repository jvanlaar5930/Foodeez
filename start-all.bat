@echo off
echo Starting Foodeez development environment...

echo Starting API (http://localhost:5000)...
start "Foodeez API" cmd /k "cd /d %~dp0apps\api\src && dotnet run --project Foodeez.API"

echo Waiting 3 seconds before starting frontend...
timeout /t 3 /nobreak >nul

echo Starting Web (http://localhost:5173)...
start "Foodeez Web" cmd /k "cd /d %~dp0 && npm run web"

echo Starting Mobile (Expo)...
start "Foodeez Mobile" cmd /k "cd /d %~dp0 && npm run mobile"

echo.
echo All 3 projects are starting in separate windows.
echo   API:    http://localhost:5000
echo   Web:    http://localhost:5173
echo   Mobile: Expo DevTools will open in your browser
echo.
echo Press any key to close this window...
pause >nul
