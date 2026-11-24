using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.ARFoundation;

// Version 2: follows the start of Pygmalion's Spectacles.
// Look at the real room -> liquid is poured into the goggles -> the view clouds white with a buzz
// -> the mist thins out and the Unity world appears.
public class PygmalionTransition : MonoBehaviour
{
    [Header("References")]
    public Renderer sphere;                 // the liquid sphere on the camera (Custom/PygmalionLiquid)
    public ARCameraManager cameraManager;   // on the Main Camera: passthrough on/off
    public AudioMixer mixer;                // optional: exposed LowPassCutoff on the world's audio group
    public AudioSource slosh;               // optional: liquid being poured in (Play On Awake off)
    public AudioSource buzz;                // optional: looping buzz (Loop on, Play On Awake off)

    [Header("Timing (seconds)")]
    public float startDelay = 3f;           // you look at your real room
    public float pourTime = 3.5f;           // liquid rises and fills the goggles
    public float holdTime = 1.2f;           // the moment before the electrolysis starts
    public float cloudTime = 1.5f;          // the view clouds white
    public float whiteHold = 1.0f;          // solid white, formless buzz
    public float clearTime = 5f;            // the mist thins and the world emerges

    static readonly float HideWorld = (float)UnityEngine.Rendering.BlendMode.Zero;
    static readonly float ShowWorld = (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

    IEnumerator Start()
    {
        var mat = sphere.material;
        mat.SetFloat("_Fill", 0f);
        mat.SetFloat("_Cloud", 0f);
        mat.SetFloat("_Liquid", 1f);
        mat.SetFloat("_DstBlend", HideWorld);   // world hidden, the real room shows through
        SetMuffle(0f);
        if (buzz) { buzz.loop = true; buzz.volume = 0f; buzz.Play(); }

        yield return new WaitForSeconds(startDelay);

        // 1. Liquid is poured in
        if (slosh) slosh.Play();
        yield return StartCoroutine(Ramp(pourTime, k => mat.SetFloat("_Fill", k)));
        yield return new WaitForSeconds(holdTime);

        // 2. Electrolysis: the liquid clouds white and the buzz starts
        yield return StartCoroutine(Ramp(cloudTime, k =>
        {
            mat.SetFloat("_Cloud", k);
            if (buzz) { buzz.volume = k; buzz.pitch = 0.8f + 0.4f * k; }
        }));

        // The view is solid white now, so swap the world in underneath and drop passthrough
        mat.SetFloat("_DstBlend", ShowWorld);
        cameraManager.enabled = false;
        yield return new WaitForSeconds(whiteHold);

        // 3. The mist thins out and the world appears
        yield return StartCoroutine(Ramp(clearTime, k =>
        {
            mat.SetFloat("_Cloud", 1f - k);
            mat.SetFloat("_Liquid", 1f - k);
            if (buzz) { buzz.volume = 1f - k; buzz.pitch = 1.2f - 0.4f * k; }
            SetMuffle(k);
        }));

        if (buzz) buzz.Stop();
        sphere.gameObject.SetActive(false);
    }

    IEnumerator Ramp(float seconds, System.Action<float> apply)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            apply(Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        apply(1f);
    }

    void SetMuffle(float k)
    {
        if (mixer == null) return;
        // 600 Hz (muffled) to 22000 Hz (open), smooth on a log scale
        mixer.SetFloat("LowPassCutoff", 600f * Mathf.Pow(22000f / 600f, k));
    }
}
