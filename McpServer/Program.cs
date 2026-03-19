// =============================================================================
// NavisBOQ MCP Server — .NET 8
//
// Este proceso es el puente entre Claude Desktop y el Plugin de Navisworks.
//
// Flujo completo:
//   Claude Desktop → (MCP stdio) → ESTE PROCESO → (HTTP localhost:8765) → Plugin en NW
//
// Por qué dos procesos:
//   - Claude Desktop requiere .NET 8 para el MCP
//   - Navisworks requiere .NET Framework 4.8 para el Plugin
//   - No pueden ser el mismo proceso → necesitamos el bridge HTTP
//
// Este servidor:
//   1. Habla MCP con Claude Desktop via stdin/stdout (protocolo JSON-RPC 2.0)
//   2. Traduce las llamadas de tools a HTTP requests al plugin en NW
//   3. Devuelve la respuesta a Claude
// =============================================================================

using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace NavisBOQ.McpServer
{
    class Program
    {
        // Puerto donde escucha el Plugin de Navisworks
        private const string PLUGIN_URL = "http://localhost:8765/";

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120) // NWD grandes pueden tardar
        };

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        static async Task Main(string[] args)
        {
            // Forzar UTF-8 sin BOM en stdout — critico para MCP
            Console.OutputEncoding = new System.Text.UTF8Encoding(false);
            Console.InputEncoding  = new System.Text.UTF8Encoding(false);

            // Stderr → Claude Desktop lo captura para logs, no interfiere con stdio
            Console.Error.WriteLine("[NavisBOQ-MCP] Servidor iniciado.");
            Console.Error.WriteLine($"[NavisBOQ-MCP] Conectando a plugin NW en {PLUGIN_URL}");

            // Verificar que el plugin de Navisworks esté activo
            bool pluginActivo = await PingPlugin();
            if (!pluginActivo)
            {
                Console.Error.WriteLine("[NavisBOQ-MCP] ⚠️  Plugin de Navisworks NO responde.");
                Console.Error.WriteLine("[NavisBOQ-MCP]    Asegúrate de que Navisworks 2025 esté abierto.");
            }
            else
            {
                Console.Error.WriteLine("[NavisBOQ-MCP] ✅ Plugin de Navisworks activo y respondiendo.");
            }

            // Loop principal MCP — leer de stdin, responder a stdout
            // IMPORTANTE: usar Console.In/Out directamente — ya tienen UTF8 sin BOM por el encoding que pusimos arriba
            while (true)
            {
                string? line = await Console.In.ReadLineAsync();
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                JsonNode? request;
                try { request = JsonNode.Parse(line); }
                catch { continue; }

                if (request == null) continue;

                string responseJson = await HandleMcpRequest(request);
                if (!string.IsNullOrEmpty(responseJson))
                {
                    var _b = System.Text.Encoding.UTF8.GetBytes(responseJson + "\n");
                    Console.OpenStandardOutput().Write(_b, 0, _b.Length);
                    Console.OpenStandardOutput().Flush();
                }
            }
        }

        // =====================================================================
        // MCP Protocol Handler
        // =====================================================================

        static async Task<string> HandleMcpRequest(JsonNode req)
        {
            var id     = req["id"];
            var method = req["method"]?.GetValue<string>() ?? "";

            try
            {
                object? result = method switch
                {
                    // ── Lifecycle MCP ─────────────────────────────────────────
                    "initialize" => BuildInitializeResult(),
                    "notifications/initialized" => null, // No se responde
                    "ping"       => new { },

                    // ── Tools ─────────────────────────────────────────────────
                    "tools/list" => BuildToolsList(),
                    "tools/call" => await HandleToolCall(req["params"]),

                    _ => throw new Exception($"Método MCP no soportado: {method}")
                };

                // Las notificaciones no tienen respuesta
                if (result == null && method.StartsWith("notifications/"))
                    return "";

                return BuildResponse(id, result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NavisBOQ-MCP] Error en {method}: {ex.Message}");
                return BuildErrorResponse(id, -32000, ex.Message);
            }
        }

        // =====================================================================
        // Manejar llamadas a tools — forwarding al plugin de Navisworks
        // =====================================================================

        static async Task<object> HandleToolCall(JsonNode? paramsNode)
        {
            if (paramsNode == null)
                throw new Exception("Params requeridos para tools/call");

            var toolName = paramsNode["name"]?.GetValue<string>()
                           ?? throw new Exception("'name' requerido en tool call");

            var arguments = paramsNode["arguments"] as JsonObject ?? new JsonObject();

            // Construir el comando para el plugin de Navisworks
            // IMPORTANTE: clonar arguments con JsonNode.Parse para evitar "node already has a parent"
            var command = new JsonObject
            {
                ["tool"]   = toolName,
                ["params"] = JsonNode.Parse(arguments.ToJsonString())
            };

            var commandJson = command.ToJsonString();

            // Enviar al plugin via HTTP
            string pluginResponse;
            try
            {
                var content  = new StringContent(commandJson, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(PLUGIN_URL, content);
                pluginResponse = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    $"No se puede conectar al plugin de Navisworks. " +
                    $"¿Está Navisworks 2025 abierto con el plugin cargado? " +
                    $"Error: {ex.Message}");
            }

            // El plugin responde { "ok": true, "data": {...} } o { "ok": false, "error": "..." }
            var parsed = JsonNode.Parse(pluginResponse);
            var ok     = parsed?["ok"]?.GetValue<bool>() ?? false;

            if (!ok)
            {
                var errMsg = parsed?["error"]?.GetValue<string>() ?? "Error desconocido en el plugin";
                throw new Exception(errMsg);
            }

            // Extraer data y devolverla como texto para Claude
            var data     = parsed?["data"];
            var dataText = data?.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
                           ?? "{}";

            // MCP requiere que el resultado de tools/call sea content[].text
            return new
            {
                content = new[]
                {
                    new { type = "text", text = dataText }
                }
            };
        }

        // =====================================================================
        // Lista de tools que exponemos a Claude
        // =====================================================================

        static object BuildToolsList() => new
        {
            tools = new object[]
            {
                MakeTool("ping",                    "Verifica conexion plugin NW.",                 new { }, new string[0]),
                MakeTool("list_source_files",       "Lista archivos NWC del NWD.",                  new { }, new string[0]),
                MakeTool("get_model_summary",       "Resumen modelo: categorias, niveles.",         new { }, new string[0]),
                MakeTool("dump_properties", "Diagnostico: vuelca propiedades reales.",
                    new { max_items = new { type = "integer", description = "Items (def 3)." } }, new string[0]),
                MakeTool("dump_geo_set", "Diagnostico desde Selection Set.",
                    new { set_name  = new { type = "string",  description = "Nombre del Set." },
                          max_items = new { type = "integer", description = "Items (def 3)." } }, new[] { "set_name" }),
                MakeTool("dump_instancia", "Diagnostico: vuelca valores exactos de Area/Volume/Length de instancias Revit.",
                    new { categoria = new { type="string", description="Categoria Revit (def Walls)." }, max_items = new { type="integer", description="Items (def 3)." } }, new string[0]),                MakeTool("get_parameters",          "Propiedades de una categoria.",
                    new { category = new { type = "string", description = "Categoria Revit." } }, new[] { "category" }),
                MakeTool("extract_quantities",      "BOQ por categoria/nivel.",
                    new { categories    = new { type = "array",   items = new { type = "string" }, description = "Categorias. Vacio=todas." },
                          level         = new { type = "string",  description = "Filtrar nivel. Opcional." },
                          summary_only  = new { type = "boolean", description = "true=resumen." } }, new string[0]),
                MakeTool("run_preconstruccion_1",   "BOQ arq: Muros,Losas,Cubiertas,Plafones,Puertas,Ventanas,Fachada.", new { }, new string[0]),
                MakeTool("highlight_elements",      "Resalta elementos por categoria.",
                    new { categories = new { type = "array",  items = new { type = "string" }, description = "Categorias." },
                          level      = new { type = "string", description = "Nivel. Opcional." } }, new string[0]),
                MakeTool("clear_selection",         "Limpia seleccion NW.",                         new { }, new string[0]),
                MakeTool("list_selection_sets",     "Lista Selection Sets del NWD.",                 new { }, new string[0]),
                MakeTool("extract_from_set",        "Cuantifica un Selection Set.",
                    new { set_name = new { type = "string", description = "Nombre del Selection Set." } }, new[] { "set_name" }),
                MakeTool("extract_from_current_selection", "Cuantifica seleccion actual.",           new { }, new string[0]),
                MakeTool("export_json",             "Exporta BOQ a JSON en Escritorio.",
                    new { output_path = new { type = "string", description = "Ruta salida. Opcional." } }, new string[0]),
            }
        };

        // =====================================================================
        // Helpers
        // =====================================================================

        static object MakeTool(string name, string description,
                                object inputProperties, string[] required) => new
        {
            name,
            description,
            inputSchema = new
            {
                type       = "object",
                properties = inputProperties,
                required
            }
        };

        static object BuildInitializeResult() => new
        {
            protocolVersion = "2024-11-05",
            capabilities    = new { },
            serverInfo = new
            {
                name    = "NavisBOQ-MCP",
                version = "1.0.0"
            }
        };

        static string BuildResponse(JsonNode? id, object? result) =>
            JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id,
                result
            }, _jsonOpts);

        static string BuildErrorResponse(JsonNode? id, int code, string message) =>
            JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id,
                error   = new { code, message }
            }, _jsonOpts);

        static async Task<bool> PingPlugin()
        {
            try
            {
                var body     = new StringContent("{\"tool\":\"ping\",\"params\":{}}",
                                                 Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(PLUGIN_URL, body);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
