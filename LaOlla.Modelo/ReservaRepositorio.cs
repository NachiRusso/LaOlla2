using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LaOlla.Modelo
{
    public class ReservaRepositorio
    {
        // Cuenta reservas que ocupan cupo (Reservada, Retirada o No Retirada; las Canceladas liberan cupo según Pregunta 9)
        public int ContarReservasQueOcupanCupo(DateTime fecha)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = @"
                SELECT COUNT(*) 
                FROM Reserva 
                WHERE Fecha = @Fecha AND Estado <> 'Cancelada';";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Fecha", fecha.Date);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool EstudianteYaReservoEnFecha(string identificacion, DateTime fecha)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = @"
                SELECT COUNT(*) 
                FROM Reserva 
                WHERE Fecha = @Fecha 
                  AND IdentificacionEstudiante = @Identificacion 
                  AND Estado <> 'Cancelada';";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Fecha", fecha.Date);
            cmd.Parameters.AddWithValue("@Identificacion", identificacion);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public bool InsertarReservaTransaccional(Reserva reserva, int cupoMaximoDia)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                const string sqlCount = @"
                    SELECT COUNT(*) 
                    FROM Reserva WITH (UPDLOCK, HOLDLOCK)
                    WHERE Fecha = @Fecha AND Estado <> 'Cancelada';";

                using (var cmdCount = new SqlCommand(sqlCount, conn, tx))
                {
                    cmdCount.Parameters.AddWithValue("@Fecha", reserva.Fecha.Date);
                    int ocupadas = Convert.ToInt32(cmdCount.ExecuteScalar());
                    if (ocupadas >= cupoMaximoDia)
                    {
                        tx.Rollback();
                        return false;
                    }
                }

                const string sqlInsert = @"
                    INSERT INTO Reserva (MenuId, Fecha, IdentificacionEstudiante, NombreEstudiante, HoraRegistro, Estado)
                    VALUES (@MenuId, @Fecha, @Identificacion, @Nombre, @HoraRegistro, @Estado);";

                using (var cmdIns = new SqlCommand(sqlInsert, conn, tx))
                {
                    cmdIns.Parameters.AddWithValue("@MenuId", reserva.MenuId);
                    cmdIns.Parameters.AddWithValue("@Fecha", reserva.Fecha.Date);
                    cmdIns.Parameters.AddWithValue("@Identificacion", reserva.IdentificacionEstudiante);
                    cmdIns.Parameters.AddWithValue("@Nombre", reserva.NombreEstudiante);
                    cmdIns.Parameters.AddWithValue("@HoraRegistro", reserva.HoraRegistro);
                    cmdIns.Parameters.AddWithValue("@Estado", reserva.Estado);
                    cmdIns.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public Reserva? ObtenerPorId(int reservaId)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = @"
                SELECT r.Id, r.MenuId, r.Fecha, r.IdentificacionEstudiante, r.NombreEstudiante, r.HoraRegistro, r.Estado,
                       m.Tipo, m.Nombre
                FROM Reserva r
                INNER JOIN Menu m ON r.MenuId = m.Id
                WHERE r.Id = @Id;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", reservaId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Reserva
                {
                    Id = reader.GetInt32(0),
                    MenuId = reader.GetInt32(1),
                    Fecha = reader.GetDateTime(2),
                    IdentificacionEstudiante = reader.GetString(3),
                    NombreEstudiante = reader.GetString(4),
                    HoraRegistro = reader.GetDateTime(5),
                    Estado = reader.GetString(6),
                    TipoVianda = reader.GetString(7),
                    NombreVianda = reader.GetString(8)
                };
            }
            return null;
        }

        public void CambiarEstado(int reservaId, string nuevoEstado)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = "UPDATE Reserva SET Estado = @Estado WHERE Id = @Id;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Estado", nuevoEstado);
            cmd.Parameters.AddWithValue("@Id", reservaId);
            cmd.ExecuteNonQuery();
        }

        public int MarcarNoRetiradasDelDia(DateTime fecha)
        {
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = @"
                UPDATE Reserva 
                SET Estado = 'No Retirada' 
                WHERE Fecha = @Fecha AND Estado = 'Reservada';";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Fecha", fecha.Date);
            return cmd.ExecuteNonQuery();
        }

        public List<Reserva> ObtenerReservasDelDia(DateTime fecha)
        {
            var lista = new List<Reserva>();
            using var conn = new SqlConnection(ConfiguracionBD.CadenaConexion);
            conn.Open();

            const string sql = @"
                SELECT r.Id, r.MenuId, r.Fecha, r.IdentificacionEstudiante, r.NombreEstudiante, r.HoraRegistro, r.Estado,
                       m.Tipo, m.Nombre
                FROM Reserva r
                INNER JOIN Menu m ON r.MenuId = m.Id
                WHERE r.Fecha = @Fecha
                ORDER BY r.Id DESC;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Fecha", fecha.Date);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Reserva
                {
                    Id = reader.GetInt32(0),
                    MenuId = reader.GetInt32(1),
                    Fecha = reader.GetDateTime(2),
                    IdentificacionEstudiante = reader.GetString(3),
                    NombreEstudiante = reader.GetString(4),
                    HoraRegistro = reader.GetDateTime(5),
                    Estado = reader.GetString(6),
                    TipoVianda = reader.GetString(7),
                    NombreVianda = reader.GetString(8)
                });
            }
            return lista;
        }
    }
}