using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaOlla.Modelo
{
    public class Reserva
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public DateTime Fecha { get; set; }
        public string IdentificacionEstudiante { get; set; } = "";
        public string NombreEstudiante { get; set; } = "";
        public DateTime HoraRegistro { get; set; }
        public string Estado { get; set; } = "Reservada";

        // Propiedades de lectura para que el personal vea qué vianda se pidió (Pregunta 12)
        public string TipoVianda { get; set; } = "";
        public string NombreVianda { get; set; } = "";
    }
}