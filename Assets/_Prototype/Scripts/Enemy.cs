using UnityEngine;

public enum ThreatType { Bacteria, Virus, Parasito, Inflamacion }
public enum Effectiveness { Effective, Partial, Ineffective }

// Tabla de efectividad: sección 5.7 del GDD
public static class EffectivenessTable
{
    public static Effectiveness Get(ThreatType threat, MedicationType med)
    {
        switch (threat)
        {
            case ThreatType.Bacteria:
                if (med == MedicationType.Antibiotico) return Effectiveness.Effective;
                if (med == MedicationType.Antiinflamatorio || med == MedicationType.Antiparasitario)
                    return Effectiveness.Partial;
                return Effectiveness.Ineffective;

            case ThreatType.Virus:
                if (med == MedicationType.Antiviral) return Effectiveness.Effective;
                if (med == MedicationType.Antiinflamatorio || med == MedicationType.Antiparasitario)
                    return Effectiveness.Partial;
                return Effectiveness.Ineffective;

            case ThreatType.Parasito:
                if (med == MedicationType.Antiparasitario) return Effectiveness.Effective;
                if (med == MedicationType.Antibiotico || med == MedicationType.Antihistaminico)
                    return Effectiveness.Partial;
                return Effectiveness.Ineffective;

            case ThreatType.Inflamacion:
                return med == MedicationType.Antiinflamatorio
                    ? Effectiveness.Effective
                    : Effectiveness.Ineffective;
        }

        return Effectiveness.Ineffective;
    }
}

public class Enemy : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 30f;
    private float currentHealth;

    // Lectura pública para otros scripts (por ejemplo las fases de LombrizBoss)
    public float CurrentHealth => currentHealth;

    [Header("Tipo de amenaza (define la tabla de efectividad)")]
    public ThreatType threat = ThreatType.Parasito;

    [Header("Multiplicadores de daño")]
    [Range(0f, 1f)] public float partialDamageMultiplier = 0.25f;
    [Range(0f, 1f)] public float ineffectiveDamageMultiplier = 0f;

    [Header("Invulnerabilidad (jefes/hazards)")]
    public bool isInvulnerable = false; // usado por LombrizBoss mientras está oculta/sumergiéndose

    private SpriteRenderer sr;
    private Color baseColor;

    void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
    }

    // Devuelve el resultado para que el proyectil pueda reaccionar (rebotar, etc.)
    public Effectiveness ReceiveHit(MedicationType type, float baseDamage)
    {
        if (isInvulnerable)
        {
            Flash(Color.gray, 0.15f);
            return Effectiveness.Ineffective;
        }

        Effectiveness result = EffectivenessTable.Get(threat, type);
        float damage = 0f;

        switch (result)
        {
            case Effectiveness.Effective:
                damage = baseDamage;
                Flash(Color.white, 0.1f);
                break;

            case Effectiveness.Partial:
                damage = baseDamage * partialDamageMultiplier;
                Flash(Color.yellow, 0.1f);
                break;

            case Effectiveness.Ineffective:
                damage = baseDamage * ineffectiveDamageMultiplier;
                Flash(Color.gray, 0.15f);
                break;
        }

        Debug.Log($"{type} vs {threat}: {result}, daño {damage}, vida restante {currentHealth - damage}");

        currentHealth -= damage;

        if (currentHealth <= 0f)
            Die();

        return result;
    }

    void Flash(Color color, float duration)
    {
        if (sr == null) return;

        CancelInvoke(nameof(ResetColor));
        sr.color = color;
        Invoke(nameof(ResetColor), duration);
    }

    void ResetColor()
    {
        if (sr != null) sr.color = baseColor;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}