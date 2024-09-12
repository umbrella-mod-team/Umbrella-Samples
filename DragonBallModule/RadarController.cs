using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using EmuVR.InputManager;
using UnityEngine.Assertions;

namespace WIGU.Modules.DragonBall
{
    public class RadarController : WiguMonoBehaviour
    {
        private float delay = 0.5f;
        private float timer;

        private List<(Transform source, Transform circle)> balls = new List<(Transform source, Transform circle)>();

        private AudioSource audioSource;
        private AudioClip clip, click;
        private RadarButtonController buttonController;

        private Transform radarCircle;
        private Transform radarTriangle;

        private int currentScaleIndex = 0;
        private bool isInitialized = false;
        private bool isButtonActivating = false;

        readonly float[] radarScales = { 5, 11.834f, 30 };
        readonly (float mid, float far)[] distances = new (float mid, float far)[] {
             (6,13), (6,13), (6,13)
        };

        //max x -20 y-:6 y:46   
        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                Debug.Log(".Initialize isInitialized:" + isInitialized);

                if (isInitialized)
                    return;

                Debug.Log("RadarController isInitialized finished");

                // Asegúrate de que el audioSource no esté reproduciendo nada al inicio
                audioSource = GetComponent<AudioSource>();
                AssertIsNotNull(audioSource, nameof(audioSource));


                // Inicia el temporizador con la duración del clip
                timer = audioSource.clip.length;

                radarTriangle = transform.Find("Triange");
                AssertIsNotNull(radarTriangle, nameof(radarTriangle));

                radarCircle = transform.Find("Circle");
                AssertIsNotNull(radarCircle, nameof(radarCircle));

                radarCircle.gameObject.SetActive(false);

                var button = transform.Find("button");
                AssertIsNotNull(button, nameof(button));

                buttonController = button.gameObject.AddComponent<RadarButtonController>();
                isInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }

            Debug.Log("RadarController initialization finished");
        }

        private void UpdateCirclesPosition()
        {
            // Obtener la posición relativa del cubo con respecto al centro del radar
            var distance = distances[currentScaleIndex];
            float currentScale = radarScales[currentScaleIndex];
            foreach (var ball in balls)
            {
                UpdateCirclePosition(ball.source, ball.circle, distance, currentScale);
            }
        }

        public void Follow(Transform transform)
        {
            var elem = balls.FirstOrDefault(s => s.source == transform);
            if (elem != default && elem.source != null)
            {
                Debug.Log($"Follow: found destroying... {balls.Count}");
                GameObject.DestroyImmediate(elem.circle);
                this.balls.Remove(elem);
                Debug.Log($"Follow: destroyed... {balls.Count}");
            }
            else
            {
                Debug.Log($"Follow: adding..{balls.Count}");
                var triangle = GameObject.Instantiate(radarCircle, gameObject.transform);
                triangle.localPosition = radarCircle.localPosition;
                triangle.localRotation = radarCircle.localRotation;
                triangle.localScale = radarCircle.localScale;
                triangle.gameObject.SetActive(true);
                this.balls.Add((transform, triangle));
                Debug.Log($"Follow: added..{balls.Count}");
            }
        }

        private void ToggleRadarScale()
        {
            // Cambiar al siguiente índice de escala
            currentScaleIndex = (currentScaleIndex + 1) % radarScales.Length;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!isInitialized)
                return;

            if (!HandGrabber.IsGrabbed(gameObject))
                return;

            UpdateCirclesPosition();

            // handle button press action
            if (isButtonActivating)
            {
                if (!buttonController.IsPlaying)
                {
                    timer = audioSource.clip.length + delay;
                    isButtonActivating = false;
                }
                return;
            }

            var addItem = Input.GetKeyDown(KeyCode.K) || InputManager.GetButtonDown(Button.ActionLeft);
            if (addItem) {
                Debug.Log("trying to follow object: SelectedObject:" + (SelectableManager.SelectedObject?.name));
                if (SelectableManager.SelectedObject)
                    Follow(SelectableManager.SelectedObject.transform);
                else
                    Debug.Log("no object to select");
            }

            var isZoomKeyPressed = Input.GetKeyDown(KeyCode.Space) || InputManager.GetButton(Button.ActionRight);
            if (isZoomKeyPressed)
            {
                isButtonActivating = true;
                buttonController.Press();
                ToggleRadarScale();
                return;
            }

            if (balls.Count == 0)
                return;

            // default ball detection only if there's any active ball
            var allBallsDisabled = balls.All(s => !s.circle.gameObject.activeSelf);
            if (allBallsDisabled)
                return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                audioSource.Play();
                timer = audioSource.clip.length + delay;
            }
        }

        private void UpdateCirclePosition(Transform cube, Transform circle, (float mid, float far) distance, float currentScale)
        {
            Vector3 relativePosition = (cube.position - radarTriangle.transform.position);
            var x = relativePosition.x * currentScale;
            var y = relativePosition.z * currentScale;

            var differenceX = System.Math.Max(x, radarTriangle.localPosition.x) - System.Math.Min(x, radarTriangle.localPosition.x);
            var differenceY = System.Math.Max(y, radarTriangle.localPosition.y) - System.Math.Min(y, radarTriangle.localPosition.y);
            Debug.Log($"cube:{cube.gameObject.name} circle:{circle.gameObject.name} differenceX:{differenceX} differenceY:{differenceY}");

            var minValue = System.Math.Max(differenceX, differenceY);
            if (minValue < distance.mid)
            {
                delay = 0.3f;
            }
            else if (minValue >= distance.mid && minValue < distance.far)
            {
                delay = 1;
            }
            else
            {
                delay = 1.75f;
            }

            //x:2.1  Y:25.6     Z:11.8
            var currentPosition = circle.localPosition;

            // Actualizar la posición del círculo en el radar
            circle.localPosition = new Vector3(x, y, currentPosition.z);

            var active = !(x < -20 || x > 20 || y < -20 || y > 22);
            //calculamos direccion
            circle.gameObject.SetActive(active);
        }
    }
}