   using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AppInsegura.Modelos;

namespace AppInsegura.Datos
{
    // Simula una tabla de base de datos (equivalente a una tabla SQLite/sqflite).
    // No usa un motor real para que el proyecto compile sin dependencias externas.
    public class BaseDatosUsuarios
    {
        private readonly List<Usuario> usuarios = new List<Usuario>();

        public void Agregar(Usuario usuario)
        {
            usuarios.Add(usuario);
        }

        public List<Usuario> ListarTodos()
        {
            return usuarios;
        }

        public Usuario? BuscarExacto(string nombre)
        {
            return usuarios.FirstOrDefault(u => u.Nombre == nombre);
        }

        // CORRECCIÓN (No confiar en la entrada): antes el nombre se añadía
        // directamente a la consulta y con ' OR '1'='1 se mostraba el primer
        // usuario. Ahora la entrada se trata como un dato y no como parte de la
        // consulta: se valida con una lista blanca (solo letras, números y _,
        // de 3 a 20 caracteres) y se compara el nombre exacto, igual que haría
        // una consulta parametrizada.
        public Usuario? BuscarPorNombre(string nombreBuscado)
        {
            if (!Regex.IsMatch(nombreBuscado, "^[A-Za-z0-9_]{3,20}$"))
            {
                return null;
            }

            return BuscarExacto(nombreBuscado);
        }
    }
}
