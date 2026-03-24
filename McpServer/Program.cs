using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace NavisBOQ.McpServer
{
    internal class Program
    {
        private const string PLUGIN_URL = "http://localhost:8765/";
        private static StreamWriter _stdout = null!;

        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(130)
        };

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        static async Task Main(string[] args)
        {
            var rawOut = Console.OpenStandardOutput();
            _stdout = new StreamWriter(rawOut, new UTF8Encoding(false)) { AutoFlush = true };

            Console.Error.WriteLine("[NavisBOQ-MCP] Servidor iniciado.");
            Console.Error.WriteLine($"[NavisBOQ-MCP] Plugin URL: {PLUGIN_URL}");

            bool pluginActivo = await PingPlugin();
            if (!pluginActivo)
            {
                Console.Error.WriteLine("[NavisBOQ-MCP] Plugin no responde todavía.");
                Console.Error.WriteLine("[NavisBOQ-MCP] Abre Navisworks con un modelo cargado.");
            }
            else
            {
                Console.Error.WriteLine("[NavisBOQ-MCP] Plugin activo.");
            }

            var rawIn = Console.OpenStandardInput();
            var reader = new StreamReader(rawIn, new UTF8Encoding(false));

            while (true)
            {
                string? line;
                try { line = await reader.ReadLineAsync(); }
                catch { break; }

                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                JsonNode? request;
                try
                {
                    request = JsonNode.Parse(line);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[NavisBOQ-MCP] JSON inválido: {ex.Message}");
                    continue;
                }

                if (request == null) continue;

                string responseJson = await HandleMcpRequest(request);
                if (!string.IsNullOrEmpty(responseJson))
                {
                    _stdout.WriteLine(responseJson);
                    _stdout.Flush();
                }
            }
        }

        static async Task<string> HandleMcpRequest(JsonNode req)
        {
            var id = req["id"];
            var method = req["method"]?.GetValue<string>() ?? "";

            if (method.StartsWith("notifications/", StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"[NavisBOQ-MCP] Notificación ignorada: {method}");
                return "";
            }

            try
            {
                object? result = method switch
                {
                    "initialize" => BuildInitializeResult(),
                    "ping" => new { },
                    "tools/list" => BuildToolsList(),
                    "tools/call" => await HandleToolCall(req["params"]),
                    _ => throw new Exception($"Método MCP no soportado: {method}")
                };

                return BuildResponse(id, result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[NavisBOQ-MCP] Error en {method}: {ex}");
                if (id != null)
                    return BuildErrorResponse(id, -32000, ex.Message);
                return "";
            }
        }

        static async Task<object> HandleToolCall(JsonNode? paramsNode)
        {
            if (paramsNode == null)
            {
                return ToolError("Params requeridos para tools/call");
            }

            var toolName = paramsNode["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(toolName))
            {
                return ToolError("'name' requerido en tools/call");
            }

            var arguments = paramsNode["arguments"] as JsonObject ?? new JsonObject();

            var command = new JsonObject
            {
                ["tool"] = toolName,
                ["params"] = JsonNode.Parse(arguments.ToJsonString())
            };

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(125));
                using var content = new StringContent(command.ToJsonString(), Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(PLUGIN_URL, content, cts.Token);
                var pluginResponse = await response.Content.ReadAsStringAsync(cts.Token);

                JsonNode? parsed;
                try
                {
                    parsed = JsonNode.Parse(pluginResponse);
                }
                catch
                {
                    return ToolError("El plugin devolvió una respuesta no válida.");
                }

                var ok = parsed?["ok"]?.GetValue<bool>() ?? false;
                var data = parsed?["data"];
                var userMessage = parsed?["user_message"]?.GetValue<string>() ?? "";
                var warnings = parsed?["warnings"];

                if (!ok)
                {
                    string err = parsed?["error"]?.GetValue<string>() ?? "Error en plugin";
                    return ToolError(err);
                }

                string dataText = data?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "{}";

                if (warnings != null || !string.IsNullOrWhiteSpace(userMessage))
                {
                    var envelope = new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = dataText
                            }
                        },
                        isError = false
                    };
                    return envelope;
                }

                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = dataText
                        }
                    },
                    isError = false
                };
            }
            catch (OperationCanceledException)
            {
                return ToolError("La herramienta excedió el tiempo límite. Segmenta más el alcance con Selection Sets o por nivel.");
            }
            catch (HttpRequestException ex)
            {
                return ToolError($"No se puede conectar al plugin. ¿Navisworks abierto? {ex.Message}");
            }
            catch (Exception ex)
            {
                return ToolError(ex.Message);
            }
        }

        static object ToolError(string message) => new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = message
                }
            },
            isError = true
        };

        static object BuildToolsList() => new
        {
            tools = new object[]
            {
                MakeTool("ping",
                    "Verifica conexión con el plugin de Navisworks.",
                    new { }, Array.Empty<string>()),

                MakeTool("list_source_files",
                    "Lista archivos NWC/NWD del modelo activo.",
                    new { }, Array.Empty<string>()),

                MakeTool("get_model_summary",
                    "Resumen del modelo: categorías, niveles y conteos.",
                    new
                    {
                        scope_mode = Str("all | selection | selection_set | level"),
                        selection_set = Str("Nombre del Selection Set. Opcional."),
                        level = Str("Nivel. Opcional.")
                    },
                    Array.Empty<string>()),

                MakeTool("list_selection_sets",
                    "Lista todos los Selection Sets del modelo.",
                    new { }, Array.Empty<string>()),

                MakeTool("extract_from_set",
                    "Cuantifica elementos de un Selection Set.",
                    new
                    {
                        set_name = Str("Nombre del Selection Set."),
                        output_mode = Str("summary | detail | auto"),
                        max_items = Int("Máximo de candidatos permitido. Opcional."),
                        max_nodes = Int("Máximo de nodos a visitar. Opcional."),
                        strict_limits = Bool("Bloquear si se supera el umbral seguro.")
                    },
                    new[] { "set_name" }),

                MakeTool("extract_from_current_selection",
                    "Cuantifica la selección activa en Navisworks.",
                    new
                    {
                        output_mode = Str("summary | detail | auto"),
                        max_items = Int("Máximo de candidatos permitido. Opcional."),
                        max_nodes = Int("Máximo de nodos a visitar. Opcional."),
                        strict_limits = Bool("Bloquear si se supera el umbral seguro.")
                    },
                    Array.Empty<string>()),

                MakeTool("extract_quantities",
                    "BOQ por categoría y nivel. Sin filtros = todo el alcance.",
                    new
                    {
                        categories = ArrStr("Categorías. Vacío = todas."),
                        level = Str("Nivel. Opcional."),
                        scope_mode = Str("all | selection | selection_set | level"),
                        selection_set = Str("Nombre del Selection Set. Opcional."),
                        output_mode = Str("summary | detail | auto"),
                        max_items = Int("Máximo de candidatos permitido. Opcional."),
                        max_nodes = Int("Máximo de nodos a visitar. Opcional."),
                        strict_limits = Bool("Bloquear si se supera el umbral seguro.")
                    },
                    Array.Empty<string>()),

                MakeTool("run_preconstruccion_1",
                    "Corrida Arquitectónica.",
                    CommonRunOptions(),
                    Array.Empty<string>()),

                MakeTool("run_preconstruccion_2",
                    "Corrida Estructural Concreto.",
                    CommonRunOptions(),
                    Array.Empty<string>()),

                MakeTool("run_preconstruccion_2_manual",
                    "Corrida 2 manual: usa la seleccion activa en Navisworks para elementos estructurales. Extrae volumenes, areas, materiales, descripcion y ubicacion estructural.",
                    new
                    {
                        output_mode = Str("summary | detail | auto"),
                        strict_limits = Bool("Bloquear si la seleccion excede el umbral seguro."),
                        max_items = Int("Maximo de elementos seleccionados permitidos. Opcional.")
                    },
                    Array.Empty<string>()),

                MakeTool("run_preconstruccion_3",
                    "Corrida Estructura Metálica: peso KG = Nominal Weight x Length. Fallback: Volumen x 7850 kg/m3.",
                    CommonRunOptions(),
                    Array.Empty<string>()),

                MakeTool("run_preconstruccion_3_manual",
                    "Corrida 3 manual: usa la seleccion activa en Navisworks. Peso KG = Nominal Weight x Length. Fallback: Volumen x 7850 kg/m3.",
                    new
                    {
                        output_mode = Str("summary | detail | auto"),
                        strict_limits = Bool("Bloquear si la seleccion excede el umbral seguro."),
                        max_items = Int("Maximo de elementos seleccionados permitidos. Opcional.")
                    },
                    Array.Empty<string>()),
                MakeTool("run_preconstruccion_3_probe",
                    "Diagnostico controlado de Corrida 3 sobre 5-10 elementos de Structural Framing / Structural Columns.",
                    new
                    {
                        scope_mode = Str("all | selection | selection_set | level"),
                        selection_set = Str("Nombre del Selection Set. Opcional."),
                        level = Str("Nivel. Opcional."),
                        max_probe = Int("Máximo de elementos a probar. Recomendado: 5 o 10.")
                    },
                    Array.Empty<string>()),

                MakeTool("highlight_elements",
                    "Resalta elementos por categoría y/o nivel.",
                    new
                    {
                        categories = ArrStr("Categorías."),
                        level = Str("Nivel. Opcional.")
                    },
                    Array.Empty<string>()),

                MakeTool("clear_selection",
                    "Limpia la selección activa.",
                    new { }, Array.Empty<string>()),

                MakeTool("dump_properties",
                    "Diagnóstico: propiedades de nodos geométricos.",
                    new
                    {
                        max_items = Int("Items (default 3).")
                    },
                    Array.Empty<string>()),

                MakeTool("dump_geo_set",
                    "Diagnóstico: propiedades de nodos de un Selection Set.",
                    new
                    {
                        set_name = Str("Nombre del Selection Set."),
                        max_items = Int("Items (default 3).")
                    },
                    new[] { "set_name" }),

                MakeTool("dump_instancia",
                    "Diagnóstico: Área, Volumen y Longitud de instancias Revit.",
                    new
                    {
                        categoria = Str("Categoría (default: Walls)."),
                        max_items = Int("Items (default 3).")
                    },
                    Array.Empty<string>()),

                MakeTool("get_parameters",
                    "Propiedades disponibles para una categoría.",
                    new
                    {
                        category = Str("Categoría Revit.")
                    },
                    new[] { "category" }),

                MakeTool("export_json",
                    "Exporta BOQ a JSON.",
                    new
                    {
                        output_path = Str("Ruta de salida. Opcional.")
                    },
                    Array.Empty<string>())
            }
        };

        static object CommonRunOptions() => new
        {
            scope_mode = Str("all | selection | selection_set | level"),
            selection_set = Str("Nombre del Selection Set. Opcional."),
            level = Str("Nivel. Opcional."),
            output_mode = Str("summary | detail | auto"),
            max_items = Int("Máximo de candidatos permitido. Opcional."),
            max_nodes = Int("Máximo de nodos a visitar. Opcional."),
            strict_limits = Bool("Bloquear si se supera el umbral seguro.")
        };

        static object MakeTool(string name, string description, object inputProperties, string[] required) => new
        {
            name,
            description,
            inputSchema = new
            {
                type = "object",
                properties = inputProperties,
                required
            }
        };

        static object Str(string description) => new { type = "string", description };
        static object Int(string description) => new { type = "integer", description };
        static object Bool(string description) => new { type = "boolean", description };
        static object ArrStr(string description) => new { type = "array", items = new { type = "string" }, description };

        static object BuildInitializeResult() => new
        {
            protocolVersion = "2025-11-25",
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = new
            {
                name = "NavisBOQ-MCP",
                version = "2.0.0"
            }
        };

        static string BuildResponse(JsonNode? id, object? result) =>
            JsonSerializer.Serialize(new { jsonrpc = "2.0", id, result }, _jsonOpts);

        static string BuildErrorResponse(JsonNode? id, int code, string message) =>
            JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code, message } }, _jsonOpts);

        static async Task<bool> PingPlugin()
        {
            try
            {
                using var body = new StringContent("{\"tool\":\"ping\",\"params\":{}}", Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(PLUGIN_URL, body);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}