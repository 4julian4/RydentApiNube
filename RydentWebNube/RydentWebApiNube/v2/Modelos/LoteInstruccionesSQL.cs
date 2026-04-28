using System.Collections.Generic;

namespace RydentWebApiNube.v2.Modelos
{
    public class ComandoSQLItem
    {
        public string ClaveResultado { get; set; } = string.Empty;
        public string ComandoSQL { get; set; } = string.Empty;
        public Dictionary<string, object>? Parametros { get; set; }
    }

    public class LoteInstruccionesSQL
    {
        public string AccionOriginal { get; set; } = string.Empty; // Ej: "ObtenerDoctor"
        public string? IdPeticion { get; set; }
        public bool ResponderPorApiHttp { get; set; } = false;
        public string? EndpointRespuesta { get; set; }
        
        public List<ComandoSQLItem> Consultas { get; set; } = new List<ComandoSQLItem>();
    }
}