# Dragon AR – Unity 6 (6000.6.4f1) + AR Foundation + ARCore

Invocas un dragón (estilo Drogon) sobre el suelo real con la cámara del celular.
Pulsa **DRACARYS** para que escupa fuego. Toca otro punto del suelo para moverlo.

## Cómo abrirlo y generar el APK

1. Unity Hub → **Add > Add project from disk** → elige esta carpeta (`DragonAR`).
   Debe tener instalado el módulo **Android Build Support** (SDK, NDK, OpenJDK).
2. Espera a que Unity descargue los paquetes (AR Foundation, ARCore, Input System).
   - Si pregunta por activar el nuevo Input System / reiniciar: **Yes**.
3. Al primer arranque, un script configura todo solo (escena, Android, IL2CPP, ARM64, API 26, APK) y muestra un mensaje.
   - Si el mensaje dice que no pudo activar ARCore: `Edit > Project Settings > XR Plug-in Management > Android > marcar ARCore`.
4. Menú **Dragon AR > 2) Compilar APK** → se genera `Builds/DragonAR.apk`.
5. Pasa el APK al celular (o conecta por USB con depuración activada y usa `File > Build Profiles > Android > Build And Run`).

Celular: Android con ARCore (Servicios de Google Play para RA). Buena luz y mover el teléfono despacio para detectar el suelo.

## Estructura
- `Assets/Scripts/DragonBootstrap.cs` – crea AR Session, XR Origin y cámara por código.
- `Assets/Scripts/DragonARGame.cs` – tocar para colocar, botones y UI.
- `Assets/Scripts/DragonController.cs` – dragón procedural, vuelo, aleteo y fuego.
- `Assets/Scripts/Fireball.cs` – bolas de fuego.
- `Assets/Editor/DragonARSetup.cs` – configuración automática y menú de compilación.

## Si algo falla
- Cámara negra: verifica ARCore marcado y Graphics API = OpenGLES3 (Player Settings > Android).
- Errores de compilación en consola: copia el mensaje y me lo pasas.
