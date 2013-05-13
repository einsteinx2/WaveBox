REM This is a launcher script for WaveBox server for Windows
REM It toggles WaveBox, so it checks if the exe is running, and if so kills it, if not, launches it

echo off

set process="WaveBoxService.exe"
set ignore_result=INFO:

for /f "usebackq" %%A in (`tasklist /nh /fi "imagename eq %process%"`) do if not %%A==%ignore_result% GOTO kill

start WaveBoxService.exe
GOTO end

:kill

taskkill /F /IM WaveBoxService.exe

:end