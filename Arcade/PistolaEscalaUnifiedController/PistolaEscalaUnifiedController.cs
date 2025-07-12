using UnityEngine;
using WIGU;
using System.Linq;

public class PistolaEscalaUnifiedController : MonoBehaviour
{
    private GameObject currentTarget;
    private LineRenderer lineRenderer;
    private float rayDistance = 10f;
    private int currentMode = 1;
    private int incognitoCounter = 0;
    private int deformAxis = 0;
    private bool isRayActive = false;
    private bool isGunActive = false;
    private bool isHeld = false;

    private bool awaitingMinus = false;

    private float desktopVerySmallScaleFactor = 0.025f;
    private float desktopSmallScaleFactor = 0.05f;
    private float desktopLargeScaleFactor = 0.09f;
    private float desktopVeryLargeScaleFactor = 0.42f;

    private float vrVerySmallScaleFactor = 0.0005f;
    private float vrSmallScaleFactor = 0.001f;
    private float vrLargeScaleFactor = 0.005f;
    private float vrVeryLargeScaleFactor = 0.015f;

    private float desktopIncognitoScaleFactor = 0.0045f;
    private float vrIncognitoScaleFactor = 0.003f;
    private float smallScaleThreshold = 0.5f;
    private float verySmallScaleThreshold = 0.2f;
    private float largeScaleThreshold = 2.5f;

    private GameObject[] energyRings;
    private Light pointLight;
    private Renderer sphereRenderer;
    private Color[] modeColors = { Color.green, Color.yellow, Color.red, Color.white };
    private float emisivoIntensidad = 2.5f;
    private float baseWaveSpeed = 2.0f;

    private GameObject energiaEffect;
    private GameObject particulasEffect;
    private float particleBrightness = 0.6f;

    private AudioSource sonidoModo;
    private AudioSource sonidoLaser;

    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        ConfigureLaser(Color.green);
        lineRenderer.enabled = false;

        energyRings = new GameObject[]
        {
            transform.Find("Torus001")?.gameObject,
            transform.Find("Torus002")?.gameObject,
            transform.Find("Torus003")?.gameObject
        };

        var sphereObject = transform.Find("Sphere003");
        if (sphereObject != null)
        {
            pointLight = sphereObject.GetComponentInChildren<Light>();
            sphereRenderer = sphereObject.GetComponent<Renderer>();
        }

        energiaEffect = transform.Find("Sphere001/Energia")?.gameObject;
        particulasEffect = transform.Find("Sphere001/Particulas")?.gameObject;

        if (energiaEffect != null && particulasEffect != null)
        {
            energiaEffect.SetActive(false);
            particulasEffect.SetActive(false);
            UpdateParticleColor();
        }

        SetRingEmission(false);
        SetPointLightEmission(false);

