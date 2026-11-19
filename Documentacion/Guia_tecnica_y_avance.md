# Campus AR Guardians — Guía técnica y registro de avance (David)

Fecha: 2026-10-29 · Versión de la app: 0.1.0 (borrador)

## 1. Versiones registradas
- Unity 6000.5.9f1 (6.5), URP 17.5.0, Input System 1.20.0, uGUI 2.5.0
- Vuforia Engine 11.4.4 (paquete local tgz). La licencia está en `Resources/VuforiaConfiguration.asset`; no publicarla en capturas ni repositorios.
- Android: IL2CPP, ARM64, minSdk 26, vertical. Id de paquete PROVISIONAL: `com.campusar.guardians`.
- Respaldo previo a los cambios: carpeta `_Respaldo_2026-10-29` (Assets, Packages, ProjectSettings). El proyecto no está en git.

## 2. Cómo abrir y regenerar
1. Abrir el proyecto con Unity 6000.5.9f1.
2. Menú **Campus AR > Construir proyecto (Inicio + AR + datos)**. Es repetible: regenera escenas, fichas, prefabs y ajustes.
3. Escenas: `Assets/CampusAR/Escenas/Inicio.unity` (índice 0) y `ExploracionAR.unity` (índice 1).
4. Build Android: File > Build Profiles > Android > Build. Instalar en un teléfono físico (hito H1).

## 3. Estructura (Assets/CampusAR)
- `Scripts/`: StationDefinition (+StationCatalog), ProgressService, AppFlowController, InformationPanelController, QuizController, ProgressPanelController, ARStationController, SafeArea.
- `Datos/Estacion_*.asset`: una ficha por estación. Se editan en el Inspector; el menú de construcción sobrescribe los campos generados, así que editar los datos definitivos en el script `Editor/CampusARConstructor.cs` o desactivar la regeneración de datos al aprobar el contenido.
- `Resources/CatalogoEstaciones.asset`: lista ordenada de estaciones.
- `Marcadores/`: ADMIN_LOBO, ING_VIKINGO, ECO_TORO, CIE_LEON (1484x1060, relación 1,4:1, Read/Write activado). Ancho registrado: 0,14 m.
- `Prefabs/Mascota_*.prefab`: FBX normalizado a 0,20 m de alto con apoyo en y=0. Si falta el modelo se usa una figura temporal.

## 4. Cómo funciona el reconocimiento
Los 4 Image Targets se crean en tiempo de ejecución desde las imágenes del catálogo (`ObserverFactory.CreateImageTarget`), sin base de datos del Target Manager. Solo se muestra la mascota de la estación activa. Si se pierde el seguimiento se oculta la mascota, pero la tarjeta y los paneles abiertos se conservan. Si hay una pregunta o una lectura en curso, otro marcador no sustituye el contenido (RF15).

## 5. Progreso
PlayerPrefs: `campusar.formato` y `campusar.sello.<id>`. El sello solo lo concede QuizController. El reinicio pide confirmación y borra únicamente esas claves.

## 6. Estado por requisito (SIN probar en teléfono)
Implementado en código, pendiente de verificar en el Editor y en Android: RF01–RF16.
NINGUNA prueba de la matriz P01–P16 se ha ejecutado todavía. No registrar como superadas hasta correrlas.

## 7. Pendientes y dependencias
- H1: APK instalado y video de detección en teléfono físico con la placa impresa a 14x10 cm.
- Revisar en el Target Manager o en pruebas reales la calidad de cada imagen (aspirar a 4–5 estrellas).
- José: validar nombres oficiales. DISCREPANCIA: el informe llama "Ciencias" a la estación del León, pero las carreras del zip para esa estación son Derecho, Derecho Corporativo y Psicología (grupo "Derecho y Ciencias Sociales").
- Stiven: preguntas definitivas (las actuales son PROVISIONALES, derivadas de la lista de carreras).
- Contenido sin aprobar, marcado [PENDIENTE]: descripción de facultad, mapa/plano, orientación, contacto, horarios, webUrl y mapUrl (QR).
- Renzo: modelos FBX finales (los actuales son los recibidos; revisar escala, orientación y materiales en el teléfono).
- Página web del QR: responsable por definir.
- Id de paquete definitivo, icono y firma del APK (controlados por el equipo).
