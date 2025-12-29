@echo off
echo Cerco il compilatore C#...

:: Cerca l'ultima versione del framework .NET disponibile
for /r "C:\Windows\Microsoft.NET\Framework64" %%# in (*) do if "%%~nx#"=="csc.exe" set "csc=%%#"

if not defined csc (
    echo Compilatore non trovato! Assicurati di avere Windows aggiornato.
    pause
    exit
)

echo Trovato: %csc%
echo Compilazione in corso...

"%csc%" /out:Launcher.exe Launcher.cs

if %errorlevel% equ 0 (
    echo.
    echo COMPILAZIONE RIUSCITA!
    echo Ora puoi avviare Launcher.exe
) else (
    echo.
    echo ERRORE DURANTE LA COMPILAZIONE.
)
pause