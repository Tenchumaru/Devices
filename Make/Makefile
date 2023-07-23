!IF "$(Configuration)" == ""
!ERROR No configuration specified
!ENDIF

all: $(MAKEDIR)\$(Configuration)\step3
	@echo Build of configuration $(Configuration) is complete

$(MAKEDIR)\$(Configuration)\step3: $(MAKEDIR)\$(Configuration)\step2
	REM Build full version of Pard
	ECHO >> ..\Pard\YaccInputParser.g.cs
	CD ..\Pard && msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU > NUL || msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step3

$(MAKEDIR)\$(Configuration)\step2: $(MAKEDIR)\$(Configuration)\step1
	REM Build Lad using XML Pard
	CD ..\Lad && msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU > NUL || msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step2

$(MAKEDIR)\$(Configuration)\step1: $(MAKEDIR)\$(Configuration)
	REM Build XML Pard using stub YaccInputParser.g.cs and YaccInputScanner.g.cs files
	msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform="Any CPU" -t:Clean
	CD ..\Pard && msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU > NUL || msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step1

$(MAKEDIR)\$(Configuration):
	MD $(MAKEDIR)\$(Configuration)

clean:
	IF EXIST $(MAKEDIR)\$(Configuration) RD /Q /S $(MAKEDIR)\$(Configuration)
