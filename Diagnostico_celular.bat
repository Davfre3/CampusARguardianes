@echo off
setlocal
cd /d "%~dp0"
set ADB=C:\Users\LENOVO\AppData\Local\Android\Sdk\platform-tools\adb.exe
if not exist "%ADB%" set ADB=C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe
echo Usando %ADB% > Diagnostico_celular.txt
"%ADB%" devices >> Diagnostico_celular.txt
echo ---- PAQUETES ---- >> Diagnostico_celular.txt
"%ADB%" shell pm list packages 2>&1 | findstr /i "campus guardian unity" >> Diagnostico_celular.txt
echo ---- MODELO ---- >> Diagnostico_celular.txt
"%ADB%" shell getprop ro.product.model >> Diagnostico_celular.txt
"%ADB%" shell getprop ro.build.version.release >> Diagnostico_celular.txt
"%ADB%" logcat -d > Diagnostico_log_completo.txt 2>&1
echo Listo. Cierra esta ventana y avisale a Claude.
pause
