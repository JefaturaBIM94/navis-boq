# NavisBOQ MCP 🏗️

> Plugin de Cuantificación BIM para **Autodesk Navisworks Manage 2025** + **Claude AI**  
> GPC Construcción — Área BIM & Tecnología — v3.0 — 2026

---

## ¿Qué es?

NavisBOQ MCP permite consultarle a Claude AI cantidades directamente desde un modelo BIM en Navisworks usando lenguaje natural:

```
"Dame las cantidades del conjunto BAÑOS VESTIDORES"
"Corrida arquitectónica de OFICINAS vs COMEDOR"
"Cuantifica lo que tengo seleccionado en Navisworks"
```

## Arquitectura

```
Claude Desktop  ──MCP──►  NavisBOQ.McpServer (.NET 8)  ──HTTP:8765──►  NavisBOQ.Plugin (.NET 4.8)  ──API──►  Navisworks
```

---

## Requisitos

| Componente | Versión |
|---|---|
| Navisworks Manage | 2025 |
| .NET Framework | 4.8 (Plugin) |
| .NET Runtime | 8.0 (MCP Server) |
| Claude Desktop | Plan Pro |
| Visual Studio | 2022 (para compilar) |
| OS | Windows 10/11 64-bit |

---

## Instalación

### 1. Clonar el repositorio

```powershell
git clone https://github.com/JefaturaBIM94/navis-boq.git C:\NavisBOQ
cd C:\NavisBOQ
```

### 2. Compilar el Plugin (.NET 4.8)

```powershell
cd C:\NavisBOQ\Plugin

& "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe" `
    NavisBOQ.Plugin.csproj /p:Configuration=Release /p:Platform=x64 /t:Rebuild
```

Resultado esperado: `Compilación correcta. 0 Advertencia(s) 0 Errores`

### 3. Instalar el Plugin en Navisworks

```powershell
# Crear carpeta si no existe
New-Item -ItemType Directory -Force `
    -Path "C:\ProgramData\Autodesk\Navisworks Manage 2025\Plugins\NavisBOQ"

# Copiar DLL
Copy-Item "C:\NavisBOQ\Plugin\bin\x64\Release\NavisBOQ.dll" `
    "C:\ProgramData\Autodesk\Navisworks Manage 2025\Plugins\NavisBOQ\" -Force
```

### 4. Compilar el MCP Server (.NET 8)

```powershell
cd C:\NavisBOQ\McpServer
dotnet build -c Release
```

El ejecutable queda en:  
`C:\NavisBOQ\McpServer\bin\Release\net8.0\win-x64\NavisBOQ.McpServer.exe`

### 5. Configurar Claude Desktop

Editar `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "navis-boq": {
      "command": "C:\\NavisBOQ\\McpServer\\bin\\Release\\net8.0\\win-x64\\NavisBOQ.McpServer.exe"
    }
  }
}
```

> ⚠️ Cerrar y reabrir Claude Desktop después de guardar.

### 6. Verificar conexión

Con Navisworks abierto y un modelo cargado, escribir en Claude Desktop:

```
ping al plugin de navisworks
```

Respuesta esperada:
```json
{ "pong": true, "version": "NavisBOQ v3", "doc": "MODELO.nwf" }
```

---

## Actualizar

```powershell
cd C:\NavisBOQ
git pull
# Luego repetir pasos 2, 3 y 4
```

---

## Herramientas MCP

| Herramienta | Descripción |
|---|---|
| `ping` | Verificar conexión con el plugin |
| `list_source_files` | Listar NWCs del modelo activo |
| `get_model_summary` | Categorías, niveles y conteos |
| `list_selection_sets` | Selection Sets disponibles |
| `extract_from_set` | Cuantificar un Selection Set |
| `extract_from_current_selection` | Cuantificar selección activa en UI |
| `extract_quantities` | Cuantificar modelo completo con filtros |
| `run_preconstruccion_1` | Corrida arquitectónica (Muros, Losas, etc.) |
| `highlight_elements` | Resaltar elementos por categoría/nivel |
| `clear_selection` | Limpiar selección |
| `export_json` | Exportar BOQ a JSON |
| `dump_instancia` | Diagnóstico de propiedades Revit (Área/Vol) |

---

## Estructura del Repositorio

```
navis-boq/
├── Plugin/
│   ├── BoqTools.cs              # Motor principal de cuantificación BIM
│   ├── CommandDispatcher.cs     # Router HTTP → BoqTools
│   ├── NavisBOQPlugin.cs        # Entry point + HTTP server :8765
│   └── NavisBOQ.Plugin.csproj
├── McpServer/
│   ├── Program.cs               # Registro MCP + proxy HTTP
│   └── NavisBOQ.McpServer.csproj
├── .gitignore
└── README.md
```

---

## Troubleshooting

| Síntoma | Solución |
|---|---|
| Plugin no aparece en Claude | Verificar JSON válido en config y reiniciar Claude Desktop |
| Ping falla | Navisworks debe estar abierto con modelo cargado |
| 0 elementos en extract_from_set | Usar selección manual como alternativa |
| Área/Volumen = 0 | Re-exportar NWC con "Convert Element Parameters" activo en Revit |
| Error CS0111 al compilar | Reemplazar BoqTools.cs con la versión del repositorio |

---

## Contacto

**GPC Construcción — Área BIM & Tecnología**  
fabian.banuet@gpconstruccion.com.mx
