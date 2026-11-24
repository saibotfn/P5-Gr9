using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class GoggleFill : MonoBehaviour
{
    public Renderer sphere;
    public float fillTime = 7f, clearTime = 3f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public ARCameraManager cameraManager;

    IEnumerator Start()
    {
        var mat = sphere.material;
        mat.SetFloat("_Fill", 0f);
        mat.SetFloat("_TintStrength", 1f);
        yield return new WaitForSeconds(2f);

        for (float t = 0; t < fillTime; t += Time.deltaTime)
        {
            mat.SetFloat("_Fill", curve.Evaluate(t / fillTime));
            yield return null;
        }
        mat.SetFloat("_Fill", 1f);

        for (float t = 0; t < clearTime; t += Time.deltaTime)
        {
            mat.SetFloat("_TintStrength", 1f - t / clearTime);
            yield return null;
        }
        sphere.gameObject.SetActive(false);
        cameraManager.enabled = false;
    }
}