# NavisBOQ MCP 🏗️

> Cuantificación BIM con IA — Autodesk Navisworks 2025 + Claude AI  
> GPC Construcción · Área BIM \& Tecnología · v3.0 · 2026

\---

## ¿Qué hace este plugin?

NavisBOQ conecta **Navisworks** con **Claude AI** para que puedas pedir cantidades de obra con lenguaje natural, sin exportar a Excel ni abrir ningún reporte. Claude lee directamente el modelo BIM y te responde al instante.

**Ejemplos reales de lo que puedes pedirle a Claude:**

```
"Dame las cantidades del conjunto BAÑOS VESTIDORES"
"Corrida arquitectónica del set OFICINAS"
"Compara muros y losas entre COMEDOR y OFICINAS"
"¿Cuántas columnas y vigas hay en GROUND LEVEL?"
"Cuantifica lo que tengo seleccionado ahora en Navisworks"
"Dame la corrida estructural del set ESTRUCTURA DE CONCRETO"
```

\---

## ✅ Qué SÍ hace

|Función|Detalle|
|-|-|
|Cuantificar por Selection Set|Le das el nombre del set y te da el BOQ completo|
|Cuantificar selección manual|Seleccionas elementos en Navisworks y Claude los cuantifica|
|Corrida Arquitectónica (C1)|Muros, Losas, Plafones, Cubiertas, Puertas, Ventanas|
|Corrida Estructural (C2)|Columnas, Vigas, Zapatas + Muros y Losas estructurales|
|Leer Área m²|Desde parámetro Revit `lcldrevit\_parameter\_-1012805`|
|Leer Volumen m³|Desde parámetro Revit `lcldrevit\_parameter\_-1012806`|
|Leer Longitud ml|Muros: `-1004005` · Columnas/Vigas: `-1001375`|
|Material Estructural|Lee el material asignado en Revit por instancia y tipo|
|Espesor de tipo|Muros: ancho · Losas/Plafones/Cubiertas: espesor|
|Agrupar por Nivel → Categoría → Familia → Tipo|Jerarquía completa del modelo|
|Comparar conjuntos|"Compara OFICINAS vs COMEDOR"|
|Exportar a JSON|BOQ completo al Escritorio para procesar en Excel|
|Diagnóstico de propiedades|`dump\_instancia`, `dump\_geo\_set` para depuración|

## ❌ Qué NO hace

|Limitación|Razón|
|-|-|
|Leer Área/Volumen si el NWC fue exportado sin parámetros|Requiere "Convert Element Parameters" activo en Revit al exportar|
|Modificar el modelo Navisworks|Es solo lectura|
|Funcionar sin Navisworks abierto|El plugin vive dentro de Navisworks|
|Funcionar sin un modelo cargado|Necesita un NWD o NWF activo|
|Leer IFC directamente|Solo modelos NWC/NWD/NWF exportados desde Revit|
|MEP cuantificado por especialidad|En versión actual solo arquitectura y estructura|

\---

## Categorías Revit soportadas

### Corrida 1 — Arquitectura

|Categoría Revit|Nombre en BOQ|Métrica|
|-|-|-|
|Walls / Muros|Muros|m² + m³ + ml|
|Floors / Suelos|Losas|m² + m³|
|Ceilings / Techos|Plafones|m²|
|Roofs / Cubiertas|Cubiertas|m² + m³|
|Doors / Puertas|Puertas|pza por nivel y tipo|
|Windows / Ventanas|Ventanas|pza por nivel y tipo|
|Curtain Wall Panels|Fachada|m²|

### Corrida 2 — Estructura

|Categoría Revit|Nombre en BOQ|Métrica|
|-|-|-|
|Structural Columns|Columnas|ml + m³|
|Structural Framing|Vigas|ml + m³|
|Structural Foundations|Cimentación|m³|
|Walls / Muros|Muros (estructurales)|m² + m³ + ml|
|Floors / Suelos|Losas (estructurales)|m² + m³|

> \*\*Nota:\*\* La separación "arquitectónico vs estructural" la define el \*\*Selection Set\*\*, no la categoría. Walls siempre es Walls en Revit — si está en tu set de estructura, se cuantifica como muro estructural.

\---

## Comandos disponibles (para Claude)

Estos son los comandos internos que Claude puede usar. No necesitas conocerlos — solo escribe en lenguaje natural y Claude elige el comando correcto.

