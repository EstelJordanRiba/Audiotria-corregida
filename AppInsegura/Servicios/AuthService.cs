using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AppInsegura.Datos;
using AppInsegura.Modelos;

namespace AppInsegura.Servicios
{
    public class AuthService
    {
        private readonly BaseDatosUsuarios baseDatos;

        public AuthService(BaseDatosUsuarios baseDatos)
        {
            this.baseDatos = baseDatos;
        }

        // ----------------------------------------------------------------------
        // CORRECCIÓN 1.er ERROR – PRIVILEGIO MÍNIMO (registro)
        // ANTES:   el registro público podía recibir el rol, así que alguien
        //          podía registrarse directamente como "admin".
        // PELIGRO: cualquier persona se daba a sí misma todos los permisos.
        // AHORA:   Registrar() ya no deja elegir rol: TODAS las cuentas nuevas
        //          son "jugador", que es el mínimo de permisos necesario.
        // Ver también: PanelAdministracion() en Program.cs.
        // ----------------------------------------------------------------------
        public Usuario Registrar(string nombre, string contrasena)  // Solución del error 1
        {
            return RegistrarConRol(nombre, contrasena, "jugador");  // Solución del error 1
        }

        // RegistrarConRol es "internal": no es público, solo se puede usar desde
        // dentro del propio programa. Solo lo usa CargarUsuariosDeEjemplo() en
        // Program.cs para las cuentas de prueba (#if DEBUG – 4.º error).
        internal Usuario RegistrarConRol(string nombre, string contrasena, string rol)  // Solución del error 1
        {
            var nuevo = new Usuario
            {
                Nombre = nombre,
                // La contraseña NO se guarda: se guarda su hash con una sal
                // aleatoria nueva de 16 bytes (6.º error – ver CalcularHash).
                ContrasenaHash = CalcularHash(contrasena, RandomNumberGenerator.GetBytes(16)),  // Solución del error 6
                Rol = rol,
                TokenSesion = ""
            };

            baseDatos.Agregar(nuevo);
            return nuevo;
        }

        public Usuario? IniciarSesion(string nombre, string contrasena)
        {
            Usuario? usuario = baseDatos.BuscarExacto(nombre);
            if (usuario == null)
            {
                return null;
            }

            // ------------------------------------------------------------------
            // CORRECCIÓN 6.º ERROR – NO INVENTAR CRIPTOGRAFÍA (comprobar login)
            // Cómo se comprueba la contraseña al iniciar sesión:
            //   1) Se saca la sal guardada de ese usuario (lo que va antes de ':').
            //   2) Se calcula el hash de la contraseña escrita con esa MISMA sal.
            //   3) Se comparan los dos hashes con FixedTimeEquals, que tarda
            //      SIEMPRE lo mismo. Una comparación normal se para en la primera
            //      letra distinta; midiendo ese tiempo, un atacante podría ir
            //      adivinando el hash poco a poco. Así no puede.
            // Ver también: CalcularHash() (más abajo).
            // ------------------------------------------------------------------
            byte[] sal = Convert.FromBase64String(usuario.ContrasenaHash.Split(':')[0]);  // Solución del error 6
            string hashIntento = CalcularHash(contrasena, sal);  // Solución del error 6
            if (!CryptographicOperations.FixedTimeEquals(  // Solución del error 6
                    Encoding.UTF8.GetBytes(usuario.ContrasenaHash),  // Solución del error 6
                    Encoding.UTF8.GetBytes(hashIntento)))  // Solución del error 6
            {
                return null;
            }

            // ------------------------------------------------------------------
            // CORRECCIÓN 7.º ERROR – NO INVENTAR CRIPTOGRAFÍA (2) (guardar token)
            // ANTES:   el token se guardaba tal cual (texto plano) en el usuario
            //          y se escribía en el log por pantalla.
            // PELIGRO: quien viera la consola, el log o el archivo podía copiar
            //          el token y hacerse pasar por el usuario.
            // AHORA:   en el usuario solo se guarda el HASH SHA-256 del token,
            //          y el log ya no muestra el token.
            // ¿Por qué aquí vale SHA-256 y para contraseñas no? Porque el token
            // es largo y aleatorio (imposible de adivinar); las contraseñas son
            // cortas y las inventan personas, por eso necesitan PBKDF2 (lento).
            // Ver también: GenerarTokenSesion(), GuardarSesionEnDisco() y
            //              VerPerfil() en Program.cs.
            // ------------------------------------------------------------------
            string token = GenerarTokenSesion();
            usuario.TokenSesion = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));  // Solución del error 7

