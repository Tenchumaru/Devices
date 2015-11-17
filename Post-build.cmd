@ECHO OFF
SETLOCAL

IF "%~1" == "" GOTO :usage
IF NOT "%~3" == "" GOTO :usage
CD /D "%~dp0"
IF NOT EXIST TestResults MD TestResults
COPY /Y "%~1\bin\%~2\*.dll" TestResults
COPY /Y "%~1\bin\%~2\%~1.exe" TestResults
IF "%~2" == "Release" COPY /Y "%~1\bin\%~2\%~1.exe" \local\bin
EXIT /B 0
:usage
ECHO usage: %~nx0 TargetName ConfigurationName
EXIT /B 2
