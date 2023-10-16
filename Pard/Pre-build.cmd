@ECHO OFF
SETLOCAL

IF "%~1" == "" EXIT /B 2
CD /D "%~dp0"
SET CONFIGURATION=%~1
SET D=bin\%CONFIGURATION%\net6.0
SET LAD=..\Lad\%D%\Lad.exe
IF NOT EXIST %D%\Pard.exe (
	SET PARD=NOT\Pard.exe
) ELSE (
	SET PARD=%D%\Pard.exe
)
IF NOT EXIST obj MD obj
IF EXIST "%LAD%" IF EXIST "%PARD%" (
	CALL :full
	EXIT /B
)
CALL :stub
EXIT /B

:full
SET I=YaccInputScanner.cs
SET O=YaccInputScanner.g.cs
SET COMMAND="%LAD%" --namespace=Pard --access="public partial" --class=YaccInputScanner -p 2 -# %I% obj\%O%
CALL :doit
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
SET I=YaccInputParser.xml
SET O=YaccInputParser.g.cs
SET COMMAND="%PARD%" -o obj\%O%.txt --namespace=Pard --access="public partial" --class=YaccInput.YaccInputParser --scanner-class=YaccInputScanner %I% obj\%O%
CALL :doit
IF NOT ERRORLEVEL 1 IF "%NEEDS_REBUILD%" == "" EXIT /B
EXIT /B 1

:doit
%COMMAND%
IF ERRORLEVEL 1 EXIT /B
SET S=%O:~9,-5%
fc obj\%O% %O% > NUL 2> NUL
IF NOT ERRORLEVEL 1 (
	ECHO %S% Same
	DEL /F /Q obj\%O%
	EXIT /B
) ELSE IF NOT EXIST %O% (
	ECHO %S% Created
) ELSE (
	ECHO %S% Changed
)
MOVE /Y obj\%O% %O%
SET NEEDS_REBUILD=YES
EXIT /B

:stub
ECHO namespace Pard{ > YaccInputScanner.g.cs
ECHO public partial class YaccInputScanner{class Reader{public void Write^(string s^){} >> YaccInputScanner.g.cs
ECHO public string Consume^(int i^){return "";}}Reader reader_;int LineNumber; >> YaccInputScanner.g.cs
FOR %%I IN (ReadSectionOne ReadSectionTwo ReadCodeBlock) DO (
	ECHO private YaccInput.Token %%I^(^)=^>default; >> YaccInputScanner.g.cs
)
ECHO private bool? CollectComments^(^)=^>default;}} >> YaccInputScanner.g.cs
ECHO namespace Pard{public partial class YaccInput{public partial class YaccInputParser{ > YaccInputParser.g.cs
FOR %%I IN (CodeBlock PCCB POCB PP PDefine PStart PToken, PType PPrec ErrorToken Literal Identifier) DO (
	ECHO internal const int %%I=0; >> YaccInputParser.g.cs
)
ECHO internal YaccInputParser^(YaccInputScanner s^){Environment.Exit^(0^);} >> YaccInputParser.g.cs
ECHO internal bool Parse^(^){return true;}}}} >> YaccInputParser.g.cs
EXIT /B
