using LaOlla.Controlador;
using LaOlla.Modelo;

namespace LaOlla.Vista;

public partial class Form1 : Form
{
    private readonly MenuController _menuController = new MenuController();
    private readonly ReservaController _reservaController = new ReservaController();

    // Estado de la sesión actual
    private bool _esAdmin = false;
    private string _dniEstudianteActual = "";

    // Controles de Cabecera / Sesión / Cupo / Reloj
    private Label _lblSesionActual = new Label();
    private Button _btnCambiarUsuario = new Button();
    private Label _lblEstadoCupo = new Label();
    private NumericUpDown _numCupoAdmin = new NumericUpDown();
    private Button _btnActualizarCupo = new Button();
    private DateTimePicker _dtpHoraActual = new DateTimePicker();

    // Controles de Alta de Vianda (Pregunta 11)
    private GroupBox _grpVianda = new GroupBox();
    private ComboBox _cmbTipoVianda = new ComboBox();
    private TextBox _txtNombreVianda = new TextBox();
    private TextBox _txtDescripcionVianda = new TextBox();
    private Button _btnGuardarVianda = new Button();

    // Controles de Reserva (Preguntas 1, 2, 3, 8, 10)
    private GroupBox _grpReserva = new GroupBox();
    private ComboBox _cmbViandaReserva = new ComboBox();
    private TextBox _txtIdentificacion = new TextBox();
    private TextBox _txtNombreEstudiante = new TextBox();
    private DateTimePicker _dtpFechaReserva = new DateTimePicker();
    private Button _btnReservar = new Button();

    // Controles de Mostrador y Estados (Preguntas 4, 6, 9, 12)
    private GroupBox _grpMostrador = new GroupBox();
    private DataGridView _dgvReservas = new DataGridView();
    private Button _btnEntregar = new Button();
    private Button _btnCancelar = new Button();
    private Button _btnCierre14Hs = new Button();
    private Label _lblMensaje = new Label();

    public Form1()
    {
        InitializeComponent();
        ConstruirInterfaz();
        this.Load += Form1_Load;
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        MostrarPantallaLogin();
    }

    private void MostrarPantallaLogin()
    {
        using var loginForm = new LoginForm(_menuController);
        if (loginForm.ShowDialog(this) == DialogResult.OK)
        {
            _esAdmin = loginForm.EsAdministrador;
            _dniEstudianteActual = loginForm.DniEstudiante;
            ConfigurarPermisosPorRol();
            RefrescarTodo();
        }
        else
        {
            Application.Exit();
        }
    }

    private void ConfigurarPermisosPorRol()
    {
        if (_esAdmin)
        {
            _lblSesionActual.Text = "Rol activo: ADMINISTRADOR (admin)";
            _lblSesionActual.ForeColor = Color.DarkBlue;

            // El administrador puede modificar el cupo, cargar viandas, entregar y hacer el cierre
            _numCupoAdmin.Enabled = true;
            _btnActualizarCupo.Enabled = true;
            _grpVianda.Enabled = true;

            // En modo admin también puede anotar a cualquier estudiante en mostrador
            _txtIdentificacion.Text = "";
            _txtIdentificacion.ReadOnly = false;
            _txtNombreEstudiante.Text = "";

            _btnEntregar.Enabled = true;
            _btnCierre14Hs.Enabled = true;
            _grpMostrador.Text = "3. Mostrador (Administrador): Todas las Reservas del Día, Entregas y Cierre (14:00 hs)";
            MostrarResultado(Resultado.Exito("Sesión iniciada como Administrador. Gestión de cupo, carga de viandas y entrega habilitadas."));
        }
        else
        {
            _lblSesionActual.Text = $"Rol activo: ESTUDIANTE (DNI: {_dniEstudianteActual})";
            _lblSesionActual.ForeColor = Color.DarkSlateGray;

            // El estudiante NO puede modificar cupo ni cargar viandas del menú
            _numCupoAdmin.Enabled = false;
            _btnActualizarCupo.Enabled = false;
            _grpVianda.Enabled = false;

            // El estudiante se autogestiona con su propio DNI (Pregunta 2)
            _txtIdentificacion.Text = _dniEstudianteActual;
            _txtIdentificacion.ReadOnly = true;
            _txtNombreEstudiante.Text = "";

            // El estudiante no entrega viandas ni ejecuta el cierre de las 14 hs; solo puede cancelar su reserva hasta las 12 hs
            _btnEntregar.Enabled = false;
            _btnCierre14Hs.Enabled = false;
            _grpMostrador.Text = $"3. Mis Reservas de Hoy (DNI: {_dniEstudianteActual}) — Cancelación disponible hasta las 12:00 hs";
            MostrarResultado(Resultado.Exito($"Autogestión activa para DNI {_dniEstudianteActual}. Seleccione su vianda para reservar."));
        }
    }

