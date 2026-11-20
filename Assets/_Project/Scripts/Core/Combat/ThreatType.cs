namespace HealingKnight.Core.Combat
{
    /// <summary>Tipo de amenaza biológica. Define qué medicamento la trata (GDD 5.7).</summary>
    public enum ThreatType
    {
        Bacteria,
        Virus,
        Parasito,
        Inflamacion,
        Mucosidad,
        /// <summary>Polen y otros alérgenos (nariz). Se tratan con antihistamínico.</summary>
        Alergeno,
        /// <summary>Neurona hiperexcitada (cerebro): suelta descargas eléctricas. Se calma con anticonvulsivo.</summary>
        Hiperexcitacion,
        /// <summary>Coágulo de plaquetas y fibrina (sistema circulatorio). Solo lo deshace el anticoagulante.</summary>
        Coagulo
    }

    /// <summary>Resultado de aplicar un medicamento a una amenaza.</summary>
    public enum Effectiveness
    {
        Ineffective,
        Partial,
        Effective
    }
}
