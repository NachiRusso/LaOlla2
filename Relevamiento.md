# Relevamiento — Comedor Universitario La Olla

## 1. Equipo y roles
- **Coordinación y Arquitectura:** Nazareno Russo y equipo
- **Entrevista y Escriba:** Equipo de relevamiento
- **Datos (SQL Server / ADO.NET):** Equipo de persistencia
- **Controlador y Vista (WinForms .NET 8):** Equipo de desarrollo y pruebas

---

## 2. El problema en una frase
El mostrador del comedor universitario "La Olla" registra las viandas del mediodía en una libreta de papel con tachones y sin control en tiempo real del cupo diario (máximo 150 viandas), provocando que se prometan almuerzos por encima del límite disponible.

---

## 3. Preguntas realizadas en la entrevista y respuestas literales del cliente

1. **¿Cuántas reservas diarias puede hacer un estudiante?**
   - *Respuesta:* El estudiante solo puede hacer una, la del mediodía.
2. **¿Es necesario gestionar a los estudiantes que reservan en el comedor?**
   - *Respuesta:* El estudiante se autogestiona. Solamente se anota en el comedor.
3. **Rango en el que se pueden hacer reservas.**
   - *Respuesta:* A partir de las 12 de la noche (00:00 hs) hasta las 12 del mediodía (12:00 hs).
4. **¿Qué pasa si quedan viandas sin retirar a la hora del cierre?**
   - *Respuesta:* A las 2 de la tarde (14:00 hs), las viandas que hayan quedado sin retirar, cambian su estado a No Retiradas.
5. **¿Cuál es el cupo máximo?**
   - *Respuesta:* El cupo máximo es 150 viandas. No más.
6. **¿Se pueden retirar viandas sin reservas?**
   - *Respuesta:* No.
7. **¿Se puede modificar el cupo?**
   - *Respuesta:* El cupo puede ser modificado solamente por el administrador.
8. **¿Se pueden hacer reservas para días futuros?**
   - *Respuesta:* No, solo para el día actual.
9. **¿Se puede cancelar una reserva? Si es que se puede, ¿se recupera el cupo?**
   - *Respuesta:* Sí se pueden cancelar las reservas y sí se recupera el cupo. Se pueden cancelar hasta las 12 del mediodía.
10. **¿Hasta qué hora se puede reservar en el mismo día?**
    - *Respuesta:* Se puede reservar hasta las 12 del mediodía.
11. **¿Cuáles son los atributos de la vianda?**
    - *Respuesta:* Son tres: Tipo de vianda (vegana, vegetariana, común y sin TACC), Nombre y Descripción.
12. **¿Qué conocimientos debe de tener el personal que entregue la vianda?**
    - *Respuesta:* El personal tiene que: Saber qué vianda se pidió y cambiarle el estado a la comida.

---

## 4. Reglas de negocio numeradas (Implementadas en `LaOlla.Controlador`)

