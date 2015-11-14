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
REM I need Lad to create the scanner for the Yacc parser.
IF EXIST "..\TestResults\Lad.exe" (
	IF EXIST %PARD% (
		cscript //nologo //E:JScript %0 > %T%
		nmake -nologo "ConfigurationName=%~1" -f %T%
		IF ERRORLEVEL 1 (
			DEL /F /Q %T%
			EXIT /B 1
		) ELSE (
			DEL /F /Q %T%
			EXIT /B 0
		)
	)
)
ECHO XML-only parser
ECHO using System; > YaccInput.l.cs
COPY /Y YaccInput.txt %OUTPUT%
EXIT /B 0
*/
function REM() {
	var yaccXmlFileName = "YaccInput.xml", lexLexFileName = "YaccInput.l";
	var yaccCsFileName = yaccXmlFileName + ".cs", lexCsFileName = lexLexFileName + ".cs";
	var fout = WScript.StdOut;
	fout.WriteLine("all: " + yaccCsFileName + ' ' + lexCsFileName);
	fout.WriteLine();
	fout.WriteLine(yaccCsFileName + ": " + yaccXmlFileName);
	fout.WriteLine('\tIF EXIST ' + yaccCsFileName + ' DEL /F /Q ' + yaccCsFileName);
	fout.WriteLine('\t"..\\TestResults\\Pard.exe" --namespace=Pard --parser-class-name=YaccInput --scanner-class-name=YaccScanner ' + yaccXmlFileName + ' ' + yaccCsFileName);
	fout.WriteLine();
	fout.WriteLine(lexCsFileName + ": " + lexLexFileName);
	fout.WriteLine('\tIF EXIST ' + lexCsFileName + ' DEL /F /Q ' + lexCsFileName);
	fout.WriteLine('\t"..\\TestResults\\Lad.exe" --namespace=Pard --class-name=YaccScanner --scanner-input-type=inline ' + lexLexFileName + ' ' + lexCsFileName);
}
