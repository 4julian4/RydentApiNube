namespace RydentWebApiNube.LogicaDeNegocio.Modelos.WompiRequests
{
    public class Paquete
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int CantidadFacturas { get; set; }
        public int Precio { get; set; }
    }
}