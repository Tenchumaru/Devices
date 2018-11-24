REM(); /*
@ECHO OFF
SETLOCAL

FOR /F "delims=" %%I IN ('DIR /B /S "C:\Program Files (x86)\Microsoft Visual Studio\nmake.exe"') DO PATH %PATH%;%%~dpI
CD /D "%~dp0"
SET T=%TEMP%\Pard.Test.%RANDOM%.make
find ".xml" "%~dp0Pard.Test.csproj" | find "Content" | cscript //nologo //E:JScript "%~nx0" %1 > %T%
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
	fout.WriteLine("ConfigurationName=" + WScript.Arguments(0));
	fout.Write("all:");
	for(var i = 0, n = xmlFileNames.length; i < n; ++i) {
		var xmlFileName = xmlFileNames[i];
		var csFileName = xmlFileName + ".cs";
		fout.Write(' ' + csFileName);
	}
	fout.WriteLine();
	for(var i = 0, n = xmlFileNames.length; i < n; ++i) {
		var xmlFileName = xmlFileNames[i];
		var parserName = xmlFileName.split('.')[0];
		var csFileName = xmlFileName + ".cs";
		fout.WriteLine();
		fout.WriteLine(csFileName + ": " + xmlFileName);
		fout.WriteLine('\tIF EXIST ' + csFileName + ' DEL /F /Q ' + csFileName);
		fout.WriteLine('\t"..\\Pard\\bin\\$(ConfigurationName)\\Pard.exe" --namespace=Pard.Test --parser-class-name=' +
			parserName + ' --scanner-class-name=Scanner ' + xmlFileName + ' ' + csFileName);
	}
}