        var sonidoPistola = transform.Find("SonidoPistola");
        if (sonidoPistola != null)
        {
            AudioSource[] audioSources = sonidoPistola.GetComponents<AudioSource>();
            if (audioSources.Length >= 2)
            {
                sonidoModo = audioSources[0];
                sonidoLaser = audioSources[1];

                sonidoModo.playOnAwake = false;
                sonidoModo.loop = true;

                sonidoLaser.playOnAwake = false;
                sonidoLaser.loop = true;
            }
            else
            {
                Debug.LogError("No se encontraron suficientes AudioSources en 'SonidoPistola'.");
            }
        }
    }

    void Update()
    {
        if (IsVRMode())
        {
            CheckHandGrabber();
            if (!isHeld) { ResetState(); return; }
            HandleVRInput();
        }
        else
        {
            if (PlayerControllerHelper.IsObjectGrabbed(gameObject))
            {
                if (!isHeld) isHeld = true;
                HandleDesktopInput();
            }
            else if (isHeld) { ResetState(); }
        }

        if (isGunActive) UpdateRingWaveEffect();
    }

    private void ToggleParticleEffects(bool state)
    {
        if (energiaEffect != null) energiaEffect.SetActive(state);
        if (particulasEffect != null) particulasEffect.SetActive(state);
    }

    private void UpdateParticleColor()
    {
        if (energiaEffect != null && particulasEffect != null)
        {
            var particleColor = modeColors[currentMode - 1] * particleBrightness;
            var energiaRenderer = energiaEffect.GetComponent<ParticleSystemRenderer>();
            var particulasRenderer = particulasEffect.GetComponent<ParticleSystemRenderer>();

            if (energiaRenderer != null) energiaRenderer.material.SetColor("_Color", particleColor);
            if (particulasRenderer != null) particulasRenderer.material.SetColor("_Color", particleColor);
        }
    }

    private void ConfigureLaser(Color color)
    {
        lineRenderer.startWidth = 0.007f;
        lineRenderer.endWidth = 0.007f;
        lineRenderer.material = new Material(Shader.Find("Standard"));
        lineRenderer.material.color = new Color(color.r, color.g, color.b, 0.3f);
        lineRenderer.material.SetColor("_EmissionColor", color * 0.5f);

        UpdateParticleColor();
    }

    private void UpdateRingWaveEffect()
    {
        float speedMultiplier = 1f + (currentMode - 1) * 0.75f;
        float waveTime = Time.time * baseWaveSpeed * speedMultiplier;

        foreach (var ring in energyRings)
        {
            if (ring == null) continue;
            var ringRenderer = ring.GetComponent<Renderer>();
            if (ringRenderer != null)
            {
                Color waveColor = Color.blue * Mathf.PingPong(waveTime + (ring == energyRings[0] ? 0f : 0.5f), emisivoIntensidad);
                ringRenderer.material.SetColor("_EmissionColor", waveColor);
            }
        }
    }

    private void SetRingEmission(bool state)
    {
        foreach (var ring in energyRings)
        {
            if (ring == null) continue;
            var ringRenderer = ring.GetComponent<Renderer>();
            if (ringRenderer != null)
                ringRenderer.material.SetColor("_EmissionColor", state ? Color.blue * emisivoIntensidad : Color.black);
        }
    }

    private void SetPointLightEmission(bool state)
    {
        Color colorToSet = state ? modeColors[currentMode - 1] * emisivoIntensidad : Color.black;

        if (sphereRenderer != null)
            sphereRenderer.material.SetColor("_EmissionColor", colorToSet);

        if (pointLight != null)
        {
            pointLight.enabled = state;
            if (state) pointLight.color = modeColors[currentMode - 1];
        }
    }

    private void UpdateLaserEffect()
    {
        float pulseSpeed = 1.0f + (currentMode - 1) * 0.5f;
        float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1.0f);
        lineRenderer.startWidth = Mathf.Lerp(0.0035f, 0.007f, pulse);
        lineRenderer.endWidth = Mathf.Lerp(0.0035f, 0.007f, pulse);
    }

    private void ResetState()
    {
        isHeld = false;
        isGunActive = false;
        isRayActive = false;
        lineRenderer.enabled = false;
        ToggleParticleEffects(false);
        currentTarget = null;
        incognitoCounter = 0;
        SetRingEmission(false);
        SetPointLightEmission(false);
        StopLaserAudio();
        StopModoAudio();
    }

    private void HandleDesktopInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isGunActive)
            {
                isGunActive = true;
                SetRingEmission(true);
                SetPointLightEmission(true);
                UpdateLightAndEmission();
                sonidoModo.Play();
            }
            else
            {
                isRayActive = !isRayActive;
                lineRenderer.enabled = isRayActive;
                ToggleParticleEffects(isRayActive);
                if (isRayActive) sonidoLaser.Play();
                else StopLaserAudio();
            }
        }

        if (isGunActive && Input.GetKeyDown(KeyCode.M))
        {
            SwitchMode();
            UpdateLightAndEmission();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Space))
        {
            isGunActive = false;
            isRayActive = false;
            lineRenderer.enabled = false;
            ToggleParticleEffects(false);
            SetRingEmission(false);
            SetPointLightEmission(false);
            StopLaserAudio();
            StopModoAudio();
        }

        if (isRayActive)
        {
            DetectAndPerformAction();
            UpdateLaserEffect();
        }

        CheckIncognitoSequenceDesktop();
    }

    private void CheckIncognitoSequenceDesktop()
    {
        if (awaitingMinus && Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            awaitingMinus = false;
            incognitoCounter++;
        }
        else if (!awaitingMinus && Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            awaitingMinus = true;
            incognitoCounter++;
        }
        else if ((awaitingMinus && Input.GetKeyDown(KeyCode.KeypadPlus)) || (!awaitingMinus && Input.GetKeyDown(KeyCode.KeypadMinus)))
        {
            incognitoCounter = 0;
            awaitingMinus = false;
        }

        if (incognitoCounter >= 10) ActivateIncognitoMode();
    }

    private void SwitchMode()
    {
        currentMode = (currentMode % 3) + 1;
        ConfigureLaser(modeColors[currentMode - 1]);
        UpdateLightAndEmission();
        SetModoFrequency();
    }

    private void ActivateIncognitoMode()
    {
        currentMode = 4;
        ConfigureLaser(Color.white);
        UpdateLightAndEmission();
        SetModoFrequency();
        incognitoCounter = 0;
    }

    private void UpdateLightAndEmission()
    {
        SetPointLightEmission(isGunActive);
        ConfigureLaser(modeColors[currentMode - 1]);
    }

    private void HandleVRInput()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four)) ToggleGunAndRay();

        if (isGunActive)
        {
            HandleLaserVR();
            HandleModeChangeVR();
        }
    }

    private void ToggleGunAndRay()
    {
        isGunActive = !isGunActive;
        isRayActive = false;
        lineRenderer.enabled = isGunActive;
        ToggleParticleEffects(isGunActive);
        SetRingEmission(isGunActive);
        SetPointLightEmission(isGunActive);

        if (isGunActive) sonidoModo.Play();
        else StopModoAudio();
    }

    private void HandleLaserVR()
    {
        if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger) || OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
        {
            if (!isRayActive)
            {
                isRayActive = true;
                lineRenderer.enabled = true;
                ToggleParticleEffects(true);
                sonidoLaser.Play();
            }
            DetectAndPerformAction();
            UpdateLaserEffect();
        }
        else
        {
            isRayActive = false;
            lineRenderer.enabled = false;
            ToggleParticleEffects(false);
            StopLaserAudio();
        }
    }

    private void HandleModeChangeVR()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
        {
            if (currentMode == 4) ResetToMode1();
            else { SwitchMode(); }
        }

        if (CheckIncognitoSequenceVR()) ActivateIncognitoMode();
    }

    private bool CheckIncognitoSequenceVR()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) && awaitingMinus) { incognitoCounter++; awaitingMinus = false; }
        else if (OVRInput.GetDown(OVRInput.Button.Three) && !awaitingMinus) { incognitoCounter++; awaitingMinus = true; }
        else if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three)) { incognitoCounter = 0; awaitingMinus = true; }
        return incognitoCounter >= 10;
    }

    private void ResetToMode1()
    {
        currentMode = 1;
        ConfigureLaser(Color.green);
        UpdateLightAndEmission();
        SetModoFrequency();
    }

    private void DetectAndPerformAction()
    {
        RaycastHit[] hits;
        Vector3 rayDirection = -transform.forward;
        Vector3 rayOrigin = transform.Find("Sphere001").position;

        lineRenderer.SetPosition(0, rayOrigin);
        lineRenderer.SetPosition(1, rayOrigin + rayDirection * rayDistance);

        hits = Physics.RaycastAll(rayOrigin, rayDirection, rayDistance);
        float closestDistance = rayDistance;
        GameObject detectedObject = null;

        foreach (var hit in hits)
        {
            if (hit.distance < closestDistance)
            {
                GameObject hitObject = hit.collider.gameObject;
                GameObject cloneObject = FindCloneObject(hitObject);
                if (cloneObject != null)
                {
                    detectedObject = cloneObject;
                    closestDistance = hit.distance;
                }
            }
        }

        if (detectedObject != null)
        {
            currentTarget = detectedObject;
            PerformAction();
            lineRenderer.SetPosition(1, rayOrigin + rayDirection * closestDistance);
        }
        else { currentTarget = null; }
    }

    private GameObject FindCloneObject(GameObject obj)
    {
        while (obj != null)
        {
            if (obj.name.IndexOf("clone", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return obj;
            obj = obj.transform.parent?.gameObject;
        }
        return null;
    }

    private void PerformAction()
    {
        if (currentTarget == null) return;

        float scaleFactor = DetermineScaleFactor(currentTarget.transform.localScale.magnitude, IsVRMode());

        switch (currentMode)
        {
            case 1:
                if (IsVRMode())
                {
                    float verticalInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
                    if (verticalInput > 0.1f)
                        currentTarget.transform.localScale += Vector3.one * scaleFactor;
                    else if (verticalInput < -0.1f)
                        currentTarget.transform.localScale -= Vector3.one * scaleFactor;
                }
                else
                {
                    if (Input.GetKey(KeyCode.KeypadPlus))
                        currentTarget.transform.localScale += Vector3.one * scaleFactor * Time.deltaTime;
                    else if (Input.GetKey(KeyCode.KeypadMinus))
                        currentTarget.transform.localScale -= Vector3.one * scaleFactor * Time.deltaTime;
                }
                break;

            case 2:
                if (IsVRMode() && OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
                {
                    MirrorObject(deformAxis);
                    deformAxis = (deformAxis + 1) % 2;
                }
                else if (!IsVRMode())
                {
                    if (Input.GetKeyDown(KeyCode.KeypadPlus))
                        MirrorObject(0);
                    else if (Input.GetKeyDown(KeyCode.KeypadMinus))
                        MirrorObject(1);
                }
                break;

            case 3:
                if (IsVRMode() && OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
                {
                    Debug.Log("Objeto eliminado: " + currentTarget.name);
                    Destroy(currentTarget);
                    currentTarget = null;
                }
                else if (!IsVRMode() && Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    Debug.Log("Objeto eliminado: " + currentTarget.name);
                    Destroy(currentTarget);
                    currentTarget = null;
                }
                break;

            case 4:
                float incognitoScale = IsVRMode() ? vrIncognitoScaleFactor : desktopIncognitoScaleFactor;
                if (Input.GetKeyDown(KeyCode.Space) || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
                {
                    deformAxis = (deformAxis + 1) % 3;
                    Debug.Log($"Cambio de eje de deformación a: {(deformAxis == 0 ? "X" : deformAxis == 1 ? "Y" : "Z")}");
                }

                float deformInput = IsVRMode() ? OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y : (Input.GetKey(KeyCode.KeypadPlus) ? 1 : Input.GetKey(KeyCode.KeypadMinus) ? -1 : 0);
                if (Mathf.Abs(deformInput) > 0.1f)
                {
                    Vector3 deformation = Vector3.zero;
                    deformation[deformAxis] = incognitoScale * Mathf.Sign(deformInput);
                    currentTarget.transform.localScale += deformation;
                }
                break;
        }
    }

    private float DetermineScaleFactor(float scaleMagnitude, bool isVRMode)
    {
        if (isVRMode)
        {
            if (scaleMagnitude <= verySmallScaleThreshold)
                return vrVerySmallScaleFactor;
            else if (scaleMagnitude <= smallScaleThreshold)
                return vrSmallScaleFactor;
            else if (scaleMagnitude <= largeScaleThreshold)
                return vrLargeScaleFactor;
            else
                return vrVeryLargeScaleFactor;
        }
        else
        {
            if (scaleMagnitude <= verySmallScaleThreshold)
                return desktopVerySmallScaleFactor;
            else if (scaleMagnitude <= smallScaleThreshold)
                return desktopSmallScaleFactor;
            else if (scaleMagnitude <= largeScaleThreshold)
                return desktopLargeScaleFactor;
            else
                return desktopVeryLargeScaleFactor;
        }
    }

    private void MirrorObject(int axis)
    {
        if (axis == 0)
            currentTarget.transform.localScale = new Vector3(-currentTarget.transform.localScale.x, currentTarget.transform.localScale.y, currentTarget.transform.localScale.z);
        else if (axis == 1)
            currentTarget.transform.localScale = new Vector3(currentTarget.transform.localScale.x, currentTarget.transform.localScale.y, -currentTarget.transform.localScale.z);
    }

    private bool IsVRMode()
    {
        return OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
    }

    private void CheckHandGrabber()
    {
        isHeld = HandGrabber.HandGrabbers?.Any(hand => hand && Vector3.Distance(hand.transform.position, transform.position) < 0.2f) == true;
    }

    private void SetModoFrequency()
    {
        if (sonidoModo != null)
        {
            sonidoModo.pitch = 1.0f + 0.1f * (currentMode - 1);
        }
    }

    private void StopLaserAudio()
    {
        if (sonidoLaser != null && sonidoLaser.isPlaying)
        {
            sonidoLaser.Stop();
        }
    }

    private void StopModoAudio()
    {
        if (sonidoModo != null && sonidoModo.isPlaying)
        {
            sonidoModo.Stop();
        }
    }
}
