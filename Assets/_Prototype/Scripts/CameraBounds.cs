using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraBounds : MonoBehaviour
{
    public Transform objetivo; // normalmente el jugador
    public Vector2 minLimite;  // esquina inferior izquierda del nivel
    public Vector2 maxLimite;  // esquina superior derecha del nivel
    public float suavizado = 5f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float clampedX = Mathf.Clamp(objetivo.position.x, minLimite.x + halfWidth, maxLimite.x - halfWidth);
        float clampedY = Mathf.Clamp(objetivo.position.y, minLimite.y + halfHeight, maxLimite.y - halfHeight);

        Vector3 posicionFinal = new Vector3(clampedX, clampedY, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, posicionFinal, Time.deltaTime * suavizado);
    }
}