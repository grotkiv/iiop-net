
build: nunit-var
    cd InvocationScenarios
    $(MAKE) build
    cd ..

nunit-var:        
    @if not defined NUNITV2_HOME echo Set the NUNITV2_HOME variable to the NUnit V2 home directory. Please check TestInstructions.txt.
    @if not defined NUNITV2_HOME exit 2

test: nunit-var
    cd InvocationScenarios
    $(MAKE) test
    cd ..
   
clean:
    cd InvocationScenarios
    $(MAKE) clean
    cd ..
