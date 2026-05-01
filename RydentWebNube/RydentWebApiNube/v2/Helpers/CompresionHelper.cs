using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RydentWebApiNube.v2.Helpers
{
    public static class CompresionHelper
    {
        // Función para descomprimir lo que manda el Worker
        public static string DecompressString(string compressedText)
        {
            if (string.IsNullOrEmpty(compressedText))
                return string.Empty;

            byte[] bytes = Convert.FromBase64String(compressedText);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
        
        // Opcional: Por si en el Hub también necesitas comprimir algo hacia Angular
        public static string CompressString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(text);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }
                return Convert.ToBase64String(mso.ToArray());
            }
        }
    }
}