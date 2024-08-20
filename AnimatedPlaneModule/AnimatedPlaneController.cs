using UnityEngine;

namespace WIGU.Modules.AnimatedPlane
{
    public class AnimatedPlaneController : MonoBehaviour
    {
        public float velocidad = 1f;
        public float tiempoCambioDireccionMin = 1f;
        public float tiempoCambioDireccionMax = 3f;

        private Rigidbody rb;
        private Vector3 direccion;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            ChangeRandomDirection();
            InvokeRepeating(nameof(ChangeRandomDirection), Random.Range(tiempoCambioDireccionMin, tiempoCambioDireccionMax), Random.Range(tiempoCambioDireccionMin, tiempoCambioDireccionMax));
            StartAnimation();
        }

        void StartAnimation()
        {
            Animator animator = GetComponent<Animator>();

            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                animator.SetBool("Loop", false); 
                if (stateInfo.loop)
                {
                    animator.SetFloat("Speed", 0); 
                }
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
