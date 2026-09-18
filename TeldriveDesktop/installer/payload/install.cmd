@echo off
setlocal
set "APPDIR=%LOCALAPPDATA%\Teldrive Desktop"
if not exist "%APPDIR%" mkdir "%APPDIR%"
xcopy "%~dp0*.*" "%APPDIR%\" /I /Y /Q >nul
del /Q "%APPDIR%\install.cmd" 2>nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$w=New-Object -ComObject WScript.Shell; $s=$w.CreateShortcut($w.SpecialFolders('Programs')+'\Teldrive Desktop.lnk'); $s.TargetPath=$env:LOCALAPPDATA+'\Teldrive Desktop\TeldriveDesktop.exe'; $s.WorkingDirectory=$env:LOCALAPPDATA+'\Teldrive Desktop'; $s.Save()"
start "" "%APPDIR%\TeldriveDesktop.exe"
exit /b 0
