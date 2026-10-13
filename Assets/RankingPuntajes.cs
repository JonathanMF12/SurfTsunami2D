using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda y lee los mejores puntajes del juego usando <see cref="PlayerPrefs"/>.
///
/// Se usa desde dos lugares:
///   - <see cref="GameManager"/> al terminar la partida (guarda el puntaje final).
///   - <see cref="MenuInicio"/> en el panel de LEADERBOARDS (los muestra).
///
/// Los puntajes quedan guardados aunque cierres el juego (máximo <see cref="Maximo"/>).
/// </summary>
public static class RankingPuntajes
{
    private const string Clave = "SurfTsunami_Ranking";

    /// <summary>Cantidad de puntajes que se conservan.</summary>
    public const int Maximo = 5;

    /// <summary>Agrega un puntaje al ranking (si es mayor que 0) y lo deja ordenado.</summary>
    public static void Guardar(int puntaje)
    {
        if (puntaje <= 0) return;

        List<int> lista = new List<int>(Obtener());
        lista.Add(puntaje);
        lista.Sort((a, b) => b.CompareTo(a)); // de mayor a menor

        while (lista.Count > Maximo) lista.RemoveAt(lista.Count - 1);

        PlayerPrefs.SetString(Clave, string.Join(",", lista.ConvertAll(v => v.ToString()).ToArray()));
        PlayerPrefs.Save();
    }

    /// <summary>Devuelve los puntajes guardados, de mayor a menor.</summary>
    public static int[] Obtener()
    {
        string guardado = PlayerPrefs.GetString(Clave, "");
        if (string.IsNullOrEmpty(guardado)) return new int[0];

        List<int> lista = new List<int>();
        foreach (string parte in guardado.Split(','))
        {
            if (int.TryParse(parte, out int valor)) lista.Add(valor);
        }

        lista.Sort((a, b) => b.CompareTo(a));
        return lista.ToArray();
    }

    /// <summary>Borra todos los puntajes (botón "BORRAR PUNTAJES" de las opciones).</summary>
    public static void Borrar()
    {
        PlayerPrefs.DeleteKey(Clave);
        PlayerPrefs.Save();
    }
}
