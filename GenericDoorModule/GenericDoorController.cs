using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace WIGU.Modules.GenericDoor
{
    public class GenericDoorController : MonoBehaviour
    {
        public string nextSceneName = "Scene2"; // Nombre de la siguiente escena
        GameObject doorWing;
        Camera lol;

        Quaternion originalRotation;

        public void Start()
        {
            doorWing = transform.Find("doorWing").gameObject;
            // Buscamos la cámara en los GameObjects
            //var cameras = GameObject.FindGameObjectWithTag("MainCamera");
            //lol = cameras.GetComponent<Camera>();

            // Asegurarse de que la rotación final sea exacta
            originalRotation = doorWing.transform.rotation;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!animacionEnProgreso)
                    StartCoroutine(RotarDuranteDosSegundos());
            }
        }

        //void Normal()
        //{
        //    lol.backgroundColor = Color.black;
        //    lol.clearFlags = CameraClearFlags.Skybox;
        //}

        //void Oscurecer()
        //{
        //    lol.backgroundColor = Color.black;
        //    lol.clearFlags = CameraClearFlags.SolidColor;
        //}

        bool animacionEnProgreso = false;
        // Corrutina para la animación de rotación durante 2 segundos
        IEnumerator RotarDuranteDosSegundos()
        {
            // Marcar la animación como en progreso
            animacionEnProgreso = true;

            //Oscurecer();
            float anguloInicial = doorWing.transform.rotation.eulerAngles.y;
            float anguloFinal = anguloInicial + 90f; // Rotación completa (360 grados)

            float tiempoInicio = Time.time;

            doorWing.transform.rotation = originalRotation;


            while (Time.time - tiempoInicio < 2f)
            {
                // Calcular la rotación actual en función del tiempo
                float tiempoPasado = Time.time - tiempoInicio;
                float fraccionCompleta = tiempoPasado / 2f; // Duración de la animación: 2 segundos
                float anguloActual = Mathf.Lerp(anguloInicial, anguloFinal, fraccionCompleta);

                // Aplicar la rotación al objeto
                doorWing.transform.rotation = Quaternion.Euler(0f, anguloActual, 0f);

                yield return null;
            }

            // Asegurarse de que la rotación final sea exacta
            doorWing.transform.rotation = Quaternion.Euler(0f, anguloFinal, 0f);

            //Normal();
            // Marcar la animación como completada
            animacionEnProgreso = false;
        }

        IEnumerator LoadNextSceneAsync()
        {
            // Cargar la siguiente escena asíncronamente
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
            asyncLoad.allowSceneActivation = false;

            // Esperar a que la carga de la siguiente escena esté casi completa
            while (!asyncLoad.isDone)
            {
                if (asyncLoad.progress >= 0.9f)
                    break;

                yield return null;
            }

            // Esperar un breve momento antes de mostrar la escena de carga
            yield return new WaitForSeconds(1.0f);

            // Permitir que la carga de la siguiente escena se complete
            asyncLoad.allowSceneActivation = true;

            // Destruir la pantalla negra
            Destroy(GameObject.Find("BlackScreen"));
        }
    }
}
