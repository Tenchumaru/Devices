@ECHO OFF
SETLOCAL

IF "%~1" == "" EXIT /B 2
SET CONFIGURATION=%~1
CD /D "%~dp0"
SET O=RegularExpressionParser.g.cs
IF NOT EXIST obj MD obj
"..\Pard\bin\%CONFIGURATION%\net6.0\Pard.exe" --class-declaration="namespace Lad{public partial class RegularExpressionParser" --scanner-class-name=RegularExpressionScanner RegularExpressionParser.xml obj\%O%
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