            // El log solo muestra el nombre, ningún dato secreto.
            Console.WriteLine($"[LOG] Login correcto -> usuario: {usuario.Nombre}");  // Solución del error 7

            GuardarSesionEnDisco(usuario);

            return usuario;
        }

        // ----------------------------------------------------------------------
        // CORRECCIÓN 6.º ERROR – NO INVENTAR CRIPTOGRAFÍA (hash de contraseñas)
        // ANTES:   las contraseñas se guardaban con MD5 y sin sal.
        // PELIGRO: - MD5 es MUY rápido: si roban los hashes, un atacante puede
        //            probar miles de millones de contraseñas por segundo.
        //          - Sin sal, dos usuarios con la misma contraseña tienen el
        //            mismo hash, y existen tablas ya calculadas para "romperlos".
        // AHORA:   se usa PBKDF2 con SHA-256, un algoritmo PROBADO y pensado
        //          para contraseñas (no nos inventamos nada):
        //   - sal       → 16 bytes aleatorios, distintos para cada usuario
        //   - 600_000   → repite el cálculo 600.000 veces para que sea LENTO a
        //                 propósito (para ti es un instante; para un atacante que
        //                 prueba millones de contraseñas, una eternidad)
        //   - SHA256    → la función de hash que usa por dentro
        //   - 32        → el hash resultante mide 32 bytes
        // Se guarda como "sal:hash" (en Base64) para poder recuperar la sal al
        // iniciar sesión.
        // ----------------------------------------------------------------------
        private string CalcularHash(string contrasena, byte[] sal)  // Solución del error 6
        {
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, 600_000, HashAlgorithmName.SHA256, 32);  // Solución del error 6
            return $"{Convert.ToBase64String(sal)}:{Convert.ToBase64String(hash)}";  // Solución del error 6
        }

        // ----------------------------------------------------------------------
        // CORRECCIÓN 7.º ERROR – NO INVENTAR CRIPTOGRAFÍA (2) (crear token)
        // ANTES:   el token se creaba con Random y tenía solo 6 cifras.
        // PELIGRO: - Random NO es seguro: sus números se pueden predecir.
        //          - 6 cifras = solo 1.000.000 de opciones: se pueden probar todas.
        // AHORA:   RandomNumberGenerator, el generador aleatorio SEGURO de .NET
        //          (pensado para criptografía), con 32 bytes aleatorios: hay
        //          tantas combinaciones que es imposible adivinarlo.
        // ----------------------------------------------------------------------
        private string GenerarTokenSesion()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));  // Solución del error 7
        }

        // ----------------------------------------------------------------------
        // CORRECCIÓN 7.º ERROR – NO INVENTAR CRIPTOGRAFÍA (2) (archivo de sesión)
        // ANTES:   en sesion.txt se guardaba el token real en texto plano.
        // PELIGRO: cualquiera con acceso al archivo podía leerlo y usarlo.
        // AHORA:   usuario.TokenSesion ya contiene el HASH del token (ver
        //          IniciarSesion), así que en el archivo se guarda el hash y no
        //          el token real.
        // ----------------------------------------------------------------------
        private void GuardarSesionEnDisco(Usuario usuario)
        {
            File.WriteAllText("sesion.txt", $"{usuario.Nombre}:{usuario.TokenSesion}");
        }
    }
}
