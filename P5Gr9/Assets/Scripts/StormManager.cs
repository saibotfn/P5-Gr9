
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StormManager : MonoBehaviour
{
    [Header("Scene References")]
    public Light sunLight;
    public AudioSource windAudio;
    public AudioSource thunderAudio;
    [SerializeField] private WindController windController;

    [Header("Storm Atmosphere")]
    [Range(0f, 1f)]
    public float stormLightMultiplier = 0.35f;

    public Color stormFogColor =
        new Color(0.20f, 0.25f, 0.32f);

    [Min(0f)]
    public float stormFogDensity = 0.006f;

    [Min(0.1f)]
    public float transitionDuration = 8f;

    [Header("Wind")]
    [Range(0f, 1f)]
    public float stormWindVolume = 0.6f;

    [Header("Lightning and Thunder")]
    public AudioClip[] thunderClips;

    public bool enableLightningFlashes = true;

    [Min(1f)]
    public float minimumLightningInterval = 8f;

    [Min(1f)]
    public float maximumLightningInterval = 16f;

    [Range(0f, 2f)]
    public float lightningFlashStrength = 0.35f;

    [Header("Testing")]
    public bool enableKeyboardTesting = true;

    private float originalLightIntensity;
    private Color originalFogColor;
    private float originalFogDensity;
    private bool originalFogEnabled;

    private Coroutine transitionRoutine;
    private Coroutine lightningRoutine;

    private bool stormActive;
    private bool stormEnding;
    private float currentFlashIntensity;

    void Awake()
    {
        // Remember the original lighting.
        if (sunLight != null)
        {
            originalLightIntensity = sunLight.intensity;
        }

        // Remember original fog settings.
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogEnabled = RenderSettings.fog;

        // Prepare wind audio.
        if (windAudio != null)
        {
            windAudio.playOnAwake = false;
            windAudio.loop = true;
            windAudio.spatialBlend = 0f;
            windAudio.volume = 0f;
        }

        // Prepare thunder audio.
        if (thunderAudio != null)
        {
            thunderAudio.playOnAwake = false;
            thunderAudio.spatialBlend = 0f;
        }
    }

    void Update()
    {
        if (!enableKeyboardTesting)
            return;

        // Uses Unity's NEW Input System.
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            BeginStorm();
        }

        if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            EndStorm();
        }
    }

    // =====================================
    // START STORM
    // =====================================

    public void BeginStorm()
    {
        if (stormActive || stormEnding)
            return;
        if (windController != null)
        {
            windController.windStrength = 0.5f;
        }

        stormActive = true;
        stormEnding = false;

        RenderSettings.fog = true;

        // Start wind sound if assigned.
        if (windAudio != null &&
            windAudio.clip != null &&
            !windAudio.isPlaying)
        {
            windAudio.Play();
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(
            TransitionAtmosphere(
                originalLightIntensity * stormLightMultiplier,
                stormFogColor,
                stormFogDensity,
                stormWindVolume,
                transitionDuration
            )
        );

        if (lightningRoutine != null)
        {
            StopCoroutine(lightningRoutine);
        }

        lightningRoutine = StartCoroutine(
            LightningLoop()
        );

        Debug.Log("Storm started!");
    }

    // =====================================
    // END STORM
    // =====================================

    public void EndStorm()
    {
        if (!stormActive || stormEnding)
            return;
        if (windController != null)
        {
            windController.windStrength = 0.1f;
        }

        stormActive = false;
        stormEnding = true;

        if (lightningRoutine != null)
        {
            StopCoroutine(lightningRoutine);
            lightningRoutine = null;
        }

        // Remove any active lightning flash.
        currentFlashIntensity = 0f;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(
            EndStormSequence()
        );

        Debug.Log("Storm ending!");
    }

    private IEnumerator EndStormSequence()
    {
        yield return TransitionAtmosphere(
            originalLightIntensity,
            originalFogColor,
            originalFogDensity,
            0f,
            transitionDuration
        );

        RenderSettings.fog = originalFogEnabled;

        if (windAudio != null)
        {
            windAudio.Stop();
        }

        stormEnding = false;
        transitionRoutine = null;

        Debug.Log("Storm ended!");
    }

    // =====================================
    // ATMOSPHERE TRANSITION
    // =====================================

    private IEnumerator TransitionAtmosphere(
        float targetLight,
        Color targetFogColor,
        float targetFogDensity,
        float targetWindVolume,
        float duration)
    {
        float startLight = sunLight != null
            ? sunLight.intensity - currentFlashIntensity
            : 0f;

        Color startFogColor = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;

        float startWindVolume = windAudio != null
            ? windAudio.volume
            : 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            if (sunLight != null)
            {
                sunLight.intensity =
                    Mathf.Lerp(startLight, targetLight, t)
                    + currentFlashIntensity;
            }

            RenderSettings.fogColor = Color.Lerp(
                startFogColor,
                targetFogColor,
                t
            );

            RenderSettings.fogDensity = Mathf.Lerp(
                startFogDensity,
                targetFogDensity,
                t
            );

            if (windAudio != null)
            {
                windAudio.volume = Mathf.Lerp(
                    startWindVolume,
                    targetWindVolume,
                    t
                );
            }

            yield return null;
        }

        if (sunLight != null)
        {
            sunLight.intensity =
                targetLight + currentFlashIntensity;
        }

        RenderSettings.fogColor = targetFogColor;
        RenderSettings.fogDensity = targetFogDensity;

        if (windAudio != null)
        {
            windAudio.volume = targetWindVolume;
        }
    }

    // =====================================
    // LIGHTNING
    // =====================================

    private IEnumerator LightningLoop()
    {
        // Wait for the atmosphere to darken.
        yield return new WaitForSeconds(
            transitionDuration
        );

        while (stormActive)
        {
            float waitTime = Random.Range(
                minimumLightningInterval,
                Mathf.Max(
                    minimumLightningInterval,
                    maximumLightningInterval
                )
            );

            yield return new WaitForSeconds(waitTime);

            if (!stormActive)
                yield break;

            if (enableLightningFlashes &&
                sunLight != null)
            {
                yield return LightningFlash();
            }

            // Thunder follows lightning.
            yield return new WaitForSeconds(
                Random.Range(1.5f, 3f)
            );

            if (!stormActive)
                yield break;

            PlayThunder();
        }
    }

    private IEnumerator LightningFlash()
    {
        // Short, relatively gentle flash.
        currentFlashIntensity = lightningFlashStrength;

        if (sunLight != null)
        {
            sunLight.intensity += currentFlashIntensity;
        }

        yield return new WaitForSeconds(0.12f);

        if (sunLight != null)
        {
            sunLight.intensity -= currentFlashIntensity;
        }

        currentFlashIntensity = 0f;
    }

    // =====================================
    // THUNDER AUDIO
    // =====================================

    private void PlayThunder()
    {
        if (thunderAudio == null ||
            thunderClips == null ||
            thunderClips.Length == 0)
        {
            return;
        }

        AudioClip clip = thunderClips[
            Random.Range(0, thunderClips.Length)
        ];

        if (clip != null)
        {
            thunderAudio.PlayOneShot(clip, 0.7f);
        }
    }

    // =====================================
    // CLEANUP
    // =====================================

    private void OnDisable()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (lightningRoutine != null)
        {
            StopCoroutine(lightningRoutine);
            lightningRoutine = null;
        }

        currentFlashIntensity = 0f;
        stormActive = false;
        stormEnding = false;

        // Restore original atmosphere.
        if (sunLight != null)
        {
            sunLight.intensity = originalLightIntensity;
        }

        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fog = originalFogEnabled;

        if (windAudio != null)
        {
            windAudio.Stop();
        }
    }
}
