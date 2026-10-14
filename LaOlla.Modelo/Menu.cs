using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaOlla.Modelo
{
    public class Menu
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = ""; // Vegana, Vegetariana, Común, Sin TACC
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";

        public string EtiquetaMostrador => $"[{Tipo}] {Nombre} - {Descripcion}";
    }
}
