using System.Text.Json;
using RydentWebApiNube.v2.Modelos;
using RydentWebApiNube.v2.Modelos.Odontograma;
using RydentWebApiNube.v2.Servicios;
using RydentWebApiNube.v2.Servicios.Odontograma;

namespace RydentWebApiNube.v2.Acciones
{
    public sealed class ObtenerOdontogramaActualAccionWorker : IAccionWorker
    {
        public string NombreAccion => "ODONTOGRAMA_OBTENER_ACTUAL";

        public LoteInstruccionesSQL GenerarLote(Dictionary<string, object> parametros)
        {
            int idTratamiento = ObtenerInt(parametros, "idTratamiento");
            DateTime fecha = ObtenerFecha(parametros, "fecha");

            return new LoteInstruccionesSQL
            {
                AccionOriginal = NombreAccion,
                Consultas = new List<ComandoSQLItem>
                {
                    new ComandoSQLItem
                    {
                        ClaveResultado = "odontograma",
                        ComandoSQL = @"
                            SELECT
                                IDDIENTES,
                                FECHA,
                                NOMBRE,
                                CARIES,
                                ENFERMEDAD,
                                RECIDIVA,
                                AMALGAMA,
                                IONOMERO,
                                ENFERMEDAD2,
                                ENFERMEDAD3,
                                OBSERVACIONES,
                                FRACTURA,
                                AMALGAMADES,
                                RESINADES,
                                RESINA,
                                RESINAINDICADA,
                                IONOMEROINDICADO,
                                IONOMERODESADAPTADO,
                                ABRASION
                            FROM DIENTESF
                            WHERE IDDIENTES = @IDTRATAMIENTO
                              AND FECHA = @FECHA
                            ",
                        Parametros = new Dictionary<string, object>
                        {
                            ["IDTRATAMIENTO"] = idTratamiento,
                            ["FECHA"] = fecha.Date
                        }
                    }
                }
            };
        }

        public string TraducirParaAngular(Dictionary<string, JsonElement> datosWorkerCrudos)
        {
            var row = LeerPrimeraFila(datosWorkerCrudos, "odontograma");

            if (row == null)
                return Serializar(null);

            var legacyRow = MapearLegacyRow(row.Value);

            var dto = OdontogramaLegacyParser.Parse(legacyRow);

            return Serializar(dto);
        }

        private static OdontogramaLegacyRow MapearLegacyRow(JsonElement row)
        {
            return new OdontogramaLegacyRow
            {
                IdTratamiento = GetInt(row, "IDDIENTES"),
                Fecha = GetDateTime(row, "FECHA"),
                Tipo = "Actual",
                OrigenLegacy = "DIENTESF",

                Nombre = GetString(row, "NOMBRE"),
                Caries = GetString(row, "CARIES"),
                Enfermedad = GetString(row, "ENFERMEDAD"),
                Recidiva = GetString(row, "RECIDIVA"),
                Amalgama = GetString(row, "AMALGAMA"),
                Ionomero = GetString(row, "IONOMERO"),
                Enfermedad2 = GetString(row, "ENFERMEDAD2"),
                Enfermedad3 = GetString(row, "ENFERMEDAD3"),
                Observaciones = GetString(row, "OBSERVACIONES"),
                Fractura = GetString(row, "FRACTURA"),
                AmalgamaDes = GetString(row, "AMALGAMADES"),
                ResinaDes = GetString(row, "RESINADES"),
                Resina = GetString(row, "RESINA"),
                ResinaIndicada = GetString(row, "RESINAINDICADA"),
                IonomeroIndicado = GetString(row, "IONOMEROINDICADO"),
                IonomeroDesadaptado = GetString(row, "IONOMERODESADAPTADO"),
                Abrasion = GetString(row, "ABRASION"),
                Firma = null
            };
        }

        private static JsonElement? LeerPrimeraFila(
            Dictionary<string, JsonElement> datos,
            string claveResultado)
        {
            if (!datos.TryGetValue(claveResultado, out var elemento))
                return null;

            if (elemento.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var fila in elemento.EnumerateArray())
                return fila;

            return null;
        }

        private static int ObtenerInt(Dictionary<string, object> parametros, string nombre)
        {
            if (!parametros.TryGetValue(nombre, out var valor))
                throw new ArgumentException($"Falta el parámetro {nombre}.");

            return Convert.ToInt32(valor);
        }

        private static DateTime ObtenerFecha(Dictionary<string, object> parametros, string nombre)
        {
            if (!parametros.TryGetValue(nombre, out var valor))
                throw new ArgumentException($"Falta el parámetro {nombre}.");

            if (valor is DateTime fecha)
                return fecha;

            return DateTime.Parse(valor.ToString()!);
        }

        private static string? GetString(JsonElement row, string propertyName)
        {
            if (!row.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            return prop.GetString();
        }

        private static int GetInt(JsonElement row, string propertyName)
        {
            if (!row.TryGetProperty(propertyName, out var prop))
                throw new Exception($"No llegó la columna {propertyName}.");

            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetInt32();

            return Convert.ToInt32(prop.GetString());
        }

        private static DateTime GetDateTime(JsonElement row, string propertyName)
        {
            if (!row.TryGetProperty(propertyName, out var prop))
                throw new Exception($"No llegó la columna {propertyName}.");

            if (prop.ValueKind == JsonValueKind.String)
                return DateTime.Parse(prop.GetString()!);

            return prop.GetDateTime();
        }

        private static string Serializar(object? data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}