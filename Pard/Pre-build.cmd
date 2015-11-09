REM(); /*
@ECHO OFF
SETLOCAL

IF "%~1" == "" (
	ECHO usage: %~nx0 ConfigurationName
	EXIT /B 2
)
PATH %PATH%;C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin
CD /D "%~dp0"
SET OUTPUT=YaccInput.xml.cs
SET PARD="..\TestResults\Pard.exe"
SET T=%TEMP%\%RANDOM%.make
find "goto" %OUTPUT% > nul
IF ERRORLEVEL 1 DEL /F /Q %OUTPUT%
IF EXIST %PARD% (
	cscript //nologo //E:JScript %0 YaccInput.xml > %T%
	nmake -nologo "ConfigurationName=%~1" -f %T%
	IF ERRORLEVEL 1 (
		DEL /F /Q %T%
		EXIT /B 1
	) ELSE (
		DEL /F /Q %T%
		EXIT /B 0
	)
) ELSE (
	ECHO XML-only parser
	ECHO using System; > %OUTPUT%
	EXIT /B 0
)
GOTO :EOF
*/
function REM() {
	var xmlFileName = WScript.Arguments(0);
	var csFileName = xmlFileName + ".cs";
	var fout = WScript.StdOut;
	fout.WriteLine(csFileName + ':');
	fout.WriteLine('\tIF EXIST ' + csFileName + ' DEL /F /Q ' + csFileName);
	fout.WriteLine('\t"..\\TestResults\\Pard.exe" --namespace=Pard --parser-class-name=YaccParser --scanner-class-name=YaccScanner ' + xmlFileName + ' ' + csFileName);
}
