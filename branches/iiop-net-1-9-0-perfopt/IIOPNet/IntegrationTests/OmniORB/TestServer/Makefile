!include .\MakeVars
RELATIVE=$(OMNIORB_HOME)
DELAY=..\..\..\Utils\delay.exe
LAUNCH=..\..\..\Utils\launch.exe
KILL=..\..\..\Utils\kill.exe


build: omniorb_home_var server.exe

omniorb_home_var:
    @IF NOT DEFINED OMNIORB_HOME echo define OMNIORB_HOME first
    @IF NOT DEFINED OMNIORB_HOME exit 2
    @IF NOT DEFINED OMNIORB_VERSION echo define OMNIORB_VERSION first
    @IF NOT DEFINED OMNIORB_VERSION exit 2

    
server.exe: unknownByClient.hh unknownByClientSK.obj unknownByClientDynSK.obj service.hh serviceSK.obj server.obj serviceDynSK.obj internalif.hh internalifSK.obj internalifDynSK.obj
	$(LD) $(LDFLAGS) /OUT:$@ unknownByClientSK.obj unknownByClientDynSK.obj serviceSK.obj serviceDynSK.obj server.obj internalifSK.obj internalifDynSK.obj $(LDLIBS)

server.cc : service.hh unknownByClient.hh internalif.hh


service.hh serviceSK.cc serviceDynSK.cc : service.idl $(IDLGEN)
	$(IDL) -Wba service.idl

unknownByClient.hh unknownByClientSK.cc unknownByClientDynSK.cc : unknownByClient.idl $(IDLGEN)
	$(IDL) -Wba unknownByClient.idl

internalif.hh internalifSK.cc internalifDynSK.cc : unknownByClient.idl $(IDLGEN)
	$(IDL) -Wba internalif.idl

start-server: omniorb_home_var
        IF EXIST omninames* del /q omninames*
	$(LAUNCH) $(OMNIORB_HOME)\bin\x86_win32\omniNames.exe -start 11356 -logdir . >> pid
	$(DELAY) 5
	$(LAUNCH) server.exe -ORBInitRef NameService=corbaloc::localhost:11356/NameService >> pid

stop-server:
	@for /F %%p in (pid) do @$(KILL) %%p
	@del pid

clean:
	IF EXIST serviceSK.cc del /q serviceSK.cc
        IF EXIST service.hh del /q service.hh 
        IF EXIST internalif.hh del /q internalif.hh
        IF EXIST unknownByClient.hh del /q unknownByClient.hh 
        IF EXIST *.obj del /q *.obj 
        IF EXIST server.exe del /q server.exe 
        IF EXIST *~ del /q *~ 
	IF EXIST *.pdb del /q *.pdb