- **R1 (Reserva única diaria — Pregunta 1):** Cada estudiante puede realizar únicamente 1 reserva activa por día para el turno del mediodía (`ReservaController.Reservar`).
- **R2 (Registro sin padrón previo — Pregunta 2):** No existe un ABM/gestión previa de estudiantes; el estudiante solo se anota indicando su identificación y nombre en el acto (`ReservaController.Reservar`).
- **R3 (Franja horaria de reserva — Preguntas 3 y 10):** Las reservas solo se aceptan entre las 00:00 hs y las 12:00 hs del mediodía (`ReservaController.Reservar`).
- **R4 (Reserva exclusiva en el día actual — Pregunta 8):** No se permiten reservas para fechas futuras ni pasadas; la fecha de reserva debe coincidir con `DateTime.Today` (`ReservaController.Reservar`).
- **R5 (Tope máximo de 150 viandas — Pregunta 5):** El cupo diario jamás puede superar las 150 viandas y el sistema rechaza nuevas reservas cuando las reservas activas alcanzan el cupo vigente del día (`MenuController.ModificarCupoDiario` y `ReservaController.Reservar`).
- **R6 (Modificación de cupo restringida al Administrador — Pregunta 7):** Solo el rol Administrador puede modificar el cupo del día, siempre que sea `<= 150` y `>= reservas activas actuales` (`MenuController.ModificarCupoDiario`).
- **R7 (Atributos obligatorios de la vianda — Pregunta 11):** Toda vianda del día requiere obligatoriamente Tipo (`Común`, `Vegetariana`, `Vegana` o `Sin TACC`), Nombre y Descripción (`MenuController.CargarViandaDelDia`).
- **R8 (Cancelación hasta las 12:00 hs con recuperación de cupo — Pregunta 9):** Una reserva en estado `Reservada` puede cancelarse hasta las 12:00 hs, cambiando a estado `Cancelada` y liberando inmediatamente 1 lugar en el cupo del día (`ReservaController.CancelarReserva`).
- **R9 (Prohibición de retiro sin reserva y entrega — Preguntas 6 y 12):** No se entregan viandas sin reserva previa. Para entregar, la reserva debe estar en estado `Reservada`, el personal visualiza el tipo y nombre de vianda pedida y cambia su estado a `Retirada` (`ReservaController.EntregarVianda`).
- **R10 (Vencimiento a las 14:00 hs — Pregunta 4):** A partir de las 14:00 hs, las reservas del día que sigan en estado `Reservada` (sin retirar) pasan al estado `No Retirada` (`ReservaController.ProcesarCierreDeLas14Hs`).

---

## 5. Supuestos explícitos del equipo y análisis de riesgo (Para desempate)

*Nota: Ninguno de estos supuestos contradice al cliente; cubren los datos mínimos necesarios para ejecutar las reglas relevadas.*

- **S1 — Identificador del estudiante al anotarse (DNI/Legajo + Nombre):**
  - *Por qué se asume:* El cliente indicó que no se gestiona una tabla de estudiantes (P2), pero exigió validar que un estudiante no reserve dos veces el mismo día (P1) y saber quién reservó (Enunciado). Asumimos que al anotarse se ingresa un identificador (`DNI/Legajo`) y su `Nombre`.
  - *Riesgo:* Si el estudiante escribe un DNI erróneo, podría eludir la validación de unicidad diaria. Mitigación: el mostrador verifica el número al anotar o entregar.
- **S2 — El cupo diario (máximo 150) es global para la tanda del día:**
  - *Por qué se asume:* El cliente definió "un cupo máximo de 150 viandas" para el comedor (P5) y 4 tipos de vianda (P11), sin especificar sub-cupos individuales por tipo de dieta. Asumimos un único contador diario global (150 máx.) compartido por las opciones de vianda cargadas en el día.
  - *Riesgo:* Si la cocina tuviera un límite físico separado para platos "Sin TACC", el cupo global no lo frenaría por separado.
- **S3 — Máquina de 4 estados de la reserva (`Reservada`, `Retirada`, `Cancelada`, `No Retirada`):**
  - *Por qué se asume:* Surge de unir las respuestas 4, 6, 9 y 12. Asumimos que el estado inicial es `Reservada`, al entregar pasa a `Retirada`, al cancelar antes de las 12:00 pasa a `Cancelada` (único estado que devuelve cupo), y a las 14:00 hs pasa a `No Retirada`.
  - *Riesgo:* Nulo; refleja fielmente el ciclo de vida descripto por el cliente.
- **S4 — Selector de hora operativa en pantalla para verificación y Modo Administrador por casilla:**
  - *Por qué se asume:* Como la restricción técnica excluye login de usuarios y la demo del torneo ocurre fuera del horario de reserva (00:00 a 12:00 hs), incluimos un selector de hora operativa y un conmutador de Administrador en la vista para probar todas las franjas horarias en vivo.
  - *Riesgo:* En producción el reloj debería leerse exclusivamente del servidor (`GETDATE()`) para evitar cambios manuales de hora en mostrador.

---

