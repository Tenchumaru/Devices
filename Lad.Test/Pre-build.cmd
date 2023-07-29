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
SET MAKEFILE=obj\Lad.Test.make
IF NOT EXIST obj MD obj
DIR /B *.l | cscript //nologo //E:JScript "%~nx0" %1 > %MAKEFILE%
nmake -nologo "ConfigurationName=%~1" -f %MAKEFILE% all
EXIT /B
*/
function REM() {
	var fin = WScript.StdIn, fout = WScript.StdOut;
	var fileNames = [];
	while(!fin.AtEndOfStream) {
		var s = fin.ReadLine();
		fileNames.push(s);
	}
	fout.WriteLine("ConfigurationName=" + WScript.Arguments(0));
	fout.Write("all:");
	for(var i = 0, n = fileNames.length; i < n; ++i) {
		var fileName = fileNames[i];
		var csFileName = fileName + ".g.cs";
		fout.Write(" " + csFileName);
	}
	fout.WriteLine();
	var exePath = '"..\\Lad\\bin\\$(ConfigurationName)\\net6.0\\Lad.exe"';
	for(var i = 0, n = fileNames.length; i < n; ++i) {
		var fileName = fileNames[i];
		var parts = fileName.split(".");
		var scannerName = parts[0] + parts[1].substr(0, 1);
		var csFileName = fileName + ".g.cs";
		fout.WriteLine();
		fout.WriteLine(csFileName + ": " + fileName);
		fout.WriteLine("\tIF EXIST " + csFileName + " DEL /F /Q " + csFileName);
		var classDeclaration = '"namespace Lad.Test{public partial class ' + scannerName + '"'
		var commandLine = [
			exePath,
			"-p 4",
			"--class-declaration=" + classDeclaration,
			fileName,
			csFileName,
		];
		fout.WriteLine("\t" + commandLine.join(" "));
	}
}
