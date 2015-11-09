@ECHO OFF
SETLOCAL

IF "%~1" == "" (
	ECHO usage: %~nx0 ConfigurationName
	EXIT /B 2
)
CD /D "%~dp0"
IF NOT EXIST ..\TestResults MD ..\TestResults
COPY /Y "..\Pard\bin\%~1\*.dll" ..\TestResults
COPY /Y "..\Pard\bin\%~1\Pard.exe" ..\TestResults
