namespace RydentWebApiNube.LogicaDeNegocio.Modelos
{
	public class WompiOptions
    {
        public string PublicKey { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
        public string IntegritySecret { get; set; } = string.Empty;
        public string EventsSecret { get; set; } = string.Empty;
        public string Currency { get; set; } = "COP";
        public string RedirectUrl { get; set; } = string.Empty;
    }
}
