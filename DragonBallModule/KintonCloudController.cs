using UnityEngine;
using System.IO;
using System.Collections;

namespace WIGU.Modules.DragonBall
{
    public class CloudController : MonoBehaviour
{
    public float gravity = 0.01f; // Gravedad suave
    public float fallSpeedLimit = 0.2f; // Velocidad máxima de caída
    public float levelSpeed = 4.0f; // Velocidad de autonivelado
    public float moveSpeed = 2.0f; // Velocidad de movimiento cuando se presiona el grip
    public float cameraReturnAcceleration = 9.81f; // Aceleración de retorno de la cámara
    public float approachSpeed = 2.0f; // Velocidad de acercamiento hacia la cámara
    public float rotateSpeed = 2.0f; // Velocidad de rotación hacia la cámara
    public float minHeight = 0.5f; // Altura mínima a la que debe estar la nube
    public float ascendSpeedFactor = 0.5f; // Factor de velocidad de ascenso (mitad de la velocidad de acercamiento)

    private Rigidbody rb;
    private Quaternion initialRotation; // Rotación inicial de la nube
    private bool isGripPressed = false;
    private bool wasGripPressed = false;
    private GameObject cameraRig;
    private float initialCameraY; // Altura inicial de la cámara
    private bool initialCameraYMeasured = false; // Para saber si se ha medido la altura inicial de la cámara
    private float cameraVelocity = 0f; // Velocidad actual de la cámara para el retorno
    private bool isApproachingCamera = false; // Indica si la nube se está acercando a la cámara
    private bool isAscending = false; // Indica si la nube está ascendiendo
    private bool hasPlayedSound = false; // Indica si el sonido ya se ha reproducido

    private AudioSource audioSource; // Referencia al AudioSource
    private string audioFilePath = "WIGUx/Sounds/Music/DBZ-Opening-Theme.ogg"; // Ruta relativa al archivo de audio

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false; // Desactivar la gravedad de Unity
        rb.isKinematic = false;

        // Guardar la rotación inicial
        initialRotation = transform.rotation;

        // Buscar el OVRCameraRig
        cameraRig = GameObject.Find("OVRCameraRig");
        if (cameraRig == null)
        {
            Debug.LogError("OVRCameraRig no encontrado.");
        }

        // Obtener el AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configurar el AudioSource
        audioSource.volume = 1.0f;
        audioSource.spatialBlend = 0.0f; // Asegurarse de que el audio no sea 3D
        audioSource.loop = false; // No hacer que el audio se repita
    }

    void Update()
    {
        // Detectar si el grip del mando derecho está siendo presionado
        DetectGrip();

        if (isAscending)
        {
            AscendToMinHeight();
        }
        else if (isApproachingCamera)
        {
            ApproachCamera();
        }
        else if (isGripPressed)
        {
            // Mover y rotar la nube en la dirección y rotación de la mano derecha
            MoveAndRotateWithHand();
            // Anclar la cámara VR a la nube
            AnchorCameraRig();
        }
        else
        {
            // Liberar el anclaje de la cámara VR
            ReleaseCameraRig();
            ApplyGravity();
            AutoLevel();
        }
    }

    void ApplyGravity()
    {
        // Aplicar una gravedad suave
        Vector3 gravityForce = new Vector3(0, -gravity, 0);
        rb.AddForce(gravityForce, ForceMode.Acceleration);

        // Limitar la velocidad de caída
        if (rb.velocity.y < -fallSpeedLimit)
        {
            rb.velocity = new Vector3(rb.velocity.x, -fallSpeedLimit, rb.velocity.z);
        }
    }

