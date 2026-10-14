@echo off
setlocal EnableExtensions
title Compilar APK - Surf Tsunami 2D
cd /d "%~dp0"

rem ============================================================
rem  Compila el juego para Android y deja el APK en .\Builds
rem  ANTES: cierra Unity (si no, no hay RAM ni se libera el proyecto)
rem ============================================================

set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
set "SURF_APK_OUT=%~dp0Builds"
set "SURF_JDK_PATH=%LOCALAPPDATA%\Unity\OpenJDK17"
set "JAVA_HOME=%SURF_JDK_PATH%"

echo ============================================================
echo    Compilar APK de Surf Tsunami 2D  (Android)
echo ============================================================
echo.

if not exist "%UNITY%" (
  echo [X] No se encuentra Unity en:
  echo     %UNITY%
  echo     Edita la variable UNITY de este .bat con tu ruta correcta.
  echo.
  pause
  exit /b 1
)

tasklist /FI "IMAGENAME eq Unity.exe" 2>NUL | find /I "Unity.exe" >NUL
if not errorlevel 1 (
  echo [!] Unity esta ABIERTO.
  echo.
  echo     - Unity bloquea el proyecto y se come la memoria RAM.
  echo     - Guarda tus cambios, CIERRA Unity y vuelve a ejecutar este archivo.
  echo.
  pause
  exit /b 1
)

if not exist "Builds" mkdir "Builds"

echo Compilando... puede tardar entre 10 y 30 minutos.
echo No cierres esta ventana.
echo.
echo (el registro completo se guarda en Builds\build-android.log)
echo.

"%UNITY%" -batchmode -nographics -quit -projectPath "%~dp0." -buildTarget Android -executeMethod BuildAndroid.BuildAPK -logFile "%SURF_APK_OUT%\build-android.log" -acceptSoftwareTermsForThisRunOnly

echo.
if exist "%SURF_APK_OUT%\SurfTsunami2D.apk" (
  echo ============================================================
  echo    LISTO!  APK creado en:
  echo    %SURF_APK_OUT%\SurfTsunami2D.apk
  echo ============================================================
  echo.
  echo Enviatelo al movil ^(WhatsApp / Drive / Telegram^) e instalalo.
  echo Si Android avisa: permite "Instalar apps de fuentes desconocidas".
  echo.
  start "" "%SURF_APK_OUT%"
) else (
  echo [X] No se ha podido crear el APK.
  echo     Mira el registro:  %SURF_APK_OUT%\build-android.log
  echo     Si el error menciona memoria, cierra Chrome/Unity y reintenta.
)
echo.
pause
