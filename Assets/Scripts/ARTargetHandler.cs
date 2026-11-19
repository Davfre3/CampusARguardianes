using UnityEngine;
using Vuforia;

// Pon este script en cada Image Target (Vikingo, Lobo, Toro, Leon).
// IMPORTANTE: quita el componente "Default Observer Event Handler" del target,
// porque este script ya se encarga de mostrar/ocultar el contenido.
[RequireComponent(typeof(ObserverBehaviour))]
public class ARTargetHandler : MonoBehaviour
{
    [Tooltip("Debe coincidir con el id en ARGameManager: administracion, ingenieria, economia, ciencias")]
    public string facultyId = "ingenieria";

    [Tooltip("Hijo del target: la mascota 3D (o un cubo temporal)")]
    public GameObject contenido3D;

    private ObserverBehaviour observer;

    void Awake()
    {
        observer = GetComponent<ObserverBehaviour>();
    }

    void OnEnable()
    {
        observer.OnTargetStatusChanged += OnStatusChanged;
    }

    void OnDisable()
    {
        observer.OnTargetStatusChanged -= OnStatusChanged;
    }

    void Start()
    {
        if (contenido3D != null) contenido3D.SetActive(false);
    }

    private void OnStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool detectado = status.Status == Status.TRACKED ||
                         status.Status == Status.EXTENDED_TRACKED;

        if (contenido3D != null) contenido3D.SetActive(detectado);

        if (ARGameManager.Instance == null) return;

        if (detectado) ARGameManager.Instance.OnFacultyDetected(facultyId);
        else ARGameManager.Instance.OnFacultyLost(facultyId);
    }
}
