!IF "$(Configuration)" == ""
!ERROR No configuration specified
!ENDIF

all: $(MAKEDIR)\$(Configuration)\step9
	@echo Build configuration $(Configuration) is complete

$(MAKEDIR)\$(Configuration)\step9: $(MAKEDIR)\$(Configuration)\step8
	REM Build full Lad
	CD ..\Lad && msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU
	CD ..\Lad && IF "$(Configuration)" == "Release" COPY /Y bin\$(Configuration)\* %SystemDrive%\local\bin
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step9

$(MAKEDIR)\$(Configuration)\step8: $(MAKEDIR)\$(Configuration)\step7
	REM Create LexGenerator.y.g.cs for Lad using full Pard
	CD ..\Lad && ..\Pard\bin\$(Configuration)\Pard.exe --namespace=Lad --parser-class-name=LexGenerator --scanner-class-name=Scanner LexGenerator.y LexGenerator.y.g.cs
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step8

$(MAKEDIR)\$(Configuration)\step7: $(MAKEDIR)\$(Configuration)\step6
	REM Create LexGenerator.il.g.cs for Lad using expression Lad
	CD ..\Lad && ..\Lad\bin\$(Configuration)\Lad.exe --scanner-input-type=inline LexGenerator.il.cs LexGenerator.il.g.cs
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step7

$(MAKEDIR)\$(Configuration)\step6: $(MAKEDIR)\$(Configuration)\step5
	REM Build full Pard
	CD ..\Pard && msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU
	CD ..\Pard && IF "$(Configuration)" == "Release" COPY /Y bin\$(Configuration)\* %SystemDrive%\local\bin
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step6

$(MAKEDIR)\$(Configuration)\step5: $(MAKEDIR)\$(Configuration)\step4
	REM Create YaccInput.xml.g.cs for Pard using XML Pard
	CD ..\Pard && ..\Pard\bin\$(Configuration)\Pard.exe --namespace=Pard --parser-class-name=YaccInput --scanner-class-name=YaccInput.Scanner YaccInput.xml YaccInput.xml.g.cs
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step5

$(MAKEDIR)\$(Configuration)\step4: $(MAKEDIR)\$(Configuration)\step3
	REM Create YaccInput.il.g.cs for Pard using expression Lad
	CD ..\Pard && ..\Lad\bin\$(Configuration)\Lad.exe --scanner-input-type=inline YaccInput.il.cs YaccInput.il.g.cs
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step4

$(MAKEDIR)\$(Configuration)\step3: $(MAKEDIR)\$(Configuration)\step2
	REM Build expression Lad using stub LexGenerator.il.g.cs and LexGenerator.y.g.cs files
	CD ..\Lad && COPY /Y LexGenerator.x.cs LexGenerator.il.g.cs
	CD ..\Lad && ECHO using System; > LexGenerator.y.g.cs
	CD ..\Lad && msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step3

$(MAKEDIR)\$(Configuration)\step2: $(MAKEDIR)\$(Configuration)\step1
	REM Create ExpressionParser.xml.g.cs for Lad using XML Pard
	CD ..\Lad && ..\Pard\bin\$(Configuration)\Pard.exe --namespace=Lad --parser-class-name=ExpressionParser --scanner-class-name=ExpressionScanner ExpressionParser.xml ExpressionParser.xml.g.cs
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step2

$(MAKEDIR)\$(Configuration)\step1: $(MAKEDIR)\$(Configuration)
	REM Build XML Pard using stub YaccInput.il.g.cs and YaccInput.xml.g.cs files
	CD ..\Pard && ECHO using System; > YaccInput.il.g.cs
	CD ..\Pard && COPY /Y YaccInput.xml.cs YaccInput.xml.g.cs
	CD ..\Pard && msbuild -noLogo -p:Configuration=$(Configuration) -p:Platform=AnyCPU
	COPY /Y NUL $(MAKEDIR)\$(Configuration)\step1

$(MAKEDIR)\$(Configuration):
	MD $(MAKEDIR)\$(Configuration)

clean:
	IF EXIST $(MAKEDIR)\$(Configuration) RD /Q /S $(MAKEDIR)\$(Configuration)
