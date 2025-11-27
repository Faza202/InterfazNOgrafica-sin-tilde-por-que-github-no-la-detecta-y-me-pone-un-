using System; // pues todo
using System.Collections.Generic; //requerido para diccionarios
using System.ComponentModel; //sirve para INotifyPropertyChanged que es la base del data binding
using System.Linq; //para operaciones LINQ como Any, Contains, etc.
using System.Security.Cryptography; //para hashing SHA256
using System.Text; //para Encoding.UTF8
using System.Text.RegularExpressions; //para validación de correo con regex;
using System.Threading.Tasks; //para uso de Task y async/await;
using Microsoft.Maui.Controls; //todo lo básico de maui;

namespace MauiApp5
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private record UserData(string Contrasena, string PreguntaSeguridad, string RespuestaSeguridad);
        //almacén de usuarios en memoria por ahora
        private static readonly Dictionary<string, UserData> s_usuariosRegistrados = new();

        string _correo = string.Empty;
        string _contrasena = string.Empty;
        string _repetirContrasena = string.Empty;
        string _preguntaSeguridad = string.Empty;
        string _respuestaSeguridad = string.Empty;

        string _correoError = string.Empty;
        string _contrasenaError = string.Empty;
        string _repetirContrasenaError = string.Empty;
        string _preguntaSeguridadError = string.Empty;

        string _registroMensaje = string.Empty;
        bool _registroExitoso;

        //propiedades para Login
        string _loginCorreo = string.Empty;
        string _loginContrasena = string.Empty;
        string _loginMensaje = string.Empty;
        bool _loginExitoso;

        //control de intentos de login
        private int _intentosFallidos = 0;
        private DateTime _tiempoBloqueo = DateTime.MinValue;

        //regex básica para forma general de correo
        static readonly Regex s_emailRegex = new(@"^(?<local>[^@\s]+)@(?<domain>[A-Za-z0-9.-]+\.[A-Za-z]{2,})$", RegexOptions.Compiled);

        //dominios de proveedores comunes
        static readonly string[] s_dominiosComunes =
        {
            "gmail.com","outlook.com","hotmail.com","live.com","yahoo.com",
            "icloud.com","me.com","proton.me","protonmail.com","gmx.com","aol.com"
        };

        //TLD válidos frecuentes
        static readonly string[] s_tldsValidos =
        {
            "com","net","org","edu","gov","es","mx","ar","co","io","me"
        };

        public string Correo
        {
            get => _correo;
            set
            {
                if (_correo == value) return;
                _correo = value ?? string.Empty;
                OnPropertyChanged(nameof(Correo));
            }
        }

        public string Contrasena
        {
            get => _contrasena;
            set
            {
                if (_contrasena == value) return;
                _contrasena = value ?? string.Empty;
                OnPropertyChanged(nameof(Contrasena));
            }
        }

        public string RepetirContrasena
        {
            get => _repetirContrasena;
            set
            {
                if (_repetirContrasena == value) return;
                _repetirContrasena = value ?? string.Empty;
                OnPropertyChanged(nameof(RepetirContrasena));
            }
        }

        public string PreguntaSeguridad
        {
            get => _preguntaSeguridad;
            set
            {
                if (_preguntaSeguridad == value) return;
                _preguntaSeguridad = value ?? string.Empty;
                OnPropertyChanged(nameof(PreguntaSeguridad));
            }
        }

        public string RespuestaSeguridad
        {
            get => _respuestaSeguridad;
            set
            {
                if (_respuestaSeguridad == value) return;
                _respuestaSeguridad = value ?? string.Empty;
                OnPropertyChanged(nameof(RespuestaSeguridad));
            }
        }

        public string CorreoError
        {
            get => _correoError;
            private set
            {
                _correoError = value;
                OnPropertyChanged(nameof(CorreoError));
                _ = AnimateLabel(CorreoErrorLabel, !string.IsNullOrEmpty(value));
            }
        }

        public string ContrasenaError
        {
            get => _contrasenaError;
            private set
            {
                _contrasenaError = value;
                OnPropertyChanged(nameof(ContrasenaError));
                _ = AnimateLabel(ContrasenaErrorLabel, !string.IsNullOrEmpty(value));
            }
        }

        public string RepetirContrasenaError
        {
            get => _repetirContrasenaError;
            private set
            {
                _repetirContrasenaError = value;
                OnPropertyChanged(nameof(RepetirContrasenaError));
                _ = AnimateLabel(RepetirContrasenaErrorLabel, !string.IsNullOrEmpty(value));
            }
        }

        public string PreguntaSeguridadError
        {
            get => _preguntaSeguridadError;
            private set
            {
                _preguntaSeguridadError = value;
                OnPropertyChanged(nameof(PreguntaSeguridadError));
                _ = AnimateLabel(PreguntaSeguridadErrorLabel, !string.IsNullOrEmpty(value));
            }
        }

        public string RegistroMensaje
        {
            get => _registroMensaje;
            private set
            {
                _registroMensaje = value;
                OnPropertyChanged(nameof(RegistroMensaje));
                _ = AnimateLabel(RegistroMensajeLabel, !string.IsNullOrEmpty(value));
            }
        }

        public bool RegistroExitoso
        {
            get => _registroExitoso;
            private set
            {
                if (_registroExitoso == value) return;
                _registroExitoso = value;
                OnPropertyChanged(nameof(RegistroExitoso));
            }
        }

        //propiedades para Login
        public string LoginCorreo
        {
            get => _loginCorreo;
            set
            {
                if (_loginCorreo == value) return;
                _loginCorreo = value ?? string.Empty;
                OnPropertyChanged(nameof(LoginCorreo));
            }
        }

        public string LoginContrasena
        {
            get => _loginContrasena;
            set
            {
                if (_loginContrasena == value) return;
                _loginContrasena = value ?? string.Empty;
                OnPropertyChanged(nameof(LoginContrasena));
            }
        }

        public string LoginMensaje
        {
            get => _loginMensaje;
            private set
            {
                _loginMensaje = value;
                OnPropertyChanged(nameof(LoginMensaje));
                _ = AnimateLabel(LoginMensajeLabel, !string.IsNullOrEmpty(value));
            }
        }

        public bool LoginExitoso
        {
            get => _loginExitoso;
            private set
            {
                if (_loginExitoso == value) return;
                _loginExitoso = value;
                OnPropertyChanged(nameof(LoginExitoso));
            }
        }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        async void OnButtonPressed(object? sender, EventArgs e)
        {
            if (sender is not Button button)
                return;
            await button.ScaleTo(0.95, 100, Easing.CubicOut);
        }

        async void OnButtonReleased(object? sender, EventArgs e)
        {
            if (sender is not Button button)
                return;
            await button.ScaleTo(1.0, 100, Easing.CubicIn);
        }

        private async Task AnimateLabel(Label label, bool show)
        {
            if (label is null) return;
            //primero, siempre intenta ocultar el label si es visible
            if (label.Opacity > 0)
            {
                await label.FadeTo(0, 50, Easing.CubicOut);
            }

            //si hay un nuevo mensaje que mostrar, espera un instante y muéstralo
            if (show)
            {
                await Task.Delay(20); // Pequeño intervalo para el efecto de parpadeo
                await label.FadeTo(1, 250, Easing.CubicIn);
            }
        }

        async void OnPromtyClicked(object? sender, EventArgs e)
        {
            string result = await DisplayPromptAsync("Ayuda", "¿Qué necesita?", "Enviar", "Cancelar", "Esto está de relleno hasta que llegue promty");

            if (!string.IsNullOrWhiteSpace(result))
            {
                //solo muestra una alerta con la pregunta.
                await DisplayAlert("Pregunta enviada", $"Hemos recibido tu pregunta: '{result}'. Espere mientras promty está pensando.", "OK");
            }
        }

        void OnRegistrarClicked(object? sender, EventArgs e)
        {
            ValidarCampos();

            if (string.IsNullOrEmpty(CorreoError) && string.IsNullOrEmpty(ContrasenaError) && string.IsNullOrEmpty(RepetirContrasenaError) && string.IsNullOrEmpty(PreguntaSeguridadError))
            {
                var hashedCorreo = HashValue(Correo);
                //guardar usuario si no existe
                if (s_usuariosRegistrados.ContainsKey(hashedCorreo))
                {
                    RegistroExitoso = false;
                    RegistroMensaje = "Este correo ya está registrado.";
                }
                else
                {
                    var hashedPassword = HashValue(Contrasena);
                    var hashedRespuesta = HashValue(RespuestaSeguridad);
                    var userData = new UserData(hashedPassword, PreguntaSeguridad, hashedRespuesta);
                    s_usuariosRegistrados.Add(hashedCorreo, userData);
                    RegistroExitoso = true;
                    RegistroMensaje = "Registro exitoso";
                }
            }
            else
            {
                RegistroExitoso = false;
                RegistroMensaje = "Corrige los errores.";
            }

            SemanticScreenReader.Announce(RegistroMensaje);
        }

        async void OnLoginClicked(object? sender, EventArgs e)
        {
            // Comprobar si el usuario está bloqueado
            if (DateTime.UtcNow < _tiempoBloqueo)
            {
                var tiempoRestante = (_tiempoBloqueo - DateTime.UtcNow).TotalSeconds;
                LoginExitoso = false;
                LoginMensaje = $"Demasiados intentos. Espere {tiempoRestante:F0} segundos.";
                SemanticScreenReader.Announce(LoginMensaje);
                return;
            }

            if (string.IsNullOrWhiteSpace(LoginCorreo) || string.IsNullOrWhiteSpace(LoginContrasena))
            {
                LoginExitoso = false;
                LoginMensaje = "Correo y contraseña son requeridos.";
                SemanticScreenReader.Announce(LoginMensaje);
                return;
            }

            var hashedLoginCorreo = HashValue(LoginCorreo);
            var hashedLoginPassword = HashValue(LoginContrasena);
            if (s_usuariosRegistrados.TryGetValue(hashedLoginCorreo, out var userData) &&
                userData.Contrasena == hashedLoginPassword)
            {
                LoginExitoso = true;
                LoginMensaje = $"Bienvenido, {LoginCorreo}!";
                _intentosFallidos = 0; // Reiniciar contador en éxito
            }
            else
            {
                LoginExitoso = false;
                LoginMensaje = "Credenciales incorrectas.";
                _intentosFallidos++;

                if (_intentosFallidos >= 3)
                {
                    // A partir del 3er fallo, cada fallo posterior impone un bloqueo
                    _tiempoBloqueo = DateTime.UtcNow.AddSeconds(15);
                    LoginMensaje = $"Credenciales incorrectas. Espere 15 segundos.";
                }
            }

            SemanticScreenReader.Announce(LoginMensaje);
        }

        async void OnOlvidoContrasenaClicked(object? sender, EventArgs e)
        {
            string? correo = await DisplayPromptAsync("Recuperar Contraseña", "Introduce tu correo electrónico:", "Aceptar", "Cancelar", keyboard: Keyboard.Email);

            if (string.IsNullOrWhiteSpace(correo))
                return;

            var hashedCorreo = HashValue(correo);
            if (s_usuariosRegistrados.TryGetValue(hashedCorreo, out var userData))
            {
                string? respuesta = await DisplayPromptAsync("Pregunta de Seguridad", userData.PreguntaSeguridad, "Enviar", "Cancelar");

                if (respuesta is not null && HashValue(respuesta) == userData.RespuestaSeguridad)
                {
                    // Ya no se puede mostrar la contraseña, así que se ofrece cambiarla.
                    string? nuevaContrasena = await DisplayPromptAsync("Nueva Contraseña", "Introduce tu nueva contraseña:", "Aceptar", "Cancelar");

                    if (!string.IsNullOrWhiteSpace(nuevaContrasena))
                    {
                        var validationError = ValidarContrasena(nuevaContrasena);
                        if (string.IsNullOrEmpty(validationError))
                        {
                            var hashedNewPassword = HashValue(nuevaContrasena);
                            s_usuariosRegistrados[hashedCorreo] = userData with { Contrasena = hashedNewPassword };
                            await DisplayAlert("Éxito", "Tu contraseña ha sido cambiada.", "OK");
                        }
                        else
                        {
                            await DisplayAlert("Error", validationError, "OK");
                        }
                    }
                }
                else if (respuesta is not null) // El usuario introdujo una respuesta incorrecta
                {
                    await DisplayAlert("Error", "La respuesta de seguridad es incorrecta.", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "El correo electrónico no está registrado.", "OK");
            }
        }

        void ValidarCampos()
        {
            CorreoError = ValidarCorreo(Correo);
            ContrasenaError = ValidarContrasena(Contrasena);
            RepetirContrasenaError = string.IsNullOrEmpty(ContrasenaError) &&
                                     RepetirContrasena == Contrasena
                                     ? string.Empty
                                     : "Las contraseñas no coinciden.";
            PreguntaSeguridadError = ValidarPreguntaYRespuesta(PreguntaSeguridad, RespuestaSeguridad);
        }

        static string ValidarPreguntaYRespuesta(string pregunta, string respuesta)
        {
            if (string.IsNullOrWhiteSpace(pregunta) || string.IsNullOrWhiteSpace(respuesta))
            {
                return "La pregunta y respuesta de seguridad son requeridas.";
            }
            return string.Empty;
        }

        static string ValidarCorreo(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return "Correo requerido.";

            if (string.Equals(valor, "tu@correo.com", StringComparison.OrdinalIgnoreCase))
                return "Sea serio hombre, use el suyo.";

            var m = s_emailRegex.Match(valor);
            if (!m.Success)
                return "Formato de correo inválido.";

            string dominio = m.Groups["domain"].Value.ToLowerInvariant();

            //separar TLD
            var partes = dominio.Split('.');
            if (partes.Length < 2)
                return "Dominio inválido.";

            string tld = partes[^1];

            //TLD mal escrito común
            if (tld == "con")
                return "TLD inválido 'con'. ¿Quiso decir 'com'?";

            //validar TLD conocido (no excluye dominios corporativos: si no está, se acepta pero se revisa typo)
            bool tldConocido = s_tldsValidos.Contains(tld, StringComparer.OrdinalIgnoreCase);

            //es exactamente un dominio común aceptado
            if (s_dominiosComunes.Contains(dominio, StringComparer.OrdinalIgnoreCase))
                return string.Empty;

            //detectar posibles typos de dominio común (distancia pequeña)
            string? sugerencia = null;
            int mejor = int.MaxValue;
            foreach (var d in s_dominiosComunes)
            {
                int dist = DistanciaLevenshtein(dominio, d);
                if (dist < mejor)
                {
                    mejor = dist;
                    sugerencia = d;
                }
            }

            if (mejor <= 2)
                return $"Dominio posiblemente mal escrito. ¿Quiso decir '{sugerencia}'?";

            //Si TLD no es conocido, aún así permitir (podría ser dominio corporativo)
            //No error.
            return string.Empty;
        }

        static int DistanciaLevenshtein(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            var dp = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int costo = a[i - 1] == b[j - 1] ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + costo);
                }
            }
            return dp[a.Length, b.Length];
        }

        static string ValidarContrasena(string valor)
        {
            if (string.IsNullOrEmpty(valor))
                return "Contraseña requerida.";

            if (valor == "12345678")
                return "Para eso mejor nada.";

            bool tieneLongitud = valor.Length >= 8;
            bool tieneMayus = valor.Any(char.IsUpper);
            bool tieneMinus = valor.Any(char.IsLower);
            bool tieneNumero = valor.Any(char.IsDigit);
            bool tieneEspecial = valor.Any(c => !char.IsLetterOrDigit(c));

            if (tieneLongitud && tieneMayus && tieneMinus && tieneNumero && tieneEspecial)
                return string.Empty;

            return "Debe incluir mayúscula, minúscula, número y símbolo (mín. 8).";
        }

        static string HashValue(string value)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}