using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;

namespace WIGU.Modules.DragonBall
{
    public class BikeController : WiguMonoBehaviour
    {
        IWiguLogger logger = ServiceProvider.Instance.GetService<IWiguLogger>();
        RaycastHit hit;
        private float moveInput, steerInput, rayLength, currentVelocityOffset;
        private AudioSource engineSound, skidSound;

        [HideInInspector] private Vector3 velocity;

        private Rigidbody sphereRB, bikeBody;
        private float maxSpeed = 200, acceleration = 30, steerStrength = 75, tiltAngle = 30,
            gravity = 200, bikeTiltIncrement = 0.09f, zTiltAngle = 45f,
            handleRotVal = 30f, handleRotSpeed = 0.15f,
            skidWidth = 0.062f,
            minSkidVelocity = 10f, tyreRootSpeed = 10000f;


        private float maxRotationAngle = 1; // Ángulo máximo de rotación hacia la derecha o izquierda
        private float rotationSpeed = 50.0f;  // Velocidad de rotación
        private float returnSpeed = 200.0f;  // Velocidad a la que el manillar regresa a la posición central
        private float currentRotation = 0.0f; // Rotación actual del manillar

        private GameObject Handle;
        private GameObject frontTyre, backTyre;

        [Range(0, 1)] private float minPitch = 0;
        [Range(0, 1)] private float maxPitch = 0;

        [Range(1, 10)]
        private float brakingFactor = 1;
        private LayerMask derivableSurface = 9; //Walls

        // Start is called before the first frame update
        void Start()
        {
            LogDebug(".Start");

            engineSound = GetComponent<AudioSource>();
            AssertIsNotNull(engineSound, nameof(engineSound));

            var visuals = transform.Find("Visuals");
            skidSound = visuals.GetComponent<AudioSource>();
            AssertIsNotNull(skidSound, nameof(skidSound));

            var bikeModel = transform.Find("BikeModel");
            AssertIsNotNull(bikeModel, nameof(bikeModel));
          
            //Volante 
            var volante = bikeModel.Find("Volante");
            AssertIsNotNull(volante, nameof(volante));
            Handle = volante.gameObject;

            var ruedaDelantera = volante.Find("RuedaDelantera");
            AssertIsNotNull(ruedaDelantera, nameof(ruedaDelantera));
            frontTyre = ruedaDelantera.gameObject;

            var ruedaTrasera = bikeModel.Find("RuedaTrasera");
            AssertIsNotNull(ruedaTrasera, nameof(ruedaTrasera));
            backTyre = ruedaTrasera.gameObject;

            var sphere = transform.Find("SphereBB");
            AssertIsNotNull(sphere, nameof(sphere));

            Inspect(bikeModel);
            Inspect(sphere);

            // Rigit body's no se guardan?
            bikeBody = bikeModel.GetComponent<Rigidbody>();
            AssertIsNotNull(bikeBody, nameof(bikeBody));

            sphereRB = sphere.gameObject.GetComponent<Rigidbody>();
            AssertIsNotNull(sphereRB, nameof(sphereRB));

            sphereRB.transform.parent = null;
            bikeBody.transform.parent = null;

            var sphereComponent = sphereRB.GetComponent<SphereCollider>();
            AssertIsNotNull(sphereComponent, nameof(sphereComponent));
            rayLength = sphereComponent.radius + 5.2f;
           
            skidSound.mute = true;


            if (!IsMenuInstance)
            {
              
            } 
            else
            {
                engineSound.mute = true;   
            }
        }

        // Update is called once per frame
        void Update()
        {
            //moveInput = Input.GetAxis("Vertical"); // W/S o flechas arriba/abajo
            if (IsMenuInstance)
                return;

            // Reinicia el valor horizontal
            moveInput = 0.0f;

            // Detecta si las teclas izquierda o derecha están presionadas
            if (Input.GetKey(KeyCode.U) || Input.GetKey(KeyCode.W))
            {
                logger.Debug("Press U");
                moveInput = -1.0f; // Mover a la izquierda
            }
            else if (Input.GetKey(KeyCode.N) || Input.GetKey(KeyCode.S))
            {
                logger.Debug("Press N");
                moveInput = 1.0f; // Mover a la derecha
            }


            // Reinicia el valor horizontal
            steerInput = 0.0f;

            if (moveInput == -1)
            {
                // Detecta si las teclas izquierda o derecha están presionadas
                if (Input.GetKey(KeyCode.H) || Input.GetKey(KeyCode.A))
                {
                    logger.Debug("Press H");
                    steerInput = -1.0f; // Mover a la izquierda
                }
                else if (Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.D))
                {
                    logger.Debug("Press J");
                    steerInput = 1.0f; // Mover a la derecha
                }
            }
            else
            {
                // Detecta si las teclas izquierda o derecha están presionadas
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                {
                    steerInput = 1.0f; // Mover a la izquierda
                }
                else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                {
                    steerInput = -1.0f; // Mover a la derecha
                }
            }

