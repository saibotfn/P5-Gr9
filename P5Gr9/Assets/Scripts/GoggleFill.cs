using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.ARFoundation;

public class GoggleFill : MonoBehaviour
{
    [Header("References")]
    public Renderer sphere;                 // the water sphere on the camera
    public ARCameraManager cameraManager;   // on the Main Camera: passthrough on/off
    public AudioMixer mixer;                // optional, needs an exposed LowPassCutoff

    [Header("Timing (seconds)")]
    public float startDelay = 3f;
    public float fillTime = 7f;
    public float clearTime = 3f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    IEnumerator Start()
    {
        var mat = sphere.material;
        mat.SetFloat("_Fill", 0f);
        mat.SetFloat("_TintStrength", 1f);
        SetMuffle(0f);

        yield return new WaitForSeconds(startDelay);

        for (float t = 0; t < fillTime; t += Time.deltaTime)
        {
            float k = curve.Evaluate(t / fillTime);
            mat.SetFloat("_Fill", k);
            SetMuffle(k);
            yield return null;
        }
        mat.SetFloat("_Fill", 1f);
        SetMuffle(1f);
        cameraManager.enabled = false;   // the real room is no longer needed

        for (float t = 0; t < clearTime; t += Time.deltaTime)
        {
            mat.SetFloat("_TintStrength", 1f - t / clearTime);
            yield return null;
        }
        sphere.gameObject.SetActive(false);
    }

    void SetMuffle(float k)
    {
        if (mixer == null) return;
        // 600 Hz (muffled) to 22000 Hz (open), smooth on a log scale
        mixer.SetFloat("LowPassCutoff", 600f * Mathf.Pow(22000f / 600f, k));
    }
}