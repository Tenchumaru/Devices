@ECHO OFF
SETLOCAL

IF "%~1" == "" (
	ECHO usage: %~nx0 ConfigurationName
	EXIT /B 2
)
CD /D "%~dp0"
SET PARD="..\Pard\bin\%~1\Pard.exe"
SET T=%TEMP%\%RANDOM%.cs
SET OUTPUT=YaccInput.xml.cs
IF EXIST %PARD% (
	%PARD% --namespace=Pard.Test --parser-class-name=YaccParser --scanner-class-name=YaccScanner YaccInput.xml %T%
	IF NOT EXIST %OUTPUT% (
		ECHO Creating %OUTPUT%
		MOVE /Y %T% %OUTPUT%
		EXIT /B 1
	) ELSE (
		FC %T% %OUTPUT% > nul
		IF ERRORLEVEL 1 (
			ECHO Updating %OUTPUT%
			MOVE /Y %T% %OUTPUT%
			EXIT /B 1
		) ELSE (
			ECHO No change to %OUTPUT%
			DEL /F /Q %T%
			EXIT /B 0
		)
	)
) ELSE (
	ECHO XML-only parser
	EXIT /B 0
)
