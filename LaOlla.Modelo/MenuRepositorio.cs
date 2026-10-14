using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LaOlla.Modelo
{
    public class MenuRepositorio
    {
        public void GuardarVianda(Menu menu)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            // Asegurar que exista el registro de cupo diario (por defecto 150)
            const string sqlCupo = @"
                IF NOT EXISTS (SELECT 1 FROM CupoDiario WHERE Fecha = @Fecha)
                    INSERT INTO CupoDiario (Fecha, CupoMaximo) VALUES (@Fecha, 150);";
            using (var cmdCupo = new SqlCommand(sqlCupo, conn))
            {
                cmdCupo.Parameters.AddWithValue("@Fecha", menu.Fecha.Date);
                cmdCupo.ExecuteNonQuery();
            }

            const string sql = @"
                INSERT INTO Menu (Fecha, Tipo, Nombre, Descripcion)
                VALUES (@Fecha, @Tipo, @Nombre, @Descripcion);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Fecha", menu.Fecha.Date);
            cmd.Parameters.AddWithValue("@Tipo", menu.Tipo);
            cmd.Parameters.AddWithValue("@Nombre", menu.Nombre);
            cmd.Parameters.AddWithValue("@Descripcion", menu.Descripcion);
            cmd.ExecuteNonQuery();
        }

        public List<Menu> ObtenerViandasPorFecha(DateTime fecha)
        {
            var lista = new List<Menu>();
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = "SELECT Id, Fecha, Tipo, Nombre, Descripcion FROM Menu WHERE Fecha = @Fecha ORDER BY Tipo, Nombre;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Fecha", fecha.Date);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Menu
                {
                    Id = reader.GetInt32(0),
                    Fecha = reader.GetDateTime(1),
                    Tipo = reader.GetString(2),
                    Nombre = reader.GetString(3),
                    Descripcion = reader.GetString(4)
                });
            }
            return lista;
        }

        public int ObtenerCupoMaximoDelDia(DateTime fecha)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = "SELECT CupoMaximo FROM CupoDiario WHERE Fecha = @Fecha;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Fecha", fecha.Date);

            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 150;
        }

        public void ActualizarCupoMaximoDelDia(DateTime fecha, int nuevoCupo)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = @"
                IF EXISTS (SELECT 1 FROM CupoDiario WHERE Fecha = @Fecha)
                    UPDATE CupoDiario SET CupoMaximo = @Cupo WHERE Fecha = @Fecha;
                ELSE
                    INSERT INTO CupoDiario (Fecha, CupoMaximo) VALUES (@Fecha, @Cupo);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Fecha", fecha.Date);
            cmd.Parameters.AddWithValue("@Cupo", nuevoCupo);
            cmd.ExecuteNonQuery();
        }
    }
}
