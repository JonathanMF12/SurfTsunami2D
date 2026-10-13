using UnityEngine;

/// <summary>
/// Compone por código los temas del juego (estilo chiptune playero) para que la música
/// suene aunque no se incluya ningún archivo de audio.
///
/// No usa Assets: genera muestras PCM con síntesis aditiva (bajo triangular, melodía
/// cuadrada, pad de seno y percusión) y las mete en un <see cref="AudioClip"/>.
///
///   - <see cref="CrearTemaJuego"/>: tema principal, rápido y alegre, pensado para repetirse
///     en bucle mientras se surfea.
///   - <see cref="CrearTemaGameOver"/>: jingle corto y cómico de "perdiste" (trombón que se
///     desinfla); NO es bucle, se toca una sola vez al terminar la partida.
/// </summary>
public static class GeneradorMusica
{
    private const int Frecuencia = 44100;

    /// <summary>Alias del tema principal (compatibilidad con llamadas antiguas).</summary>
    public static AudioClip CrearTema(float bpm)
    {
        return CrearTemaJuego(bpm);
    }

    /// <summary>
    /// Tema principal del juego: rápido y alegre, sobre una progresión pop I - V - vi - IV
    /// (C - G - Am - F). Pensado para repetirse en bucle sin cortes.
    /// </summary>
    public static AudioClip CrearTemaJuego(float bpm)
    {
        bpm = Mathf.Clamp(bpm, 90f, 200f);
        float pulso = 60f / bpm;        // negra
        float corchea = pulso * 0.5f;
        const int compases = 4;
        const int pulsosPorCompas = 4;

        float duracion = compases * pulsosPorCompas * pulso;
        int total = Mathf.RoundToInt(duracion * Frecuencia);
        float[] mezcla = new float[total];

        // Progresión I - V - vi - IV  (C - G - Am - F): pop/playera y muy optimista.
        int[] bajo = { 36, 43, 45, 41 };                 // C2, G2, A2, F2
        int[][] pad = {
            new[] { 60, 64, 67 },   // C  E  G
            new[] { 59, 62, 67 },   // B  D  G
            new[] { 57, 60, 64 },   // A  C  E
            new[] { 57, 60, 65 },   // A  C  F
        };
        int[][] melodia =
        {
            new[] { 76, 79, 84, 79, 76, 79, 76, 74 }, // E  G  C  G  E  G  E  D
            new[] { 74, 79, 83, 79, 74, 79, 74, 71 }, // D  G  B  G  D  G  D  B
            new[] { 72, 76, 81, 76, 72, 76, 81, 79 }, // C  E  A  E  C  E  A  G
            new[] { 69, 72, 77, 72, 69, 72, 77, 76 }, // A  C  F  C  A  C  F  E
        };

        for (int c = 0; c < compases; c++)
        {
            float inicioCompas = c * pulsosPorCompas * pulso;

            // --- Bajo (triangular): raíz en los pulsos 1 y 3 + octava de enganche al final ---
            AgregarNota(mezcla, bajo[c], inicioCompas, pulso * 1.1f, Onda.Triangular, 0.34f, 0.01f, 0.30f, 1.2f);
            AgregarNota(mezcla, bajo[c], inicioCompas + pulso * 2f, pulso * 1.1f, Onda.Triangular, 0.30f, 0.01f, 0.30f, 1.2f);
            AgregarNota(mezcla, bajo[c] + 12, inicioCompas + pulso * 3.5f, pulso * 0.45f, Onda.Triangular, 0.22f, 0.01f, 0.40f, 1.6f);

            // --- Pad de acordes (seno suave durante todo el compás) ---
            for (int k = 0; k < pad[c].Length; k++)
            {
                AgregarNota(mezcla, pad[c][k], inicioCompas, pulsosPorCompas * pulso, Onda.Seno, 0.05f, 0.25f, 0.30f, 0.3f);
            }

            // --- Melodía (cuadrada suave) en corcheas ---
            for (int s = 0; s < 8; s++)
            {
                float t = inicioCompas + s * corchea;
                float g = (s % 4 == 0) ? 0.17f : (s % 2 == 0 ? 0.13f : 0.10f);
                AgregarNota(mezcla, melodia[c][s], t, corchea * 0.9f, Onda.Cuadrada, g, 0.02f, 0.40f, 2.0f);
            }
        }

        // --- Percusión: bombo en 1 y 3, caja en 2 y 4 y "shaker" en cada corchea ---
        var azar = new System.Random(2024);
        for (int c = 0; c < compases; c++)
        {
            float inicioCompas = c * pulsosPorCompas * pulso;
            AgregarBombo(mezcla, inicioCompas, 0.55f);
            AgregarBombo(mezcla, inicioCompas + pulso * 2f, 0.45f);
            AgregarCaja(mezcla, inicioCompas + pulso, 0.28f, azar);
            AgregarCaja(mezcla, inicioCompas + pulso * 3f, 0.28f, azar);

            for (int s = 0; s < 8; s++)
            {
                float t = inicioCompas + s * corchea;
                float g = (s % 2 == 1) ? 0.07f : 0.035f;
                AgregarRuido(mezcla, t, 0.06f, g, azar);
            }
        }

        // Mezcla final: saturación suave (evita chasquidos por recorte) y un fundido
        // muy corto en los extremos para que el bucle no suene con "clic".
        for (int i = 0; i < mezcla.Length; i++)
        {
            mezcla[i] = (float)System.Math.Tanh(mezcla[i]) * 0.9f;
        }

        int fundido = Mathf.RoundToInt(0.012f * Frecuencia);
        for (int i = 0; i < fundido && i < mezcla.Length; i++)
        {
            float f = i / (float)fundido;
            mezcla[i] *= f;
            mezcla[mezcla.Length - 1 - i] *= f;
        }

        AudioClip clip = AudioClip.Create("TemaSurfGenerado", mezcla.Length, 1, Frecuencia, false);
        clip.SetData(mezcla, 0);
        return clip;
    }

