REM(); /*
@ECHO OFF
SETLOCAL

IF "%~1" == "" (
	ECHO usage: %~nx0 ConfigurationName
	EXIT /B 2
)
PATH %PATH%;C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\bin
CD /D "%~dp0"
SET OUTPUT=ExpressionParser.xml.cs
SET PARD="..\TestResults\Pard.exe"
SET T=%TEMP%\Lad.%RANDOM%.make
IF EXIST %OUTPUT% (
	find "goto" %OUTPUT% > nul
	IF ERRORLEVEL 1 DEL /F /Q %OUTPUT%
)
REM I need Pard to create the Expression and Lex parsers.
IF EXIST %PARD% (
	cscript //nologo //E:JScript %0 > %T%
	nmake -nologo "ConfigurationName=%~1" -f %T%
	IF ERRORLEVEL 1 (
		DEL /F /Q %T%
		EXIT /B 1
	) ELSE (
		REM I need Lad to create the scanner for the Lex parser.
		IF EXIST "..\TestResults\Lad.exe" (
			nmake -nologo "ConfigurationName=%~1" -f %T% LexGenerator.il.cs
			IF ERRORLEVEL 1 (
				DEL /F /Q %T%
				EXIT /B 1
			)
		)
		DEL /F /Q %T%
		EXIT /B 0
	)
)
ECHO Non-functional Expression parser
COPY /Y ExpressionParser.txt %OUTPUT%
EXIT /B 0
*/
function REM() {
	var yaccFileName = "LexGenerator.y", lexFileName = "LexGenerator.il";
	var yaccCsFileName = yaccFileName + ".cs", lexCsFileName = lexFileName + ".cs";
	var xmlFileName = "ExpressionParser.xml", csFileName = xmlFileName + ".cs";
	var fout = WScript.StdOut;
	fout.WriteLine("parsers: " + yaccCsFileName + ' ' + csFileName);
	fout.WriteLine();
	fout.WriteLine(lexCsFileName + ": " + lexFileName);
	fout.WriteLine('\tIF EXIST ' + lexCsFileName + ' DEL /F /Q ' + lexCsFileName);
	fout.WriteLine('\t"..\\TestResults\\Lad.exe" --scanner-input-type=inline ' + lexFileName + ' ' + lexCsFileName);
	fout.WriteLine();
	fout.WriteLine(yaccCsFileName + ": " + yaccFileName);
	fout.WriteLine('\tIF EXIST ' + yaccCsFileName + ' DEL /F /Q ' + yaccCsFileName);
	fout.WriteLine('\t"..\\TestResults\\Pard.exe" --namespace=Lad --parser-class-name=LexGenerator --scanner-class-name=Scanner ' + yaccFileName + ' ' + yaccCsFileName);
	fout.WriteLine();
	fout.WriteLine(csFileName + ": " + xmlFileName);
	fout.WriteLine('\tIF EXIST ' + csFileName + ' DEL /F /Q ' + csFileName);
	fout.WriteLine('\t"..\\TestResults\\Pard.exe" --namespace=Lad --parser-class-name=ExpressionParser --scanner-class-name=ExpressionScanner ' + xmlFileName + ' ' + csFileName);
}
