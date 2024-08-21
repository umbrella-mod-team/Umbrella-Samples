using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;

namespace WIGU.Modules.DragonBall
{
    public class BallController : WiguMonoBehaviour
    {
        private float moveInput, steerInput, rayLength, currentVelocityOffset;

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

        private TrailRenderer skidMarks;

        [Range(0, 1)] private float minPitch = 0;
        [Range(0, 1)] private float maxPitch = 0;

        [Range(1, 10)]
        private float brakingFactor = 1;
        private LayerMask derivableSurface = 9; //Walls

        // Start is called before the first frame update
        void Start()
        {
            //LogDebug(".Start");

            //var visuals = transform.Find("Visuals");

            //var bikeModel = transform.Find("BikeModel");
            //AssertIsNotNull(bikeModel, nameof(bikeModel));
          
            ////Volante 
            //var volante = bikeModel.Find("Volante");
            //AssertIsNotNull(volante, nameof(volante));

            //skidMarks = visuals.GetComponent<TrailRenderer>();
            //AssertIsNotNull(skidMarks, nameof(skidMarks));

            //var sphere = transform.Find("SphereBB");
            //AssertIsNotNull(sphere, nameof(sphere));

            //Inspect(bikeModel);
            //Inspect(sphere);

            //// Rigit body's no se guardan?
            //bikeBody = bikeModel.GetComponent<Rigidbody>();
            //AssertIsNotNull(bikeBody, nameof(bikeBody));

            //sphereRB = sphere.gameObject.GetComponent<Rigidbody>();
            ////AssertIsNotNull(sphereRB, nameof(sphereRB));

            //Destroy(GetComponent<Rigidbody>());

            //sphereRB.transform.parent = null;
            //bikeBody.transform.parent = null;

            //var sphereComponent = sphereRB.GetComponent<SphereCollider>();
            //AssertIsNotNull(sphereComponent, nameof(sphereComponent));
            //rayLength = sphereComponent.radius + 5.2f;
           
            //skidMarks.startWidth = skidWidth;
            //skidMarks.emitting = false;
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

        RaycastHit hit;
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

        void SkidMarks()
        {
            if (Grounded() && Mathf.Abs(velocity.x) > minSkidVelocity)
            {
                skidMarks.emitting = true;
            }
            else
            {
                skidMarks.emitting = false;
            }
        }
    }
}