    /// <summary>
    /// Jingle cómico de "perdiste": un trombón que se desinfla (tres notas que bajan y un
    /// descenso final largo). Corto y sin bucle.
    /// </summary>
    public static AudioClip CrearTemaGameOver()
    {
        const float duracion = 3.1f;
        int total = Mathf.RoundToInt(duracion * Frecuencia);
        float[] mezcla = new float[total];

        // "Wah wah wah waaah": tres notas cortas que bajan y un descenso largo al final.
        AgregarTrombon(mezcla, 72f, 71.5f, 0.00f, 0.30f, 0.55f, 5f, 0.010f); // C5
        AgregarTrombon(mezcla, 71f, 70.5f, 0.34f, 0.30f, 0.55f, 5f, 0.010f); // B4
        AgregarTrombon(mezcla, 70f, 69.5f, 0.68f, 0.30f, 0.55f, 5f, 0.010f); // Bb4
        AgregarTrombon(mezcla, 69f, 55f, 1.05f, 1.60f, 0.50f, 5.5f, 0.035f); // A4 -> A2, largo

        // Golpe grave final para rematar la broma.
        AgregarBombo(mezcla, 2.45f, 0.45f);
        AgregarBombo(mezcla, 2.60f, 0.30f);

        // Saturación suave y fundido (entrada muy corta, salida para que no chasquee).
        for (int i = 0; i < mezcla.Length; i++)
        {
            mezcla[i] = (float)System.Math.Tanh(mezcla[i]) * 0.9f;
        }

        int fundidoIn = Mathf.RoundToInt(0.008f * Frecuencia);
        for (int i = 0; i < fundidoIn && i < mezcla.Length; i++)
        {
            mezcla[i] *= i / (float)fundidoIn;
        }

        int fundidoOut = Mathf.RoundToInt(0.12f * Frecuencia);
        for (int i = 0; i < fundidoOut && i < mezcla.Length; i++)
        {
            mezcla[mezcla.Length - 1 - i] *= i / (float)fundidoOut;
        }

        AudioClip clip = AudioClip.Create("GameOverGenerado", mezcla.Length, 1, Frecuencia, false);
        clip.SetData(mezcla, 0);
        return clip;
    }

    // ------------------------------------------------------------------
    // Síntesis
    // ------------------------------------------------------------------

    private enum Onda { Seno, Cuadrada, Triangular, Sierra }

    private static float NotaFrecuencia(int midi)
    {
        return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    }

    private static float ValorOnda(Onda onda, float fase)
    {
        switch (onda)
        {
            case Onda.Cuadrada: return fase < 0.5f ? 1f : -1f;
            case Onda.Triangular: return 4f * Mathf.Abs(fase - 0.5f) - 1f;
            case Onda.Sierra: return 2f * fase - 1f;
            default: return Mathf.Sin(fase * Mathf.PI * 2f);
        }
    }

    private static float Envolvente(float t, float ataque, float caida)
    {
        float env = 1f;
        if (ataque > 0f && t < ataque) env = t / ataque;
        if (caida > 0f && t > 1f - caida) env = Mathf.Min(env, (1f - t) / caida);
        return Mathf.Clamp01(env);
    }

    private static void AgregarNota(float[] buf, int midi, float inicioSeg, float durSeg,
                                    Onda onda, float ganancia, float ataque, float caida, float decaimiento)
    {
        int inicio = Mathf.RoundToInt(inicioSeg * Frecuencia);
        int muestras = Mathf.Max(1, Mathf.RoundToInt(durSeg * Frecuencia));
        int fin = Mathf.Min(buf.Length, inicio + muestras);
        if (inicio < 0) inicio = 0;
        if (inicio >= buf.Length) return;

        float inc = NotaFrecuencia(midi) / Frecuencia;
        float fase = 0f;

        for (int i = inicio; i < fin; i++)
        {
            float t = (i - inicio) / (float)muestras;
            float env = Envolvente(t, ataque, caida) * Mathf.Exp(-decaimiento * t);
            buf[i] += ValorOnda(onda, fase) * env * ganancia;

            fase += inc;
            if (fase >= 1f) fase -= 1f;
        }
    }

