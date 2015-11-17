@ECHO OFF
SETLOCAL

IF "%~1" == "" (
	ECHO usage: %~nx0 ConfigurationName
	EXIT /B 2
)
CD /D "%~dp0"
IF NOT EXIST ..\TestResults MD ..\TestResults
COPY /Y "..\Lad\bin\%~1\*.dll" ..\TestResults
COPY /Y "..\Lad\bin\%~1\Lad.exe" ..\TestResults
