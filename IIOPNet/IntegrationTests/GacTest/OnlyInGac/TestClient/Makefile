NUNIT=$(NUNITV2_HOME)\bin\nunit.framework.dll 
IIOPCHANNEL = ..\..\..\..\IIOPChannel\bin\IIOPChannel.dll 
IIOPCHANNELPDB = ..\..\..\..\IIOPChannel\bin\IIOPChannel.pdb 
bin         = bin\ 
binTmp      = binTmp\ 

build: build-client

build-client: $(bin)ClientTest.dll

compile-idl: IDL\Ch\Elca\Iiop\IntegrationTests\TestService.idl


IDL\Ch\Elca\Iiop\IntegrationTests\TestService.idl: ..\TestServer\IDL\Ch\Elca\Iiop\IntegrationTests\TestService.idl
	if not exist IDL\nul mkdir IDL
	cd IDL
	copy ..\..\..\..\..\IDLToCLSCompiler\IDL\*.idl .
	xcopy /s /q /y ..\..\TestServer\IDL .
	cd ..

$(binTmp)Client.dll: IDL\Ch\Elca\Iiop\IntegrationTests\TestService.idl IDL\Ch\Elca\Iiop\IntegrationTests\TestExceptionService.idl IDL\Ch\Elca\Iiop\IntegrationTests\ISimpleTestInterface.idl IDL\Ch\Elca\Iiop\IntegrationTests\TestBoxedValueTypeService.idl
	cd IDL 
	..\..\..\..\..\IDLToCLSCompiler\IDLCompiler\bin\IDLToCLSCompiler.exe -o ..\$(binTmp) -snk ..\..\..\..\..\Key.Snk client Ch\Elca\Iiop\IntegrationTests\TestService.idl Ch\Elca\Iiop\IntegrationTests\TestExceptionService.idl Ch\Elca\Iiop\IntegrationTests\ISimpleTestInterface.idl Ch\Elca\Iiop\IntegrationTests\TestBoxedValueTypeService.idl
	cd ..


$(bin)ClientTest.dll: $(binTmp)Client.dll *.cs
        if not exist $(bin)nul mkdir $(bin)
	csc $(CSFLAGS) /t:library /r:$(NUNIT) /r:$(IIOPCHANNEL) /r:$(binTmp)Client.dll /out:$(bin)ClientTest.dll TestClient.cs ValueTypeImpls.cs


start-client: install-gac
	@echo "Basic GAC IntegrationTest"
	%NUNITV2_HOME%\bin\nunit-console.exe /assembly:bin\ClientTest.dll
	$(MAKE) Makefile -C uninstall-gac

install-gac:
	gacutil /i $(IIOPCHANNEL)
	gacutil /i $(binTmp)Client.dll

uninstall-gac:
	gacutil /u Client
	gacutil /u IiopChannel
	

clean: uninstall-gac
	if exist IDL del /s /q IDL
	if exist bin del /s /q bin
	if exist IDL rmdir /s /q IDL
	if exist bin rmdir /s /q bin
	if exist $(binTmp) rmdir /s /q $(binTmp)

