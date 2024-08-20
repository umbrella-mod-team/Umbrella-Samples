using UnityEngine;

namespace WIGUx.Modules.Zombie
{

    public class ZombieController : MonoBehaviour
    {
        public float velocidadMovimiento = 1.0f;
        public float tiempoMinAndando = 2.0f; // Tiempo mínimo para andar
        public float tiempoMaxAndando = 6.0f; // Tiempo máximo para andar
        public float tiempoDeEspera = 3.0f;    // Tiempo de espera después de andar
        private CharacterController characterController;
        private Vector3 movimiento = Vector3.zero;
        private enum Estado { Andando, Parado, Esperando };
        private Estado estadoActual = Estado.Andando;
        private float tiempoInicioEstado;
        private float tiempoDeAndar;

       // private Animation animacion;
        private Rigidbody rb;
        private Vector3 direccion;

        public float velocidad = 1f;
        public float tiempoCambioDireccionMin = 1f;
        public float tiempoCambioDireccionMax = 3f;

        void Start()
        {
           // animacion = GetComponent<Animation>();
            characterController = GetComponent<CharacterController>();

            StartAnimation();
            
            rb = GetComponent<Rigidbody>();
            BoxCollider c = GetComponent<BoxCollider>();
            c.center += new Vector3(0, 0, 0.2f);
            c.size += new Vector3(0, 0, 0.2f);
            ChangeRandomDirection();
            InvokeRepeating(nameof(ChangeRandomDirection), Random.Range(tiempoCambioDireccionMin, tiempoCambioDireccionMax), Random.Range(tiempoCambioDireccionMin, tiempoCambioDireccionMax));
        }

        void StartAnimation()
        {
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("walk");
            }
        }

        void FixedUpdate()
        {
            rb.MovePosition(rb.position + direccion * velocidad * Time.fixedDeltaTime);
        }

        void ChangeRandomDirection()
        {
            direccion = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            rb.MoveRotation(Quaternion.LookRotation(direccion));
        }

        void OnCollisionEnter(Collision collision)
        {
            ChangeRandomDirection();
        }
    }
}
