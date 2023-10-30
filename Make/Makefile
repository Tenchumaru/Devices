!IF "$(Configuration)" == ""
!ERROR No configuration specified
!ENDIF

all: $(MAKEDIR)\$(Configuration)\step5
	@ECHO Build of configuration $(Configuration) is complete

$(MAKEDIR)\$(Configuration)\step5: $(MAKEDIR)\$(Configuration)\step4
	REM Build Pard.Test
	$(MAKEDIR)\Build.cmd $(Configuration) Pard.Test
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step5

$(MAKEDIR)\$(Configuration)\step4: $(MAKEDIR)\$(Configuration)\step3
	REM Build Lad.Test
	$(MAKEDIR)\Build.cmd $(Configuration) Lad.Test
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step4

$(MAKEDIR)\$(Configuration)\step3: $(MAKEDIR)\$(Configuration)\step2
	REM Build full version of Pard
	ECHO >> ..\Pard\YaccInputParser.g.cs
	$(MAKEDIR)\Build.cmd $(Configuration) Pard
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step3

$(MAKEDIR)\$(Configuration)\step2: $(MAKEDIR)\$(Configuration)\step1
	REM Build Lad using XML Pard
	$(MAKEDIR)\Build.cmd $(Configuration) Lad
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step2

$(MAKEDIR)\$(Configuration)\step1: $(MAKEDIR)\$(Configuration)
	REM Build XML Pard using stub YaccInputParser.g.cs and YaccInputScanner.g.cs files
	$(MAKEDIR)\Build.cmd $(Configuration) Pard
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step1

$(MAKEDIR)\$(Configuration):
	MD $(MAKEDIR)\$(Configuration)

clean:
	IF EXIST $(MAKEDIR)\$(Configuration) RD /Q /S $(MAKEDIR)\$(Configuration)
	CD .. && msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform="Any CPU" -t:Clean
	FOR /D %I IN (..\*) DO FOR %J IN (%I\*.g.cs) DO DEL /F /Q %J
