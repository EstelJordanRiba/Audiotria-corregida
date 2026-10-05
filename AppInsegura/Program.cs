using System;
using AppInsegura.Datos;
using AppInsegura.Modelos;
using AppInsegura.Servicios;

namespace AppInsegura
{
    public class Program
    {
        private static readonly BaseDatosUsuarios baseDatos = new BaseDatosUsuarios();
        private static readonly AuthService auth = new AuthService(baseDatos);
        private static Usuario? usuarioActual = null;

        public static void Main(string[] args)
        {
            CargarUsuariosDeEjemplo();

            Console.WriteLine("=== Gestor de Usuarios y Partidas ===");
            // CORRECCIÓN (Defensa en profundidad): se elimina el aviso que
            // mostraba al arrancar los usuarios de prueba y sus contraseñas.
            Console.WriteLine();

            bool salir = false;
            while (!salir)
            {
                MostrarMenu();
                string opcion = Console.ReadLine() ?? "";

                try
                {
                    switch (opcion)
                    {
                        case "1":
                            Registrar();
                            break;
                        case "2":
                            IniciarSesion();
                            break;
                        case "3":
                            BuscarUsuario();
                            break;
                        case "4":
                            VerPerfil();
                            break;
                        case "5":
                            PanelAdministracion();
                            break;
                        case "6":
                            SincronizarConServidor();
                            break;
                        case "0":
                            salir = true;
                            break;
                        default:
                            Console.WriteLine("Opción no válida.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // CORRECCIÓN (Fallar de forma segura 2): el usuario solo ve un
                    // mensaje genérico; el detalle del error (ex.ToString()) se
                    // envía únicamente a la traza de depuración.
                    Console.WriteLine("Ha ocurrido un error inesperado.");
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }

                Console.WriteLine();
            }

            Console.WriteLine("Hasta luego.");
        }

        // CORRECCIÓN (Defensa en profundidad): las cuentas de prueba solo se
        // crean en la versión de depuración (#if DEBUG) y sus contraseñas se
        // leen de variables de entorno, no del código.
        private static void CargarUsuariosDeEjemplo()
        {
#if DEBUG
            string? claveAdmin = Environment.GetEnvironmentVariable("APP_ADMIN_PASSWORD");
            string? claveAna = Environment.GetEnvironmentVariable("APP_ANA_PASSWORD");

            if (!string.IsNullOrEmpty(claveAdmin))
            {
                auth.RegistrarConRol("admin", claveAdmin, "admin");
            }
            if (!string.IsNullOrEmpty(claveAna))
            {
                auth.RegistrarConRol("ana", claveAna, "jugador");
            }
#endif
        }

        // CORRECCIÓN (Fallar de forma segura): la comprobación de rol se
        // centraliza aquí. Si no hay sesión o no se puede confirmar que el rol
        // es "admin", se deniega (denegar por defecto).
        private static bool EsAdmin()
        {
            return usuarioActual != null && usuarioActual.Rol == "admin";
        }

        private static void MostrarMenu()
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine($"Usuario actual: {(usuarioActual != null ? usuarioActual.Nombre : "ninguno")}");
            Console.WriteLine("1. Registrar usuario");
            Console.WriteLine("2. Iniciar sesión");
            Console.WriteLine("3. Buscar usuario por nombre");
            Console.WriteLine("4. Ver mi perfil");
            if (EsAdmin())
            {
                Console.WriteLine("5. Panel de administración");
            }
            Console.WriteLine("6. Sincronizar partida con el servidor");
            Console.WriteLine("0. Salir");
            Console.Write("Elige una opción: ");
        }

        private static void Registrar()
        {
            Console.Write("Nombre de usuario: ");
            string nombre = Console.ReadLine() ?? "";
            Console.Write("Contraseña: ");
            string contrasena = Console.ReadLine() ?? "";

            Usuario nuevo = auth.Registrar(nombre, contrasena);
            Console.WriteLine($"Usuario '{nuevo.Nombre}' registrado con rol '{nuevo.Rol}'.");
        }

        private static void IniciarSesion()
        {
            Console.Write("Nombre de usuario: ");
            string nombre = Console.ReadLine() ?? "";
            Console.Write("Contraseña: ");
            string contrasena = Console.ReadLine() ?? "";

            Usuario? usuario = auth.IniciarSesion(nombre, contrasena);
            if (usuario == null)
            {
                Console.WriteLine("Usuario o contraseña incorrectos.");
                return;
            }

            usuarioActual = usuario;
            Console.WriteLine($"Bienvenido, {usuario.Nombre}.");
        }

        private static void BuscarUsuario()
        {
            // CORRECCIÓN (No confiar en la entrada 2): para buscar usuarios hay
            // que haber iniciado sesión.
            if (usuarioActual == null)
            {
                Console.WriteLine("Primero debes iniciar sesión.");
                return;
            }

            Console.Write("Nombre a buscar: ");
            string nombre = Console.ReadLine() ?? "";

            Usuario? encontrado = baseDatos.BuscarPorNombre(nombre);
            // CORRECCIÓN (No confiar en la entrada 2): el rol del usuario
            // encontrado solo se muestra si quien busca es administrador.
            Console.WriteLine(encontrado != null
                ? (EsAdmin()
                    ? $"Encontrado: {encontrado.Nombre} (rol: {encontrado.Rol})"
                    : $"Encontrado: {encontrado.Nombre}")
                : "No se ha encontrado ningún usuario con ese nombre.");
        }

        private static void VerPerfil()
        {
            if (usuarioActual == null)
            {
                Console.WriteLine("Primero debes iniciar sesión.");
                return;
            }

            Console.WriteLine($"Nombre: {usuarioActual.Nombre}");
            Console.WriteLine($"Rol: {usuarioActual.Rol}");
            // CORRECCIÓN (No inventar criptografía 2): el token de sesión ya no
            // se muestra por pantalla.
        }

        private static void PanelAdministracion()
        {
            // CORRECCIÓN (Privilegio mínimo / Fallar de forma segura): se
            // comprueba que el usuario es administrador justo antes de mostrar
            // los datos, no solo al dibujar el menú. Si no lo es, se deniega.
            if (!EsAdmin())
            {
                Console.WriteLine("Acceso denegado.");
                return;
            }

            Console.WriteLine("=== PANEL DE ADMINISTRACIÓN ===");
            Console.WriteLine("Lista de usuarios registrados:");
            foreach (Usuario u in baseDatos.ListarTodos())
            {
                Console.WriteLine($" - {u.Nombre} ({u.Rol})");
            }
        }

        private static void SincronizarConServidor()
        {
            if (usuarioActual == null)
            {
                Console.WriteLine("Primero debes iniciar sesión.");
                return;
            }

            var red = new RedService();
            red.EnviarPuntuacion(usuarioActual.Nombre, 1000);
        }
    }
}