|Comando|Cuándo se usa|
|-|-|
|`ping`|Verificar que el plugin está activo|
|`list\_source\_files`|Ver qué NWCs componen el modelo|
|`list\_selection\_sets`|Ver todos los Selection Sets disponibles|
|`extract\_from\_set`|Cuantificar un Selection Set por nombre|
|`extract\_from\_current\_selection`|Cuantificar lo que tienes seleccionado en Navisworks|
|`run\_preconstruccion\_1`|Corrida arquitectónica completa|
|`run\_preconstruccion\_2`|Corrida estructural completa|
|`get\_model\_summary`|Resumen general del modelo|
|`highlight\_elements`|Resaltar elementos por categoría/nivel en 3D|
|`clear\_selection`|Limpiar selección activa|
|`export\_json`|Exportar BOQ a JSON en el Escritorio|
|`dump\_instancia`|Diagnóstico: ver propiedades de una categoría|
|`dump\_geo\_set`|Diagnóstico: ver árbol de un Selection Set|

\---

## Instalación paso a paso

> ⚠️ \*\*No necesitas saber programar.\*\* Solo seguir los pasos en orden. Si algo falla, el mensaje de error te dirá exactamente qué falló.

### Lo que necesitas instalado antes de empezar

Verifica que tienes esto en tu computadora:

