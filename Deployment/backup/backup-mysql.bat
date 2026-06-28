@echo off
REM Match IQ MySQL backup (Windows / XAMPP)
set BACKUP_DIR=%~dp0..\..\Backups
set MYSQL_BIN=C:\xampp\mysql\bin\mysqldump.exe
set MYSQL_USER=root
set MYSQL_PASS=
set MYSQL_DB=game

if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

for /f "tokens=1-3 delims=/ " %%a in ('date /t') do set MYDATE=%%c%%a%%b
for /f "tokens=1-2 delims=: " %%a in ('time /t') do set MYTIME=%%a%%b
set OUT=%BACKUP_DIR%\%MYSQL_DB%_%MYDATE%_%MYTIME%.sql

"%MYSQL_BIN%" -u%MYSQL_USER% %MYSQL_DB% > "%OUT%"
echo Backup saved: %OUT%