    /// <summary>
    /// Nota con "deslizamiento" de afinación y vibrato: imita un trombón (o un "wah wah").
    /// Puede empezar y terminar en notas distintas para el descenso largo del Game Over.
    /// </summary>
    private static void AgregarTrombon(float[] buf, float midiInicio, float midiFin, float inicioSeg,
                                       float durSeg, float ganancia, float vibHz, float vibCantidad)
    {
        int inicio = Mathf.RoundToInt(inicioSeg * Frecuencia);
        int muestras = Mathf.Max(1, Mathf.RoundToInt(durSeg * Frecuencia));
        int fin = Mathf.Min(buf.Length, inicio + muestras);
        if (inicio < 0) inicio = 0;
        if (inicio >= buf.Length) return;

        float fase = 0f;

        for (int i = inicio; i < fin; i++)
        {
            float t = (i - inicio) / (float)muestras;
            float tiempo = (i - inicio) / (float)Frecuencia;

            float midi = Mathf.Lerp(midiInicio, midiFin, t);
            float vibrato = 1f + vibCantidad * Mathf.Sin(Mathf.PI * 2f * vibHz * tiempo);
            float freq = 440f * Mathf.Pow(2f, (midi - 69f) / 12f) * vibrato;

            // Mezcla de sierra + cuadrada: timbre "metálico" de bronce.
            float onda = 0.55f * ValorOnda(Onda.Sierra, fase) + 0.45f * ValorOnda(Onda.Cuadrada, fase);
            float env = Envolvente(t, 0.03f, 0.35f) * Mathf.Exp(-0.5f * t);
            buf[i] += onda * env * ganancia;

            fase += freq / Frecuencia;
            if (fase >= 1f) fase -= 1f;
        }
    }

    private static void AgregarBombo(float[] buf, float inicioSeg, float ganancia)
    {
        int inicio = Mathf.RoundToInt(inicioSeg * Frecuencia);
        int muestras = Mathf.RoundToInt(0.16f * Frecuencia);
        int fin = Mathf.Min(buf.Length, inicio + muestras);
        float fase = 0f;

        for (int i = inicio; i < fin; i++)
        {
            if (i < 0) continue;

            float t = (i - inicio) / (float)muestras;
            float freq = Mathf.Lerp(150f, 55f, t);
            float env = Mathf.Exp(-6f * t);
            buf[i] += Mathf.Sin(fase * Mathf.PI * 2f) * env * ganancia;

            fase += freq / Frecuencia;
            if (fase >= 1f) fase -= 1f;
        }
    }

    /// <summary>Caja (snare): golpe de ruido con un toque de tono, típico de la percusión chiptune.</summary>
    private static void AgregarCaja(float[] buf, float inicioSeg, float ganancia, System.Random azar)
    {
        int inicio = Mathf.RoundToInt(inicioSeg * Frecuencia);
        int muestras = Mathf.RoundToInt(0.14f * Frecuencia);
        int fin = Mathf.Min(buf.Length, inicio + muestras);
        float fase = 0f;

        for (int i = inicio; i < fin; i++)
        {
            if (i < 0) continue;

            float t = (i - inicio) / (float)muestras;
            float env = Mathf.Exp(-16f * t);
            float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);
            float tono = Mathf.Sin(fase * Mathf.PI * 2f) * 0.35f;
            buf[i] += (ruido + tono) * env * ganancia;

            fase += 190f / Frecuencia;
            if (fase >= 1f) fase -= 1f;
        }
    }

    private static void AgregarRuido(float[] buf, float inicioSeg, float durSeg, float ganancia, System.Random azar)
    {
        int inicio = Mathf.RoundToInt(inicioSeg * Frecuencia);
        int muestras = Mathf.Max(1, Mathf.RoundToInt(durSeg * Frecuencia));
        int fin = Mathf.Min(buf.Length, inicio + muestras);
        float anterior = 0f;

        for (int i = inicio; i < fin; i++)
        {
            if (i < 0) continue;

            float t = (i - inicio) / (float)muestras;
            float env = Mathf.Exp(-14f * t);
            float ruido = (float)(azar.NextDouble() * 2.0 - 1.0);

            // Paso alto sencillo: suena a "shaker" y no a retumbe.
            float filtrado = ruido - anterior;
            anterior = ruido;
            buf[i] += filtrado * env * ganancia;
        }
    }
}
