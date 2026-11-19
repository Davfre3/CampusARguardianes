# Campus AR Guardians

App de realidad aumentada (Android) para ESAN: cuatro placas impresas de 14 × 10 cm
(Lobo/Administración, Toro/Economía, Vikingo/Ingeniería, León/Derecho y Ciencias Sociales)
que muestran una mascota 3D, un menú por facultad, información, mini reto y sellos.

- Unity 6 (6000.5.9f1), URP, Vuforia Engine 11.4.4
- Reconocimiento por imagen (placas) y, de forma experimental, por modelo 3D (Model Target del lobo)

## Cómo abrir el proyecto

1. Instala Unity 6000.5.9f1 con el módulo **Android Build Support**.
2. Descarga **Vuforia Engine 11.4.4** (`com.ptc.vuforia.engine-11.4.4.tgz`) desde
   <https://developer.vuforia.com> y colócalo en `Packages/` (el archivo pesa 138 MB y no está en el repositorio).
3. Abre la carpeta del proyecto con Unity Hub.
4. Pon **tu propia License Key** de Vuforia en `Assets/Resources/VuforiaConfiguration`
   (en este repositorio se quitó la clave).
5. Menú **Campus AR → Construir proyecto (Inicio + AR + datos)** para reconstruir las escenas.
6. Android: API mínima 29 (la exige Vuforia). **Campus AR → Construir APK (con informe)** genera `Builds/CampusAR.apk`.

## Estructura

- `Assets/CampusAR/Scripts`: controladores de flujo, estaciones AR, UI y efectos de la mascota.
- `Assets/CampusAR/Editor`: constructor de escenas y herramienta de build.
- `Assets/StreamingAssets/Vuforia`: bases de datos de Vuforia (placas y Model Targets).
- `Assets/Modelos`: modelos 3D de las mascotas.