    private void ConstruirInterfaz()
    {
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9.5F);
        this.ClientSize = new Size(1020, 715);

        // 1. Barra de Sesión y Cambio de Usuario
        var pnlSesion = new Panel { Location = new Point(15, 8), Size = new Size(985, 36), BackColor = Color.FromArgb(235, 242, 250) };
        _lblSesionActual = new Label { Location = new Point(12, 8), Size = new Size(500, 22), Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
        _btnCambiarUsuario = new Button
        {
            Text = "Cerrar Sesión / Cambiar Usuario",
            Location = new Point(760, 4),
            Size = new Size(215, 28),
            BackColor = Color.WhiteSmoke
        };
        _btnCambiarUsuario.Click += (s, e) => MostrarPantallaLogin();
        pnlSesion.Controls.AddRange(new Control[] { _lblSesionActual, _btnCambiarUsuario });

        // 2. Panel Superior: Cupo, Hora y Modificación de Cupo (Solo Admin)
        var grpCupo = new GroupBox { Text = "Estado del Cupo Diario y Reloj de Operación", Location = new Point(15, 48), Size = new Size(985, 68) };

        _lblEstadoCupo = new Label { Location = new Point(15, 26), Size = new Size(360, 28), Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = Color.DarkGreen };

        var lblHora = new Label { Text = "Hora actual:", Location = new Point(385, 29), AutoSize = true };
        _dtpHoraActual = new DateTimePicker
        {
            Location = new Point(465, 25),
            Size = new Size(90, 25),
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(10)
        };

        var lblCupoAdmin = new Label { Text = "Cupo Máx (Admin):", Location = new Point(580, 29), AutoSize = true };
        _numCupoAdmin = new NumericUpDown { Location = new Point(710, 25), Size = new Size(70, 25), Minimum = 1, Maximum = 200, Value = 150, Enabled = false };
        _btnActualizarCupo = new Button { Text = "Modificar Cupo", Location = new Point(790, 23), Size = new Size(180, 30), Enabled = false };
        _btnActualizarCupo.Click += BtnActualizarCupo_Click;

        grpCupo.Controls.AddRange(new Control[] { _lblEstadoCupo, lblHora, _dtpHoraActual, lblCupoAdmin, _numCupoAdmin, _btnActualizarCupo });

        // 3. Panel Izquierdo: Cargar Vianda del Día (Solo Administrador - Pregunta 11)
        _grpVianda = new GroupBox { Text = "1. Cargar Vianda del Día (Exclusivo Administrador)", Location = new Point(15, 122), Size = new Size(480, 195) };

        var lblTipo = new Label { Text = "Tipo de Vianda:", Location = new Point(15, 30), AutoSize = true };
        _cmbTipoVianda = new ComboBox { Location = new Point(135, 27), Size = new Size(325, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbTipoVianda.Items.AddRange(new object[] { "Común", "Vegetariana", "Vegana", "Sin TACC" });
        _cmbTipoVianda.SelectedIndex = 0;

        var lblNomV = new Label { Text = "Nombre Plato:", Location = new Point(15, 68), AutoSize = true };
        _txtNombreVianda = new TextBox { Location = new Point(135, 65), Size = new Size(325, 25) };

        var lblDescV = new Label { Text = "Descripción:", Location = new Point(15, 106), AutoSize = true };
        _txtDescripcionVianda = new TextBox { Location = new Point(135, 103), Size = new Size(325, 25) };

        _btnGuardarVianda = new Button { Text = "Guardar Vianda del Día", Location = new Point(135, 142), Size = new Size(325, 35), BackColor = Color.SteelBlue, ForeColor = Color.White };
        _btnGuardarVianda.Click += BtnGuardarVianda_Click;

        _grpVianda.Controls.AddRange(new Control[] { lblTipo, _cmbTipoVianda, lblNomV, _txtNombreVianda, lblDescV, _txtDescripcionVianda, _btnGuardarVianda });

        // 4. Panel Derecho: Registrar Reserva (Autogestión Estudiante - Preguntas 1, 2, 3, 8, 10)
        _grpReserva = new GroupBox { Text = "2. Anotar Reserva de Vianda (00:00 a 12:00 hs)", Location = new Point(510, 122), Size = new Size(490, 195) };

        var lblViandaSel = new Label { Text = "Vianda elegida:", Location = new Point(15, 30), AutoSize = true };
        _cmbViandaReserva = new ComboBox { Location = new Point(145, 27), Size = new Size(325, 25), DropDownStyle = ComboBoxStyle.DropDownList };

        var lblIdEst = new Label { Text = "DNI Estudiante:", Location = new Point(15, 65), AutoSize = true };
        _txtIdentificacion = new TextBox { Location = new Point(145, 62), Size = new Size(150, 25) };

        var lblFechaRes = new Label { Text = "Fecha:", Location = new Point(305, 65), AutoSize = true };
        _dtpFechaReserva = new DateTimePicker { Location = new Point(355, 62), Size = new Size(115, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today };

        var lblNomEst = new Label { Text = "Nombre y Ap.:", Location = new Point(15, 102), AutoSize = true };
        _txtNombreEstudiante = new TextBox { Location = new Point(145, 99), Size = new Size(325, 25) };

        _btnReservar = new Button { Text = "Registrar Reserva del Mediodía", Location = new Point(145, 142), Size = new Size(325, 35), BackColor = Color.SeaGreen, ForeColor = Color.White };
        _btnReservar.Click += BtnReservar_Click;

        _grpReserva.Controls.AddRange(new Control[] { lblViandaSel, _cmbViandaReserva, lblIdEst, _txtIdentificacion, lblFechaRes, _dtpFechaReserva, lblNomEst, _txtNombreEstudiante, _btnReservar });

        // 5. Banner de Mensajes del Controlador
        _lblMensaje = new Label
        {
            Location = new Point(15, 326),
            Size = new Size(985, 38),
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.LightYellow,
            Text = "Listo."
        };

        // 6. Panel Inferior: Gestión de Entrega y Estados de Mostrador (Preguntas 4, 6, 9, 12)
        _grpMostrador = new GroupBox { Text = "3. Reservas del Día", Location = new Point(15, 372), Size = new Size(985, 325) };

        _dgvReservas = new DataGridView
        {
            Location = new Point(15, 28),
            Size = new Size(955, 230),
            ReadOnly = true,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        _btnEntregar = new Button { Text = "Entregar Vianda (Estado -> Retirada)", Location = new Point(15, 270), Size = new Size(290, 38), BackColor = Color.DarkOliveGreen, ForeColor = Color.White };
        _btnEntregar.Click += BtnEntregar_Click;

        _btnCancelar = new Button { Text = "Cancelar Reserva (Hasta 12:00 / Recupera Cupo)", Location = new Point(320, 270), Size = new Size(330, 38), BackColor = Color.IndianRed, ForeColor = Color.White };
        _btnCancelar.Click += BtnCancelar_Click;

        _btnCierre14Hs = new Button { Text = "Cierre 14:00 hs (Pasar a 'No Retirada')", Location = new Point(665, 270), Size = new Size(305, 38), BackColor = Color.DarkSlateGray, ForeColor = Color.White };
        _btnCierre14Hs.Click += BtnCierre14Hs_Click;

        _grpMostrador.Controls.AddRange(new Control[] { _dgvReservas, _btnEntregar, _btnCancelar, _btnCierre14Hs });

        this.Controls.AddRange(new Control[] { pnlSesion, grpCupo, _grpVianda, _grpReserva, _lblMensaje, _grpMostrador });
    }

    private void MostrarResultado(Resultado res)
    {
        _lblMensaje.Text = res.Mensaje;
        _lblMensaje.BackColor = res.Ok ? Color.FromArgb(220, 255, 220) : Color.FromArgb(255, 220, 220);
        _lblMensaje.ForeColor = res.Ok ? Color.DarkGreen : Color.DarkRed;
    }

    private void RefrescarTodo()
    {
        try
        {
            int cupoMax = _menuController.ObtenerCupoMaximoHoy();
            int ocupadas = _reservaController.ObtenerCupoOcupadoHoy();
            int disponibles = _reservaController.ObtenerCupoDisponibleHoy();

            _lblEstadoCupo.Text = $"Cupo Hoy: {disponibles} disponibles (Ocupadas: {ocupadas} / Máx: {cupoMax})";
            _lblEstadoCupo.ForeColor = disponibles > 0 ? Color.DarkGreen : Color.Red;
            _numCupoAdmin.Value = cupoMax;

            var viandas = _menuController.ObtenerViandasDeHoy();
            _cmbViandaReserva.DataSource = null;
            _cmbViandaReserva.DataSource = viandas;
            _cmbViandaReserva.DisplayMember = "EtiquetaMostrador";
            _cmbViandaReserva.ValueMember = "Id";

            var reservas = _reservaController.ListarReservasDeHoy();
            if (!_esAdmin)
            {
                // El estudiante ve únicamente sus reservas para autogestionarse
                reservas = reservas
                    .Where(r => r.IdentificacionEstudiante.Equals(_dniEstudianteActual, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            _dgvReservas.DataSource = null;
            _dgvReservas.DataSource = reservas;

            if (_dgvReservas.Columns["MenuId"] != null) _dgvReservas.Columns["MenuId"].Visible = false;
        }
        catch (Exception ex)
        {
            _lblMensaje.Text = $"Atención (verificar conexión SQL en ConfiguracionBD.cs / App.config): {ex.Message}";
            _lblMensaje.BackColor = Color.MistyRose;
            _lblMensaje.ForeColor = Color.DarkRed;
        }
    }

    private void BtnGuardarVianda_Click(object? sender, EventArgs e)
    {
        string tipo = _cmbTipoVianda.SelectedItem?.ToString() ?? "";
        var res = _menuController.CargarViandaDelDia(tipo, _txtNombreVianda.Text, _txtDescripcionVianda.Text, _esAdmin);
        MostrarResultado(res);
        if (res.Ok)
        {
            _txtNombreVianda.Clear();
            _txtDescripcionVianda.Clear();
            RefrescarTodo();
        }
    }

    private void BtnActualizarCupo_Click(object? sender, EventArgs e)
    {
        var res = _menuController.ModificarCupoDiario((int)_numCupoAdmin.Value, _esAdmin);
        MostrarResultado(res);
        RefrescarTodo();
    }

    private void BtnReservar_Click(object? sender, EventArgs e)
    {
        int menuId = _cmbViandaReserva.SelectedValue is int id ? id : 0;
        TimeSpan horaOperacion = _dtpHoraActual.Value.TimeOfDay;

        string dniParaReservar = _esAdmin ? _txtIdentificacion.Text : _dniEstudianteActual;

        var res = _reservaController.Reservar(
            menuId,
            dniParaReservar,
            _txtNombreEstudiante.Text,
            _dtpFechaReserva.Value,
            horaOperacion
        );

        MostrarResultado(res);
        if (res.Ok)
        {
            if (_esAdmin) _txtIdentificacion.Clear();
            _txtNombreEstudiante.Clear();
            RefrescarTodo();
        }
    }

    private int ObtenerReservaSeleccionadaId()
    {
        if (_dgvReservas.CurrentRow?.DataBoundItem is Reserva r)
            return r.Id;
        return 0;
    }

    private void BtnEntregar_Click(object? sender, EventArgs e)
    {
        if (!_esAdmin)
        {
            MostrarResultado(Resultado.Fallo("Solo el personal de mostrador / administrador puede entregar viandas."));
            return;
        }
        int reservaId = ObtenerReservaSeleccionadaId();
        var res = _reservaController.EntregarVianda(reservaId, _dtpHoraActual.Value.TimeOfDay);
        MostrarResultado(res);
        RefrescarTodo();
    }

    private void BtnCancelar_Click(object? sender, EventArgs e)
    {
        int reservaId = ObtenerReservaSeleccionadaId();
        var res = _reservaController.CancelarReserva(reservaId, _dtpHoraActual.Value.TimeOfDay);
        MostrarResultado(res);
        RefrescarTodo();
    }

    private void BtnCierre14Hs_Click(object? sender, EventArgs e)
    {
        if (!_esAdmin)
        {
            MostrarResultado(Resultado.Fallo("Solo el personal de mostrador / administrador puede ejecutar el cierre de las 14:00 hs."));
            return;
        }
        var res = _reservaController.ProcesarCierreDeLas14Hs(_dtpHoraActual.Value.TimeOfDay);
        MostrarResultado(res);
        RefrescarTodo();
    }
}

// Ventana de Inicio de Sesión (Estudiante por DNI / Administrador con admin y admin123)
public class LoginForm : Form
{
    private readonly MenuController _menuController;

    public bool EsAdministrador { get; private set; }
    public string DniEstudiante { get; private set; } = "";

    private TextBox _txtDniEstudiante = new TextBox();
    private TextBox _txtUsuarioAdmin = new TextBox();
    private TextBox _txtClaveAdmin = new TextBox();
    private Label _lblError = new Label();

    public LoginForm(MenuController menuController)
    {
        _menuController = menuController;
        ConstruirLogin();
    }

    private void ConstruirLogin()
    {
        this.Text = "Comedor Universitario La Olla — Ingreso al Sistema";
        this.ClientSize = new Size(560, 310);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Font = new Font("Segoe UI", 9.5F);

        var lblTitulo = new Label
        {
            Text = "Seleccione su modo de ingreso a La Olla",
            Location = new Point(15, 12),
            Size = new Size(530, 26),
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Panel Izquierdo: Ingreso Estudiante (Solo DNI)
        var grpEstudiante = new GroupBox
        {
            Text = "Autogestión Estudiante",
            Location = new Point(15, 50),
            Size = new Size(255, 195)
        };

        var lblInfoEst = new Label
        {
            Text = "Ingrese únicamente su DNI para anotarse en la vianda del día:",
            Location = new Point(15, 28),
            Size = new Size(225, 40)
        };

        var lblDni = new Label { Text = "DNI:", Location = new Point(15, 78), AutoSize = true };
        _txtDniEstudiante = new TextBox { Location = new Point(15, 100), Size = new Size(225, 26) };

        var btnIngresarEst = new Button
        {
            Text = "Ingresar como Estudiante",
            Location = new Point(15, 140),
            Size = new Size(225, 36),
            BackColor = Color.SeaGreen,
            ForeColor = Color.White
        };
        btnIngresarEst.Click += BtnIngresarEst_Click;

        grpEstudiante.Controls.AddRange(new Control[] { lblInfoEst, lblDni, _txtDniEstudiante, btnIngresarEst });

        // Panel Derecho: Ingreso Administrador (admin / admin123)
        var grpAdmin = new GroupBox
        {
            Text = "Ingreso Administrador",
            Location = new Point(290, 50),
            Size = new Size(255, 195)
        };

        var lblUsu = new Label { Text = "Nombre de usuario:", Location = new Point(15, 25), AutoSize = true };
        _txtUsuarioAdmin = new TextBox { Location = new Point(15, 45), Size = new Size(225, 26), Text = "admin" };

        var lblPass = new Label { Text = "Contraseña:", Location = new Point(15, 78), AutoSize = true };
        _txtClaveAdmin = new TextBox { Location = new Point(15, 100), Size = new Size(225, 26), PasswordChar = '?' };

        var btnIngresarAdmin = new Button
        {
            Text = "Ingresar como Admin",
            Location = new Point(15, 140),
            Size = new Size(225, 36),
            BackColor = Color.SteelBlue,
            ForeColor = Color.White
        };
        btnIngresarAdmin.Click += BtnIngresarAdmin_Click;

        grpAdmin.Controls.AddRange(new Control[] { lblUsu, _txtUsuarioAdmin, lblPass, _txtClaveAdmin, btnIngresarAdmin });

        // Etiqueta inferior para errores de login
        _lblError = new Label
        {
            Location = new Point(15, 255),
            Size = new Size(530, 40),
            ForeColor = Color.DarkRed,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        this.Controls.AddRange(new Control[] { lblTitulo, grpEstudiante, grpAdmin, _lblError });
    }

    private void BtnIngresarEst_Click(object? sender, EventArgs e)
    {
        var res = _menuController.ValidarIngresoEstudiante(_txtDniEstudiante.Text);
        if (!res.Ok)
        {
            _lblError.Text = res.Mensaje;
            return;
        }

        EsAdministrador = false;
        DniEstudiante = _txtDniEstudiante.Text.Trim();
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void BtnIngresarAdmin_Click(object? sender, EventArgs e)
    {
        var res = _menuController.ValidarLoginAdmin(_txtUsuarioAdmin.Text, _txtClaveAdmin.Text);
        if (!res.Ok)
        {
            _lblError.Text = res.Mensaje;
            return;
        }

        EsAdministrador = true;
        DniEstudiante = "";
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}

