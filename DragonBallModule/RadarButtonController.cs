using UnityEngine;
namespace WIGU.Modules.DragonBall
{
    public class RadarButtonController : MonoBehaviour
    {
        private Vector3 originalPosition; // La posición original del objeto
        private Vector3 pressedPosition; // La posición cuando el botón está presionado
        private AudioSource audioSource; // El componente de AudioSource para reproducir el sonido

        public bool IsPlaying = false;    // Estado para verificar si el botón está presionado

        private void Start()
        {
            // Guardar la posición original
            originalPosition = transform.localPosition;

            // Definir la posición cuando está presionado, desplazando 0.7 en el eje Y
            pressedPosition = new Vector3(originalPosition.x, -27.47f, originalPosition.z);

            // Obtener el componente AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("No se encontró un AudioSource en el objeto.");
            }

            // Iniciar en la posición original
            transform.localPosition = originalPosition;
        }

        private void Update()
        {
            // Verificar si el botón está en estado presionado
            if (IsPlaying)
            {
                // Verificar si el tiempo de sonido ha terminado
                if (!audioSource.isPlaying)
                {
                    // Regresar a la posición original
                    transform.localPosition = originalPosition;

                    // Resetear el estado
                    IsPlaying = false;
                }
            }
        }

        public void Press()
        {
            if (!IsPlaying) // Asegurarse de que no se vuelva a presionar mientras ya está presionado
            {
                // Cambiar a la posición presionada
                transform.localPosition = pressedPosition;

                // Reproducir el sonido
                audioSource?.Play();

                // Establecer el estado como presionado
                IsPlaying = true;
            }
        }
    }
}