PRCURRENTTIME = ..\..\Utils\currenttime.exe

build: nunit-var
    cd OnlyInGac
    $(MAKE) build
    cd ..
    cd LocalAndInGac
    $(MAKE) build
    cd ..

nunit-var:        
    @if not defined NUNITV2_HOME echo Set the NUNITV2_HOME variable to the NUnit V2 home directory. Please check TestInstructions.txt.
    @if not defined NUNITV2_HOME exit 2

test: nunit-var
    @echo Gac integration tests starting at:
    @$(PRCURRENTTIME)    
    cd OnlyInGac
    $(MAKE) test
    cd ..
    cd LocalAndInGac
    $(MAKE) test
    cd ..
   
clean:
    cd OnlyInGac
    $(MAKE) clean
    cd ..
    cd LocalAndInGac
    $(MAKE) clean
    cd ..
