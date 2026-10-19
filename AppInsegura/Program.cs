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
            // ------------------------------------------------------------------
            // CORRECCIÓN 4.º ERROR – DEFENSA EN PROFUNDIDAD (aviso del arranque)
            // ANTES:   al arrancar se mostraba un aviso con los usuarios de
            //          prueba y sus contraseñas.
            // PELIGRO: cualquiera que abriera el programa ya sabía con qué
            //          usuario y contraseña entrar, incluso como admin.
            // AHORA:   se ha quitado ese aviso. No se da ninguna pista.
            // Ver también: CargarUsuariosDeEjemplo() (más abajo).
            // ------------------------------------------------------------------
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
                    // ----------------------------------------------------------
                    // CORRECCIÓN 9.º ERROR – FALLAR DE FORMA SEGURA (2)
                    // ANTES:   Console.WriteLine(ex.ToString()) enseñaba el error
                    //          completo: tipo de excepción, clases, métodos,
                    //          archivos y números de línea.
                    // PELIGRO: un atacante usa esa información como un "plano"
                    //          del programa para buscar puntos débiles.
                    // AHORA:   el usuario solo ve un mensaje genérico.
                    //          El detalle técnico va a Debug.WriteLine, que solo
                    //          ve el programador mientras depura (no sale por
                    //          pantalla al usuario).
                    // ----------------------------------------------------------
                    Console.WriteLine("Ha ocurrido un error inesperado.");  // Solución del error 9
                    System.Diagnostics.Debug.WriteLine(ex.ToString());  // Solución del error 9
                }

                Console.WriteLine();
            }

            Console.WriteLine("Hasta luego.");
        }

        // ----------------------------------------------------------------------
        // CORRECCIÓN 4.º ERROR – DEFENSA EN PROFUNDIDAD (cuentas de prueba)
        // ANTES:   el programa SIEMPRE creaba cuentas de prueba (admin incluida)
        //          con contraseñas conocidas escritas dentro del código.
        // PELIGRO: quien leyera el código (por ejemplo en GitHub) tenía la
        //          contraseña de admin y podía entrar con todos los permisos.
        // AHORA:   se ponen VARIAS barreras (capas), no solo una:
        //   1) #if DEBUG ... #endif → este bloque solo existe en la versión de
        //      pruebas del programador. En la versión final (Release) no se
        //      compila, así que esas cuentas ni siquiera existen.
        //   2) Las contraseñas ya no están en el código: se leen de variables
        //      de entorno (valores guardados en el ordenador, fuera del programa).
        //   3) Si la variable de entorno no existe, la cuenta no se crea.
        // Ver también: el aviso eliminado en Main().
        // ----------------------------------------------------------------------
        private static void CargarUsuariosDeEjemplo()
        {
#if DEBUG  // Solución del error 4
            string? claveAdmin = Environment.GetEnvironmentVariable("APP_ADMIN_PASSWORD");  // Solución del error 4
            string? claveAna = Environment.GetEnvironmentVariable("APP_ANA_PASSWORD");  // Solución del error 4

            if (!string.IsNullOrEmpty(claveAdmin))  // Solución del error 4
            {
                auth.RegistrarConRol("admin", claveAdmin, "admin");  // Solución del error 4
            }
            if (!string.IsNullOrEmpty(claveAna))  // Solución del error 4
            {
                auth.RegistrarConRol("ana", claveAna, "jugador");  // Solución del error 4
            }
#endif  // Solución del error 4
        }

        // ----------------------------------------------------------------------
        // CORRECCIÓN 5.º ERROR – FALLAR DE FORMA SEGURA
        // ANTES:   solo se miraba si eras admin al DIBUJAR el menú, no al
        //          EJECUTAR la acción del panel.
        // PELIGRO: si esa comprobación no se hacía, el programa dejaba pasar
        //          (fallaba "abierto", como una puerta que se abre sola cuando
        //          se va la luz).
        // AHORA:   la comprobación está centralizada en este método y devuelve
        //          true SOLO si se cumplen las DOS condiciones:
        //            - hay alguien con sesión iniciada (usuarioActual != null)
        //            - Y su rol es exactamente "admin"
        //          En cualquier otro caso (sin sesión, otro rol, dudas) → false.
        //          Esto se llama "DENEGAR POR DEFECTO": ante la duda, NO.
        // Se usa en: PanelAdministracion(), BuscarUsuario() y MostrarMenu().
        // ----------------------------------------------------------------------
        private static bool EsAdmin()  // Solución del error 5
        {
            return usuarioActual != null && usuarioActual.Rol == "admin";  // Solución del error 5
        }

        private static void MostrarMenu()
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine($"Usuario actual: {(usuarioActual != null ? usuarioActual.Nombre : "ninguno")}");
            Console.WriteLine("1. Registrar usuario");
            Console.WriteLine("2. Iniciar sesión");
            Console.WriteLine("3. Buscar usuario por nombre");
            Console.WriteLine("4. Ver mi perfil");
            // OJO: esconder la opción 5 del menú es solo comodidad, NO seguridad.
            // La protección de verdad está en PanelAdministracion() (1.er error).
            if (EsAdmin())  // Solución del error 5
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

            // Registrar() ya no deja elegir el rol: siempre crea "jugador"
            // (1.er error – ver AuthService.cs).
            Usuario nuevo = auth.Registrar(nombre, contrasena);  // Solución del error 1
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
            // ------------------------------------------------------------------
            // CORRECCIÓN 3.er ERROR – NO CONFIAR EN LA ENTRADA (2) (sesión)
            // ANTES:   cualquiera, SIN iniciar sesión, podía buscar usuarios.
            // PELIGRO: un desconocido podía ir probando nombres para descubrir
            //          qué cuentas existen.
            // AHORA:   si no has iniciado sesión, no puedes buscar.
            // ------------------------------------------------------------------
            if (usuarioActual == null)  // Solución del error 3
            {
                Console.WriteLine("Primero debes iniciar sesión.");  // Solución del error 3
                return;  // Solución del error 3
            }

            Console.Write("Nombre a buscar: ");
            string nombre = Console.ReadLine() ?? "";

            // Lo que se escribe aquí se valida dentro de BuscarPorNombre
            // (2.º error – ver BaseDatosUsuarios.cs).
            Usuario? encontrado = baseDatos.BuscarPorNombre(nombre);
            // ------------------------------------------------------------------
            // CORRECCIÓN 3.er ERROR – NO CONFIAR EN LA ENTRADA (2) (rol oculto)
            // ANTES:   el resultado mostraba SIEMPRE el rol del usuario encontrado.
            // PELIGRO: el atacante podía localizar cuál es la cuenta de
            //          administrador, que es la que más le interesa atacar.
            // AHORA:   el rol solo se muestra si quien busca es admin (EsAdmin()).
            //          Un jugador normal solo ve el nombre.
            // ------------------------------------------------------------------
            Console.WriteLine(encontrado != null
                ? (EsAdmin()  // Solución del error 3
                    ? $"Encontrado: {encontrado.Nombre} (rol: {encontrado.Rol})"  // Solución del error 3
                    : $"Encontrado: {encontrado.Nombre}")  // Solución del error 3
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
            // ------------------------------------------------------------------
            // CORRECCIÓN 7.º ERROR – NO INVENTAR CRIPTOGRAFÍA (2) (token oculto)
            // ANTES:   aquí se mostraba el token de sesión por pantalla.
            // PELIGRO: el token es como la "pulsera" que demuestra que has
            //          iniciado sesión: si alguien lo ve, puede hacerse pasar
            //          por ti.
            // AHORA:   el token ya no se muestra.
            // Ver también: AuthService.cs (IniciarSesion, GenerarTokenSesion
            //              y GuardarSesionEnDisco).
            // ------------------------------------------------------------------
        }

        private static void PanelAdministracion()
        {
            // ------------------------------------------------------------------
            // CORRECCIÓN 1.er ERROR – PRIVILEGIO MÍNIMO
            //          + 5.º ERROR – FALLAR DE FORMA SEGURA
            // ANTES:   la opción 5 solo estaba ESCONDIDA en el menú, pero si
            //          escribías "5" el panel se abría igual, aunque no hubieras
            //          iniciado sesión. Nadie comprobaba quién entraba.
            // PELIGRO: cualquiera podía ver la lista de todos los usuarios y
            //          sus roles.
            // AHORA:   justo ANTES de enseñar los datos se comprueba EsAdmin().
            //          Si no eres admin → "Acceso denegado" y se sale (return).
            // IDEA CLAVE: esconder un botón NO es proteger.
            // Ver también: Registrar() en AuthService.cs (1.er error).
            // ------------------------------------------------------------------
            if (!EsAdmin())  // Solución del error 1 y 5
            {
                Console.WriteLine("Acceso denegado.");  // Solución del error 1 y 5
                return;  // Solución del error 1 y 5
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

            // La clave del servidor se protege dentro de RedService
            // (8.º error – ver RedService.cs).
            var red = new RedService();
            red.EnviarPuntuacion(usuarioActual.Nombre, 1000);
        }
    }
}
