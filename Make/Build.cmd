@ECHO OFF
SETLOCAL

CD /D %~dp0..\%~2
SET COMMAND=msbuild -noLogo -p:Configuration=%1 -p:Platform=AnyCPU
SET OUTPUT=%~dp0%~1\output.txt
%COMMAND% > "%OUTPUT%"
IF ERRORLEVEL 1 (
	DEL "%OUTPUT%"
	%COMMAND%
) ELSE (
	TYPE "%OUTPUT%"
	DEL "%OUTPUT%"
)
