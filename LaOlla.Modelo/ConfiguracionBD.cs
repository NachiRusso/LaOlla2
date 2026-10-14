using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace LaOlla.Modelo
{
    public static class ConfiguracionBD
    {
        public static string CadenaConexion =>
    "Server=localhost;Database=LaOllaDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