* ✅ **Navisworks Manage 2025** — [Descargar desde Autodesk](https://www.autodesk.com)
* ✅ **Claude Desktop** (Plan Pro) — [Descargar aquí](https://claude.ai/download)
* ✅ **Visual Studio 2022** (cualquier edición, incluso Community gratis) — [Descargar aquí](https://visualstudio.microsoft.com/es/downloads/)

  * Durante la instalación marcar: ✅ *Desarrollo de escritorio con .NET*
* ✅ **.NET 8 SDK** — [Descargar aquí](https://dotnet.microsoft.com/download/dotnet/8.0)
* ✅ **Git** — [Descargar aquí](https://git-scm.com/download/win)

\---

### PASO 1 — Abrir PowerShell

PowerShell es la "terminal" de Windows. Para abrirla:

1. Presiona las teclas **Windows + X**
2. Selecciona **"Terminal de Windows"** o **"Windows PowerShell"**
3. Se abre una ventana azul o negra con texto — eso es PowerShell ✅

\---

### PASO 2 — Descargar el proyecto

Escribe esto en PowerShell y presiona Enter:

```powershell
git clone https://github.com/JefaturaBIM94/navis-boq.git C:\\NavisBOQ
```

Verás texto descargándose. Cuando termine y aparezca el símbolo `PS C:\\>` de nuevo, continúa.

> Si ves error "git no reconocido": instala Git del enlace de arriba, cierra PowerShell y ábrelo de nuevo.

\---

### PASO 3 — Compilar el Plugin

El plugin necesita "compilarse" — convertir el código fuente en un archivo que Navisworks pueda usar.

Escribe estos comandos **uno por uno**, presionando Enter después de cada uno:

```powershell
cd C:\\NavisBOQ\\Plugin
```

```powershell
\& "C:\\Program Files\\Microsoft Visual Studio\\2022\\Professional\\MSBuild\\Current\\Bin\\amd64\\MSBuild.exe" NavisBOQ.Plugin.csproj /p:Configuration=Release /p:Platform=x64 /t:Rebuild
```

> Si Visual Studio es la edición \*\*Community\*\*, cambia `Professional` por `Community` en la ruta.

**Resultado esperado al final:**

```
Compilación correcta.
    0 Advertencia(s)
    0 Errores
```

Si ves errores, escríbelos aquí o en el chat con tu equipo BIM.

\---

### PASO 4 — Instalar el Plugin en Navisworks

Primero crear la carpeta destino (solo la primera vez):

```powershell
New-Item -ItemType Directory -Force -Path "C:\\ProgramData\\Autodesk\\Navisworks Manage 2025\\Plugins\\NavisBOQ"
```

Luego copiar el plugin compilado:

```powershell
Copy-Item "C:\\NavisBOQ\\Plugin\\bin\\x64\\Release\\NavisBOQ.dll" "C:\\ProgramData\\Autodesk\\Navisworks Manage 2025\\Plugins\\NavisBOQ\\" -Force
```

Si no aparece ningún error en rojo: ✅ instalado correctamente.

\---

### PASO 5 — Compilar el Servidor MCP

El MCP Server es el "traductor" entre Claude y Navisworks.

```powershell
cd C:\\NavisBOQ\\McpServer
dotnet build -c Release
```

**Resultado esperado:**

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

\---

### PASO 6 — Configurar Claude Desktop

Aquí le dices a Claude dónde está el servidor MCP.

**Abre el archivo de configuración:**

```powershell
notepad "$env:APPDATA\\Claude\\claude\_desktop\_config.json"
```

Se abre el Bloc de notas con el archivo. Reemplaza TODO el contenido con esto:

```json
{
  "mcpServers": {
    "navis-boq": {
      "command": "C:\\\\NavisBOQ\\\\McpServer\\\\bin\\\\Release\\\\net8.0\\\\win-x64\\\\NavisBOQ.McpServer.exe"
    }
  }
}
```

> ⚠️ Los `\\\\` dobles son importantes — no los cambies por `\\` simples.

Guarda el archivo: **Ctrl+S** → cierra el Bloc de notas.

**Cierra Claude Desktop completamente** (clic derecho en el ícono de la barra de tareas → Salir) y ábrelo de nuevo.

\---

### PASO 7 — Verificar que todo funciona

1. Abre **Navisworks Manage 2025**
2. Carga un modelo (archivo `.nwf` o `.nwd`)
3. Abre **Claude Desktop**
4. Escribe en el chat:

```
ping al plugin de navisworks
```

**Respuesta esperada de Claude:**

```
✅ Plugin activo — NavisBOQ v3 · Modelo: TU-MODELO.nwf
```

Si Claude responde con eso: **¡instalación completa!** 🎉

\---

### Problemas comunes

|Síntoma|Solución|
|-|-|
|`git` no reconocido|Instala Git y reinicia PowerShell|
|`dotnet` no reconocido|Instala .NET 8 SDK y reinicia PowerShell|
|MSBuild no encontrado|Verifica la ruta según tu edición de VS (Professional/Community/Enterprise)|
|Claude no muestra `navis-boq`|Verifica que el JSON sea válido (sin comas extra) y reinicia Claude|
|Ping falla|Navisworks debe estar abierto **con un modelo cargado** antes de usar Claude|
|Área y Volumen = 0|El NWC fue exportado sin parámetros. Solución en Revit → ver sección abajo|

\---

### ¿Por qué Área y Volumen pueden ser 0?

Si obtienes cantidades pero Área y Volumen son 0 en todos los elementos, el NWC fue exportado desde Revit sin activar los parámetros de elemento.

**Solución en Revit:**

1. Ve a la pestaña **Add-Ins** en Revit
2. Clic en **Navisworks** → **Export Settings**
3. Activa ✅ **Convert Element Parameters**
4. Re-exporta el NWC
5. Recarga el modelo en Navisworks

\---

## Actualizar el plugin

Cuando haya nuevas versiones:

```powershell
cd C:\\NavisBOQ
git pull
```

Luego repetir los Pasos 3, 4 y 5.

\---

## Uso diario — Flujo de trabajo

### Opción A: por Selection Set

1. Abre Navisworks con tu modelo
2. Abre Claude Desktop
3. Escribe: `"Dame las cantidades del conjunto NOMBRE-DEL-SET"`

### Opción B: selección manual

1. Selecciona elementos en Navisworks (click, ventana, o desde el árbol)
2. En Claude escribe: `"Cuantifica lo que tengo seleccionado"`

### Opción C: corridas predefinidas

* `"Corrida arquitectónica del set OFICINAS"` → Muros, Losas, Plafones, Puertas, Ventanas
* `"Corrida estructural del set ESTRUCTURA"` → Columnas, Vigas, Zapatas

\---

## Estructura del repositorio

```
navis-boq/
├── Plugin/
│   ├── BoqTools.cs              ← Motor de cuantificación BIM (núcleo del plugin)
│   ├── CommandDispatcher.cs     ← Enruta comandos HTTP a BoqTools
│   ├── NavisBOQPlugin.cs        ← Servidor HTTP interno :8765
│   └── NavisBOQ.Plugin.csproj   ← Proyecto .NET 4.8
├── McpServer/
│   ├── Program.cs               ← Servidor MCP + registro de herramientas
│   └── NavisBOQ.McpServer.csproj ← Proyecto .NET 8
├── .gitignore
└── README.md
```

\---

## Contacto

**GPC Construcción — Área BIM \& Tecnología**  
📧 fabian.banuet@gpconstruccion.com.mx  
🔗 https://github.com/JefaturaBIM94/navis-boq

