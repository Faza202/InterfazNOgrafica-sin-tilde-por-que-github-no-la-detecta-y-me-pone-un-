using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;

namespace MauiApp5
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        string _correo = string.Empty;
        string _contrasena = string.Empty;
        string _repetirContrasena = string.Empty;

        string _correoError = string.Empty;
        string _contrasenaError = string.Empty;
        string _repetirContrasenaError = string.Empty;

        string _registroMensaje = string.Empty;
        bool _registroExitoso;

        // Regex básica para forma general de correo
        static readonly Regex s_emailRegex = new(@"^(?<local>[^@\s]+)@(?<domain>[A-Za-z0-9.-]+\.[A-Za-z]{2,})$", RegexOptions.Compiled);

        // Dominios de proveedores comunes
        static readonly string[] s_dominiosComunes =
        {
            "gmail.com","outlook.com","hotmail.com","live.com","yahoo.com",
            "icloud.com","me.com","proton.me","protonmail.com","gmx.com","aol.com"
        };

        // TLD válidos frecuentes
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

        public string CorreoError
        {
            get => _correoError;
            private set
            {
                if (_correoError == value) return;
                _correoError = value;
                OnPropertyChanged(nameof(CorreoError));
                OnPropertyChanged(nameof(TieneCorreoError));
            }
        }

        public string ContrasenaError
        {
            get => _contrasenaError;
            private set
            {
                if (_contrasenaError == value) return;
                _contrasenaError = value;
                OnPropertyChanged(nameof(ContrasenaError));
                OnPropertyChanged(nameof(TieneContrasenaError));
            }
        }

        public string RepetirContrasenaError
        {
            get => _repetirContrasenaError;
            private set
            {
                if (_repetirContrasenaError == value) return;
                _repetirContrasenaError = value;
                OnPropertyChanged(nameof(RepetirContrasenaError));
                OnPropertyChanged(nameof(TieneRepetirContrasenaError));
            }
        }

        public bool TieneCorreoError => !string.IsNullOrEmpty(_correoError);
        public bool TieneContrasenaError => !string.IsNullOrEmpty(_contrasenaError);
        public bool TieneRepetirContrasenaError => !string.IsNullOrEmpty(_repetirContrasenaError);

        public string RegistroMensaje
        {
            get => _registroMensaje;
            private set
            {
                if (_registroMensaje == value) return;
                _registroMensaje = value;
                OnPropertyChanged(nameof(RegistroMensaje));
                OnPropertyChanged(nameof(TieneRegistroMensaje));
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

        public bool TieneRegistroMensaje => !string.IsNullOrEmpty(_registroMensaje);

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        void OnRegistrarClicked(object? sender, EventArgs e)
        {
            ValidarCampos();

            if (!TieneCorreoError && !TieneContrasenaError && !TieneRepetirContrasenaError)
            {
                RegistroExitoso = true;
                RegistroMensaje = "Registro exitoso";
            }
            else
            {
                RegistroExitoso = false;
                RegistroMensaje = "Corrige los errores.";
            }

            SemanticScreenReader.Announce(RegistroMensaje);
        }

        void ValidarCampos()
        {
            CorreoError = ValidarCorreo(Correo);
            ContrasenaError = ValidarContrasena(Contrasena);
            RepetirContrasenaError = string.IsNullOrEmpty(ContrasenaError) &&
                                     RepetirContrasena == Contrasena
                                     ? string.Empty
                                     : "Las contraseñas no coinciden.";
        }

        static string ValidarCorreo(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return "Correo requerido.";

            var m = s_emailRegex.Match(valor);
            if (!m.Success)
                return "Formato de correo inválido.";

            string dominio = m.Groups["domain"].Value.ToLowerInvariant();

            // Separar TLD
            var partes = dominio.Split('.');
            if (partes.Length < 2)
                return "Dominio inválido.";

            string tld = partes[^1];

            // TLD mal escrito común
            if (tld == "con")
                return "TLD inválido 'con'. ¿Quiso decir 'com'?";

            // Validar TLD conocido (no excluye dominios corporativos: si no está, se acepta pero se revisa typo)
            bool tldConocido = s_tldsValidos.Contains(tld, StringComparer.OrdinalIgnoreCase);

            // Si es exactamente un dominio común aceptado
            if (s_dominiosComunes.Contains(dominio, StringComparer.OrdinalIgnoreCase))
                return string.Empty;

            // Detectar posibles typos de dominio común (distancia pequeña)
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

            // Si TLD no es conocido, aún así permitir (podría ser dominio corporativo)
            // No error.
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

            bool tieneLongitud = valor.Length >= 6;
            bool tieneMayus = valor.Any(char.IsUpper);
            bool tieneMinus = valor.Any(char.IsLower);
            bool tieneNumero = valor.Any(char.IsDigit);
            bool tieneEspecial = valor.Any(c => !char.IsLetterOrDigit(c));

            if (tieneLongitud && tieneMayus && tieneMinus && tieneNumero && tieneEspecial)
                return string.Empty;

            return "Debe incluir mayúscula, minúscula, número y símbolo (mín. 6).";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