    void AutoLevel()
    {
        // Calcular la rotación objetivo (horizontal en los ejes X y Z)
        Quaternion targetRotation = Quaternion.Euler(initialRotation.eulerAngles.x, transform.rotation.eulerAngles.y, initialRotation.eulerAngles.z);

        // Interpolar suavemente hacia la rotación objetivo
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, levelSpeed * Time.deltaTime);
    }

    private void DetectGrip()
    {
        // Detectar si el grip del mando derecho está siendo presionado
        if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.Oculus)
        {
            isGripPressed = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);
        }
        else if (PlayerVRSetup.VRMode == PlayerVRSetup.VRSDK.OpenVR)
        {
            var rightController = SteamVRInput.GetController(HandType.Right);
            isGripPressed = rightController.GetPress(SteamVR_Controller.ButtonMask.Grip);
        }

        // Capturar la altura inicial de la cámara al presionar el grip por primera vez
        if (isGripPressed && !wasGripPressed && cameraRig != null && !initialCameraYMeasured)
        {
            initialCameraY = cameraRig.transform.position.y;
            initialCameraYMeasured = true;
        }

        // Si se presiona el grip y no estamos acercándonos a la cámara, iniciar el acercamiento
        if (isGripPressed && !wasGripPressed && !isApproachingCamera)
        {
            if (transform.position.y < minHeight)
            {
                isAscending = true;
            }
            else
            {
                isApproachingCamera = true;
            }
        }

        wasGripPressed = isGripPressed; // Actualizar el estado del grip para la próxima verificación
    }

    private void MoveAndRotateWithHand()
    {
        // Obtener la dirección y rotación de Hands/HandRight
        GameObject handRight = GameObject.Find("Hands/HandRight");
        if (handRight != null)
        {
            // Mover la nube en la dirección hacia adelante de la mano derecha
            Vector3 moveDirection = handRight.transform.forward;
            rb.velocity = moveDirection * moveSpeed;

            // Copiar la rotación de la mano derecha y aplicar una rotación adicional de -90 grados en el eje Y
            Quaternion handRotation = handRight.transform.rotation;
            Quaternion additionalRotation = Quaternion.Euler(0, -90, 0);
            transform.rotation = handRotation * additionalRotation;
        }
    }

    private void AnchorCameraRig()
    {
        if (cameraRig != null)
        {
            // Anclar solo la posición de la cámara a la nube sumándole la altura capturada al presionar el grip
            Vector3 newPosition = new Vector3(transform.position.x, transform.position.y - initialCameraY, transform.position.z);
            cameraRig.transform.position = newPosition;

            // Reproducir el sonido la primera vez que la cámara se ancle a la nube
            if (!hasPlayedSound)
            {
                StartCoroutine(LoadAndPlayAudio());
            }
        }
    }

    private void ReleaseCameraRig()
    {
        if (cameraRig != null)
        {
            float targetY = initialCameraY;
            float currentY = cameraRig.transform.position.y;

            // Mover la cámara hacia su altura inicial con una aceleración igual a la gravedad
            if (Mathf.Abs(currentY - targetY) > 0.01f)
            {
                float direction = (currentY > targetY) ? -1f : 1f;
                cameraVelocity += direction * cameraReturnAcceleration * Time.deltaTime;
                cameraRig.transform.position = new Vector3(cameraRig.transform.position.x, cameraRig.transform.position.y + cameraVelocity * Time.deltaTime, cameraRig.transform.position.z);

                // Si se ha pasado del objetivo, ajustar la posición final
                if ((direction == -1f && cameraRig.transform.position.y <= targetY) || (direction == 1f && cameraRig.transform.position.y >= targetY))
                {
                    cameraRig.transform.position = new Vector3(cameraRig.transform.position.x, targetY, cameraRig.transform.position.z);
                    cameraVelocity = 0f; // Resetear la velocidad
                }
            }
        }
    }

    private void AscendToMinHeight()
    {
        // Elevar la nube hasta minHeight en el eje Y a la mitad de la velocidad de acercamiento
        Vector3 targetPosition = new Vector3(transform.position.x, minHeight, transform.position.z);
        rb.velocity = Vector3.up * (approachSpeed * ascendSpeedFactor);

        if (transform.position.y >= minHeight)
        {
            // Cuando se alcanza la altura deseada, detener la elevación y comenzar a aproximarse a la cámara
            isAscending = false;
            isApproachingCamera = true;
        }
    }

    private void ApproachCamera()
    {
        if (cameraRig != null)
        {
            Vector3 targetPosition = cameraRig.transform.position;
            Vector3 direction = (targetPosition - transform.position).normalized;

            // Asegurarse de que la nube no baje de minHeight
            if (transform.position.y < minHeight)
            {
                direction.y = Mathf.Max(direction.y, 0);
            }

            rb.velocity = direction * approachSpeed;

            // Calcular la rotación necesaria para mirar hacia la cámara
            Quaternion targetRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);

            // Comprobar si la nube está lo suficientemente cerca de la cámara
            if (Vector3.Distance(transform.position, targetPosition) <= 0.5f)
            {
                // La nube ha llegado a la cámara, detener el acercamiento y activar el anclaje
                isApproachingCamera = false;
                AnchorCameraRig();
            }
        }
    }

    private IEnumerator LoadAndPlayAudio()
    {
        string path = Path.Combine(Application.dataPath, "..", audioFilePath); // Usar la ruta relativa proporcionada
        path = Path.GetFullPath(path); // Obtener la ruta completa con barras correctas

        if (!File.Exists(path))
        {
            yield break;
        }

        using (WWW www = new WWW("file://" + path))
        {
            yield return www;

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError(www.error);
            }
            else
            {
                audioSource.clip = www.GetAudioClip(false, true);
                if (audioSource.clip != null)
                {
                    audioSource.Play();
                    hasPlayedSound = true; // Marcar que el sonido ha sido reproducido
                }
            }
        }
    }
}

}