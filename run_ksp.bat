@echo off

set KSP="C:\KSP-moddev"
set DEST="%KSP%\GameData"
set MODFOLDER="%DEST%\FARKOSData"

set SOURCE="E:\Users\mthiffault\KSPProjects\FARKOSData\GameData\FARKOSData"

rmdir %MODFOLDER% /s /q

xcopy %SOURCE% %MODFOLDER% /D /E /C /R /I /K /Y

c:
cd "%KSP%"
KSP.exe

pause