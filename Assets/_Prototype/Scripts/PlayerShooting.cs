using UnityEngine;
using TMPro;

public class PlayerShooting : MonoBehaviour
{
    public Transform firePoint;
    public GameObject projectilePrefab;
    public float fireCooldown = 0.3f;

    public MedicationType currentMedication = MedicationType.Antibiotico;

    [Header("UI")]
    public TextMeshProUGUI medicationLabel;

    private float cooldownTimer;
    private SpriteRenderer sr;
    private Camera cam;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;
    }

    void Start()
    {
        UpdateLabel();
    }

    void Update()
    {
        AimAtMouse();

        cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.E) && cooldownTimer <= 0f)
        {
            Shoot();
            cooldownTimer = fireCooldown;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SetMedication(MedicationType.Antibiotico);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetMedication(MedicationType.Antiviral);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetMedication(MedicationType.Antiinflamatorio);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetMedication(MedicationType.Antihistaminico);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetMedication(MedicationType.Antiparasitario);
    }

    void AimAtMouse()
    {
        if (firePoint == null || cam == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 dir = (Vector2)(mouseWorld - firePoint.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(0f, 0f, angle);

        // El cuerpo solo se voltea izquierda/derecha; el FirePoint es el que apunta en 360°
        if (sr != null)
        {
            sr.flipX = dir.x < 0f;
        }
    }

    void SetMedication(MedicationType type)
    {
        currentMedication = type;
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (medicationLabel == null) return;
        medicationLabel.text = "Medicamento: " + currentMedication;
        medicationLabel.color = GetColorForMedication(currentMedication);
    }

    public static Color GetColorForMedication(MedicationType type)
    {
        switch (type)
        {
            case MedicationType.Antibiotico: return new Color(0.2f, 0.5f, 1f);      // azul
            case MedicationType.Antiviral: return new Color(0.8f, 0.9f, 1f);        // cristalino/blanco azulado
            case MedicationType.Antiinflamatorio: return new Color(1f, 0.5f, 0.7f); // rosa
            case MedicationType.Antihistaminico: return new Color(0.7f, 0.5f, 1f);  // violeta
            case MedicationType.Antiparasitario: return new Color(1f, 0.7f, 0.1f);  // ámbar
            default: return Color.white;
        }
    }

    void Shoot()
    {
        Debug.Log("Disparo t=" + Time.time.ToString("F2")); // TEMPORAL (P08): bórrala al terminar las pruebas

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile p = proj.GetComponent<Projectile>();
        p.type = currentMedication;

        SpriteRenderer projSr = proj.GetComponent<SpriteRenderer>();
        if (projSr != null) projSr.color = GetColorForMedication(currentMedication);
    }

    // Llamado por PlayerShield cuando un bloqueo justo a tiempo (parry) es exitoso
    public void RewardParry()
    {
        cooldownTimer = 0f;
    }
}