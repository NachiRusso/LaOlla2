using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaOlla.Modelo
{
    public class Resultado
    {
        
       
            public bool Ok { get; init; }
            public string Mensaje { get; init; } = "";

            public static Resultado Exito(string mensaje) => new Resultado { Ok = true, Mensaje = mensaje };
            public static Resultado Fallo(string mensaje) => new Resultado { Ok = false, Mensaje = mensaje };
        
    }
}