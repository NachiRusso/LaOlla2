using LaOlla.Modelo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaOlla.Controlador
{
    public class ReservaController
    {
        private readonly ReservaRepositorio _reservaRepo = new ReservaRepositorio();
        private readonly MenuRepositorio _menuRepo = new MenuRepositorio();

        public Resultado Reservar(int menuId, string identificacionEstudiante, string nombreEstudiante, DateTime fechaReserva, TimeSpan horaActual)
        {
            // Validación de campos básicos (Pregunta 2: autogestión / anotarse en el comedor)
            if (menuId <= 0)
                return Resultado.Fallo("Debe seleccionar una vianda del día para reservar.");

            if (string.IsNullOrWhiteSpace(identificacionEstudiante) || string.IsNullOrWhiteSpace(nombreEstudiante))
                return Resultado.Fallo("Regla R2: Debe ingresar identificación (DNI/Legajo) y nombre del estudiante para anotarlo.");

            // Regla R4 (Pregunta 8): ¿Se pueden hacer reservas para días futuros? No, solo para el día actual.
            if (fechaReserva.Date != DateTime.Today)
                return Resultado.Fallo("Regla R4: No se permiten reservas para días futuros ni pasados. Solo se reserva para el día actual.");

            // Regla R3 (Preguntas 3 y 10): Rango de reservas de 00:00 (12 de la noche) hasta las 12:00 (12 del mediodía).
            var horaInicio = new TimeSpan(0, 0, 0);
            var horaLimite = new TimeSpan(12, 0, 0);
            if (horaActual < horaInicio || horaActual > horaLimite)
                return Resultado.Fallo("Regla R3: Las reservas solo pueden realizarse desde las 00:00 hs hasta las 12:00 hs del mediodía.");

            // Regla R1 (Pregunta 1): El estudiante solo puede hacer una reserva diaria (la del mediodía).
            if (_reservaRepo.EstudianteYaReservoEnFecha(identificacionEstudiante.Trim(), DateTime.Today))
                return Resultado.Fallo($"Regla R1: El estudiante '{identificacionEstudiante.Trim()}' ya posee una reserva activa para el mediodía de hoy.");

            // Regla R5 (Pregunta 5): No superar el cupo del día (máximo 150 viandas).
            int cupoMaximoHoy = _menuRepo.ObtenerCupoMaximoDelDia(DateTime.Today);
            int ocupadasHoy = _reservaRepo.ContarReservasQueOcupanCupo(DateTime.Today);

            if (ocupadasHoy >= cupoMaximoHoy)
                return Resultado.Fallo($"Regla R5: Cupo agotado ({ocupadasHoy}/{cupoMaximoHoy}). El sistema no permite prometer viandas de más.");

            var nuevaReserva = new Reserva
            {
                MenuId = menuId,
                Fecha = DateTime.Today,
                IdentificacionEstudiante = identificacionEstudiante.Trim(),
                NombreEstudiante = nombreEstudiante.Trim(),
                HoraRegistro = DateTime.Today.Add(horaActual),
                Estado = "Reservada"
            };

            bool guardado = _reservaRepo.InsertarReservaTransaccional(nuevaReserva, cupoMaximoHoy);
            if (!guardado)
                return Resultado.Fallo("Regla R5: No quedan viandas disponibles en el cupo del día.");

            int restantes = cupoMaximoHoy - (ocupadasHoy + 1);
            return Resultado.Exito($"Reserva registrada con éxito para {nuevaReserva.NombreEstudiante}. Cupo disponible restante: {restantes}.");
        }

        // Regla R8 (Pregunta 9): Se pueden cancelar reservas hasta las 12 del mediodía y se recupera el cupo.
        public Resultado CancelarReserva(int reservaId, TimeSpan horaActual)
        {
            var reserva = _reservaRepo.ObtenerPorId(reservaId);
            if (reserva == null)
                return Resultado.Fallo("No se encontró la reserva seleccionada.");

            if (reserva.Estado != "Reservada")
                return Resultado.Fallo($"No se puede cancelar una reserva cuyo estado actual es '{reserva.Estado}'.");

            var horaLimiteCancelacion = new TimeSpan(12, 0, 0);
            if (horaActual > horaLimiteCancelacion)
                return Resultado.Fallo("Regla R8: Las reservas solo pueden cancelarse hasta las 12:00 hs del mediodía.");

            _reservaRepo.CambiarEstado(reservaId, "Cancelada");
            return Resultado.Exito($"Reserva #{reservaId} cancelada correctamente. Se ha recuperado 1 lugar en el cupo del día.");
        }

        // Regla R9 (Preguntas 6 y 12): No se retiran viandas sin reserva; el personal ve qué vianda se pidió y cambia el estado a Retirada.
        public Resultado EntregarVianda(int reservaId, TimeSpan horaActual)
        {
            if (reservaId <= 0)
                return Resultado.Fallo("Regla R9: No se pueden retirar viandas sin una reserva previa registrada.");

            var reserva = _reservaRepo.ObtenerPorId(reservaId);
            if (reserva == null)
                return Resultado.Fallo("Regla R9: La reserva indicada no existe. No se entregan viandas sin reserva.");

            if (horaActual >= new TimeSpan(14, 0, 0))
                return Resultado.Fallo("Regla R10: El comedor cerró a las 14:00 hs. Las viandas no retiradas pasaron a estado 'No Retirada'.");

            if (reserva.Estado != "Reservada")
                return Resultado.Fallo($"No se puede entregar la vianda porque la reserva está en estado '{reserva.Estado}'.");

            _reservaRepo.CambiarEstado(reservaId, "Retirada");
            return Resultado.Exito($"Vianda entregada a {reserva.NombreEstudiante}: [{reserva.TipoVianda}] {reserva.NombreVianda}. Estado cambiado a 'Retirada'.");
        }

        // Regla R10 (Pregunta 4): A las 2 de la tarde (14:00 hs), las viandas sin retirar cambian su estado a No Retiradas.
        public Resultado ProcesarCierreDeLas14Hs(TimeSpan horaActual)
        {
            var horaCierre = new TimeSpan(14, 0, 0);
            if (horaActual < horaCierre)
                return Resultado.Fallo("Regla R10: El cambio a 'No Retirada' solo se ejecuta a partir de las 14:00 hs (2 de la tarde).");

            int afectadas = _reservaRepo.MarcarNoRetiradasDelDia(DateTime.Today);
            return Resultado.Exito($"Cierre de las 14:00 hs procesado: {afectadas} vianda(s) sin retirar cambiaron su estado a 'No Retirada'.");
        }

        public int ObtenerCupoDisponibleHoy()
        {
            int max = _menuRepo.ObtenerCupoMaximoDelDia(DateTime.Today);
            int ocupadas = _reservaRepo.ContarReservasQueOcupanCupo(DateTime.Today);
            return Math.Max(0, max - ocupadas);
        }

        public int ObtenerCupoOcupadoHoy() => _reservaRepo.ContarReservasQueOcupanCupo(DateTime.Today);

        public List<Reserva> ListarReservasDeHoy() => _reservaRepo.ObtenerReservasDelDia(DateTime.Today);
    }
}