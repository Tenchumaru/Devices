REM(); /*
@ECHO OFF
SETLOCAL

FOR /F "delims=" %%I IN ('DIR /B /S "%SystemDrive%\Program Files\Microsoft Visual Studio\vcvars64.bat"') DO SET VCVARS=%%~I
IF "%VCVARS%" == "" (
	ECHO Cannot find vcvars64.bat
	EXIT /B 1
)
CALL "%VCVARS%"
CD /D "%~dp0"
SET T=%TEMP%\Lad.Test.%RANDOM%.make
find ".xml" "%~dp0Lad.Test.csproj" | find "Content" | cscript //nologo //E:JScript "%~nx0" %1 > %T%
nmake -nologo "ConfigurationName=%~1" -f %T% all
SET EXIT_CODE=%ERRORLEVEL%
DEL /F /Q %T%
EXIT /B %EXIT_CODE%
*/
function REM() {
	var fin = WScript.StdIn, fout = WScript.StdOut;
	var xmlFileNames = [];
	while(!fin.AtEndOfStream) {
		var s = fin.ReadLine();
		xmlFileNames.push(s.split('"')[1]);
	}
	fout.WriteLine('ConfigurationName=' + WScript.Arguments(0));
	fout.Write('all:');
	for(var i = 0, n = xmlFileNames.length; i < n; ++i) {
		var xmlFileName = xmlFileNames[i];
		var csFileName = xmlFileName + '.g.cs';
		fout.Write(' ' + csFileName);
	}
	fout.WriteLine();
	for(var i = 0, n = xmlFileNames.length; i < n; ++i) {
		var xmlFileName = xmlFileNames[i];
		var parserName = xmlFileName.split('.')[0];
		var csFileName = xmlFileName + '.g.cs';
		fout.WriteLine();
		fout.WriteLine(csFileName + ': ' + xmlFileName);
		fout.WriteLine('\tIF EXIST ' + csFileName + ' DEL /F /Q ' + csFileName);
		fout.WriteLine('\t"..\\Lad\\bin\\$(ConfigurationName)\\Lad.exe" --namespace=Lad.Test --class-name=' +
			parserName + xmlFileName + ' ' + csFileName);
	}
}
