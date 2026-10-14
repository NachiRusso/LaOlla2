using LaOlla.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaOlla.Controlador
{
    public class MenuController
    {
        private readonly MenuRepositorio _menuRepo = new MenuRepositorio();
        private readonly ReservaRepositorio _reservaRepo = new ReservaRepositorio();

        private static readonly HashSet<string> TiposValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Vegana", "Vegetariana", "Común", "Sin TACC"
        };

        // Validación de Login Administrador (Regla R6 - Pregunta 7)
        public Resultado ValidarLoginAdmin(string usuario, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
                return Resultado.Fallo("Debe ingresar usuario y contraseña de administrador.");

            if (usuario.Trim() == "admin" && contrasena == "admin123")
                return Resultado.Exito("Sesión iniciada como Administrador.");

            return Resultado.Fallo("Credenciales incorrectas. Usuario o contraseña de administrador inválidos.");
        }

        // Validación de Login Estudiante por Autogestión (Regla R2 - Pregunta 2: solo con DNI)
        public Resultado ValidarIngresoEstudiante(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni))
                return Resultado.Fallo("Debe ingresar su número de DNI para autogestionar su reserva.");

            string dniLimpio = dni.Trim();
            if (dniLimpio.Length < 6 || !dniLimpio.All(char.IsDigit))
                return Resultado.Fallo("El DNI ingresado no es válido. Ingrese solo números (mínimo 6 dígitos).");

            return Resultado.Exito($"Bienvenido/a. Autogestión habilitada para el DNI {dniLimpio}.");
        }

        // Regla R7 (Pregunta 11): La vianda tiene 3 atributos: Tipo (Vegana, Vegetariana, Común, Sin TACC), Nombre y Descripción.
        public Resultado CargarViandaDelDia(string tipo, string nombre, string descripcion, bool esAdministrador)
        {
            if (!esAdministrador)
                return Resultado.Fallo("Solo el personal/administrador puede cargar las viandas del día.");

            if (string.IsNullOrWhiteSpace(tipo) || !TiposValidos.Contains(tipo.Trim()))
                return Resultado.Fallo("Regla R7: El tipo de vianda debe ser Vegana, Vegetariana, Común o Sin TACC.");

            if (string.IsNullOrWhiteSpace(nombre))
                return Resultado.Fallo("Regla R7: El nombre de la vianda es obligatorio.");

            if (string.IsNullOrWhiteSpace(descripcion))
                return Resultado.Fallo("Regla R7: La descripción de la vianda es obligatoria.");

            try
            {
                var nuevaVianda = new Menu
                {
                    Fecha = DateTime.Today,
                    Tipo = tipo.Trim(),
                    Nombre = nombre.Trim(),
                    Descripcion = descripcion.Trim()
                };

                _menuRepo.GuardarVianda(nuevaVianda);
                return Resultado.Exito($"Vianda '{nuevaVianda.Nombre}' ({nuevaVianda.Tipo}) cargada para el día de hoy.");
            }
            catch (Exception ex)
            {
                return Resultado.Fallo($"Error al guardar en base de datos: {ex.Message}");
            }
        }

        // Reglas R5 y R6 (Preguntas 5 y 7): Cupo máximo 150, modificable únicamente por el Administrador.
        public Resultado ModificarCupoDiario(int nuevoCupo, bool esAdministrador)
        {
            if (!esAdministrador)
                return Resultado.Fallo("Regla R6: El cupo diario solo puede ser modificado por el Administrador.");

            if (nuevoCupo <= 0 || nuevoCupo > 150)
                return Resultado.Fallo("Regla R5: El cupo máximo permitido es de 150 viandas (no más de 150 y mayor a 0).");

            try
            {
                int ocupadasHoy = _reservaRepo.ContarReservasQueOcupanCupo(DateTime.Today);
                if (nuevoCupo < ocupadasHoy)
                    return Resultado.Fallo($"No se puede fijar un cupo de {nuevoCupo} porque ya existen {ocupadasHoy} reservas activas hoy.");

                _menuRepo.ActualizarCupoMaximoDelDia(DateTime.Today, nuevoCupo);
                return Resultado.Exito($"Cupo del día actualizado a {nuevoCupo} viandas por el Administrador.");
            }
            catch (Exception ex)
            {
                return Resultado.Fallo($"Error al actualizar el cupo en base de datos: {ex.Message}");
            }
        }

        public List<Menu> ObtenerViandasDeHoy() => _menuRepo.ObtenerViandasPorFecha(DateTime.Today);

        public int ObtenerCupoMaximoHoy() => _menuRepo.ObtenerCupoMaximoDelDia(DateTime.Today);
    }
}