            transform.position = sphereRB.transform.position;
            velocity = bikeBody.transform.InverseTransformDirection(bikeBody.velocity);
            currentVelocityOffset = velocity.z / maxSpeed;

            // Calcula el cambio de rotación basado en la entrada del usuario
            float rotationChange = steerInput * rotationSpeed * Time.deltaTime;

            // Calcula la nueva rotación potencial
            float newRotation = currentRotation + rotationChange;

            // Limita la rotación dentro del rango permitido
            newRotation = Mathf.Clamp(newRotation, -maxRotationAngle, maxRotationAngle);

            // Si no hay entrada, regresa el manillar a la posición central
            if (steerInput == 0.0f)
            {
                // Gradualmente devuelve el manillar a la posición central
                newRotation = Mathf.MoveTowards(newRotation, 0, returnSpeed * Time.deltaTime);
            }

            // Aplica la rotación al manillar
            Handle.transform.localEulerAngles = new Vector3(0, newRotation, 0);

            // Actualiza la rotación actual
            currentRotation = newRotation;


            // Calcula la rotación basándose en la entrada y la velocidad
            float rotationAmount = moveInput * rotationSpeed * Time.deltaTime;

            // Aplica la rotación alrededor del eje X local
            frontTyre.transform.Rotate(Vector3.right, rotationAmount, Space.Self);
            backTyre.transform.Rotate(Vector3.right, rotationAmount, Space.Self);


        }

        private void FixedUpdate()
        {
            if (IsMenuInstance)
                return;

            Movement();

            EngineSound();
        }

        void Rotation()
        {
            transform.Rotate(0, steerInput * moveInput * currentVelocityOffset * steerStrength * Time.fixedDeltaTime, 0, Space.World);

        }

        void Movement()
        {
            if (Grounded())
            {
                if (!Input.GetKey(KeyCode.Space))
                {
                    Acceleration();
                    Rotation();

                }
                Brake();
            }
            else
            {

                Gravity();
            }

            BikeTilt();
        }

        void BikeTilt()
        {
            float xRot = (Quaternion.FromToRotation(bikeBody.transform.up, hit.normal) * bikeBody.transform.rotation).eulerAngles.x;
            float zRot = 0;

            if (currentVelocityOffset > 0)
            {
                zRot = -zTiltAngle * steerInput * currentVelocityOffset;
            }

            Quaternion targetRot = Quaternion.Slerp(bikeBody.transform.rotation, Quaternion.Euler(xRot, transform.eulerAngles.y, zRot), bikeTiltIncrement);

            Quaternion newRotation = Quaternion.Euler(targetRot.eulerAngles.x, transform.eulerAngles.y, targetRot.eulerAngles.z);
            bikeBody.MoveRotation(newRotation);
        }

        void Gravity()
        {
            sphereRB.AddForce(gravity * Vector3.down, ForceMode.Acceleration);
        }

        private void Acceleration()
        {
            sphereRB.velocity = Vector3.Lerp(sphereRB.velocity, maxSpeed * moveInput * transform.forward, Time.fixedDeltaTime * acceleration);
        }

        void Brake()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                sphereRB.velocity *= brakingFactor / 10;
            }
        }

        bool Grounded()
        {
            // Dibuja el raycast en la escena para ver su dirección y longitud
            Debug.DrawRay(sphereRB.position, Vector3.down * rayLength, Color.red);

            if (Physics.Raycast(sphereRB.position, Vector3.down, out hit, rayLength, derivableSurface))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void EngineSound()
        {
            engineSound.pitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Abs(currentVelocityOffset));
        }

    }
}
