CREATE DATABASE LaOllaDB;
GO
USE LaOllaDB;
GO

-- Tabla para gestionar el cupo diario (modificable por el Administrador hasta 150)
CREATE TABLE CupoDiario (
    Fecha DATE PRIMARY KEY,
    CupoMaximo INT NOT NULL CHECK (CupoMaximo > 0 AND CupoMaximo <= 150)
);

-- Tabla de Viandas / Menú del día (Atributos relevados en Pregunta 11)
CREATE TABLE Menu (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Fecha DATE NOT NULL,
    Tipo NVARCHAR(30) NOT NULL, -- 'Común', 'Vegetariana', 'Vegana', 'Sin TACC'
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(250) NOT NULL
);

-- Tabla de Reservas del día (Reglas de Preguntas 1, 2, 3, 4, 6, 8, 9, 10, 12)
CREATE TABLE Reserva (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MenuId INT NOT NULL FOREIGN KEY REFERENCES Menu(Id),
    Fecha DATE NOT NULL,
    IdentificacionEstudiante NVARCHAR(50) NOT NULL, -- DNI/Legajo o identificador de autogestión
    NombreEstudiante NVARCHAR(100) NOT NULL,
    HoraRegistro DATETIME NOT NULL,
    Estado NVARCHAR(30) NOT NULL -- 'Reservada', 'Retirada', 'Cancelada', 'No Retirada'
);
GO

-- Inicializar cupo por defecto para hoy (150 viandas)
INSERT INTO CupoDiario (Fecha, CupoMaximo)
VALUES (CAST(GETDATE() AS DATE), 150);
GO