using RydentWebApiNube.v2.Modelos;

namespace RydentWebApiNube.v2.Gateways
{
    public interface IWorkerGateway
{
    Task<string> EjecutarLoteAsync(LoteInstruccionesSQL lote);
}
}