## 6. Alcance de esta versión

### Dentro del alcance (Implementado y funcionando)
- Alta de viandas del día con Tipo (`Común`, `Vegetariana`, `Vegana`, `Sin TACC`), Nombre y Descripción.
- Gestión y visualización en tiempo real del cupo del día (por defecto 150, modificable por Administrador hasta 150).
- Alta de reservas del día actual con control de horario (00:00–12:00), control de duplicado por estudiante y control transaccional de cupo.
- Listado en mostrador indicando qué vianda pidió cada estudiante y su estado.
- Cambio de estado a `Retirada` (entrega), `Cancelada` (hasta las 12:00 hs recuperando cupo) y `No Retirada` (cierre de las 14:00 hs).

### Fuera del alcance
- Padrón o ABM de estudiantes e inicio de sesión (descartado por el cliente en Pregunta 2 y por restricciones técnicas del torneo).
- Reservas para días futuros (prohibido por el cliente en Pregunta 8).
- Aplicación web/móvil, impresión de tickets o reportes PDF (excluidos por restricciones técnicas).

---

## 7. Justificación de persistencia: ADO.NET
Elegimos **ADO.NET en modo conectado** con `Microsoft.Data.SqlClient` dentro de la capa `LaOlla.Modelo` porque permite encapsular la verificación de cupo y el `INSERT` de la reserva dentro de una misma transacción explícita (`SqlTransaction` con bloqueo de lectura `UPDLOCK, HOLDLOCK`), garantizando que nunca se supere el cupo bajo concurrencia, usando comandos 100% parametrizados (`SqlParameter`) y cerrando conexiones inmediatamente con bloques `using`.

---

## 8. Modelo de datos relacional
- **`CupoDiario`**: `Fecha (PK, DATE)`, `CupoMaximo (INT, <= 150)`.
- **`Menu`**: `Id (PK, INT IDENTITY)`, `Fecha (DATE)`, `Tipo (NVARCHAR(30))`, `Nombre (NVARCHAR(100))`, `Descripcion (NVARCHAR(250))`.
- **`Reserva`**: `Id (PK, INT IDENTITY)`, `MenuId (FK -> Menu.Id)`, `Fecha (DATE)`, `IdentificacionEstudiante (NVARCHAR(50))`, `NombreEstudiante (NVARCHAR(100))`, `HoraRegistro (DATETIME)`, `Estado (NVARCHAR(30))`.

---

## 9. Trazabilidad Arquitectura (Vista → Controlador → Modelo)
- `LaOlla.Vista` (`Form1`) solo lee entradas de pantalla, invoca a `MenuController` y `ReservaController`, y pinta el `Resultado.Mensaje`.
- `LaOlla.Controlador` evalúa las reglas `R1` a `R10` sin abrir conexiones SQL.
- `LaOlla.Modelo` ejecuta las sentencias parametrizadas leyendo la cadena desde `App.config`.

---

## 10. Datos de prueba para la Demo (Minuto 1 a 3)
1. **Vianda 1:** Tipo = `Común`, Nombre = `Milanesa con puré`, Descripción = `Milanesa de carne al horno con puré de papas`.
2. **Vianda 2:** Tipo = `Vegana`, Nombre = `Wok de vegetales y garbanzos`, Descripción = `Salteado de estación con arroz integral y garbanzos`.
3. **Reserva Válida (Hora 10:30 hs):** DNI/Legajo = `45123456`, Estudiante = `Juan Pérez`, Vianda = `Milanesa con puré` -> **Resultado:** Reserva exitosa, cupo baja de 150 a 149.
4. **Reserva Rechazada por R1 (Duplicado):** Mismo DNI `45123456` intenta reservar de nuevo hoy -> **Resultado:** Rechazado por `Regla R1`.
5. **Reserva Rechazada por R3 (Fuera de horario):** Cambiar hora a `12:30 hs`, DNI `46999888` -> **Resultado:** Rechazado por `Regla R3` (pasadas las 12 del mediodía).