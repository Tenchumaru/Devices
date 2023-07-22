@ECHO OFF
SETLOCAL

CD /D "%~dp0"
SET O=RegularExpressionParser.g.cs
IF NOT EXIST obj MD obj
Pard.exe --namespace=Lad --parser-class-name=RegularExpressionParser --scanner-class-name=RegularExpressionScanner RegularExpressionParser.xml obj\%O%
fc obj\%O% %O% > NUL 2> NUL
IF NOT ERRORLEVEL 1 (
	ECHO Same
	DEL /F /Q obj\%O%
	EXIT /B
) ELSE IF NOT EXIST %O% (
	ECHO Created
) ELSE (
	ECHO Changed
)
MOVE /Y obj\%O% %O%
EXIT /B 1
