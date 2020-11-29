!IF "$(Configuration)" == ""
!ERROR No configuration specified
!ENDIF

all: $(MAKEDIR)\$(Configuration)\step9
	@echo Build configuration $(Configuration) is complete

$(MAKEDIR)\$(Configuration)\step9: $(MAKEDIR)\$(Configuration)\step8
	REM Build full Lad
	CD ..\\Lad && msbuild -noLogo -verbosity:quiet
	CD ..\\Lad && COPY /Y bin\\$(Configuration)\\* $(MAKEDIR)\\$(Configuration)
	CD ..\\Lad && IF "$(Configuration)" == "Release" COPY /Y bin\\$(Configuration)\\* %SystemDrive%\\local\\bin
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step9

$(MAKEDIR)\$(Configuration)\step8: $(MAKEDIR)\$(Configuration)\step7
	REM Create LexGenerator.y.cs for Lad using full Pard
	CD ..\\Lad && $(MAKEDIR)\\$(Configuration)\\Pard.exe --namespace=Lad --parser-class-name=LexGenerator --scanner-class-name=Scanner LexGenerator.y LexGenerator.y.cs
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step8

$(MAKEDIR)\$(Configuration)\step7: $(MAKEDIR)\$(Configuration)\step6
	REM Create LexGenerator.il.cs for Lad using expression Lad
	CD ..\\Lad && $(MAKEDIR)\\$(Configuration)\\Lad.expression.exe --scanner-input-type=inline LexGenerator.il LexGenerator.il.cs
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step7

$(MAKEDIR)\$(Configuration)\step6: $(MAKEDIR)\$(Configuration)\step5
	REM Build full Pard
	CD ..\\Pard && msbuild -noLogo -verbosity:quiet
	CD ..\\Pard && COPY /Y bin\\$(Configuration)\\* $(MAKEDIR)\\$(Configuration)
	CD ..\\Pard && IF "$(Configuration)" == "Release" COPY /Y bin\\$(Configuration)\\* %SystemDrive%\\local\\bin
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step6

$(MAKEDIR)\$(Configuration)\step5: $(MAKEDIR)\$(Configuration)\step4
	REM Create YaccInput.xml.cs for Pard using XML Pard
	CD ..\\Pard && $(MAKEDIR)\\$(Configuration)\\Pard.xml.exe --namespace=Pard --parser-class-name=YaccInput --scanner-class-name=YaccInput.Scanner YaccInput.xml YaccInput.xml.cs
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step5

$(MAKEDIR)\$(Configuration)\step4: $(MAKEDIR)\$(Configuration)\step3
	REM Create YaccInput.il.cs for Pard using expression Lad
	CD ..\\Pard && $(MAKEDIR)\\$(Configuration)\\Lad.expression.exe --scanner-input-type=inline YaccInput.il YaccInput.il.cs
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step4

$(MAKEDIR)\$(Configuration)\step3: $(MAKEDIR)\$(Configuration)\step2
	REM Build expression Lad using stub LexGenerator.il.cs and LexGenerator.y.cs files
	CD ..\\Lad && COPY /Y LexGenerator.txt LexGenerator.il.cs
	CD ..\\Lad && ECHO using System; > LexGenerator.y.cs
	CD ..\\Lad && msbuild -noLogo -verbosity:quiet
	CD ..\\Lad && FOR %I IN (exe.config exe pdb) DO MOVE /Y bin\\$(Configuration)\\Lad.%I $(MAKEDIR)\\$(Configuration)\\Lad.expression.%I
	CD ..\\Lad && MOVE /Y bin\\$(Configuration)\\* $(MAKEDIR)\\$(Configuration)
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step3

$(MAKEDIR)\$(Configuration)\step2: $(MAKEDIR)\$(Configuration)\step1
	REM Create ExpressionParser.xml.cs for Lad using XML Pard
	CD ..\\Lad && $(MAKEDIR)\\$(Configuration)\\Pard.xml.exe --namespace=Lad --parser-class-name=ExpressionParser --scanner-class-name=ExpressionScanner ExpressionParser.xml ExpressionParser.xml.cs
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step2

$(MAKEDIR)\$(Configuration)\step1: $(MAKEDIR)\$(Configuration)
	REM Build XML Pard using stub YaccInput.il.cs and YaccInput.xml.cs files
	CD ..\\Pard && ECHO using System; > YaccInput.il.cs
	CD ..\\Pard && COPY /Y YaccInput.txt YaccInput.xml.cs
	CD ..\\Pard && msbuild -noLogo -verbosity:quiet
	CD ..\\Pard && FOR %I IN (exe.config exe pdb) DO MOVE /Y bin\\$(Configuration)\\Pard.%I $(MAKEDIR)\\$(Configuration)\\Pard.xml.%I
	CD ..\\Pard && MOVE /Y bin\\$(Configuration)\\* $(MAKEDIR)\\$(Configuration)
	COPY /Y NUL $(MAKEDIR)\\$(Configuration)\\step1

$(MAKEDIR)\$(Configuration):
	MD $(MAKEDIR)\\$(Configuration)

clean:
	RD /Q /S $(MAKEDIR)\\$(Configuration)
