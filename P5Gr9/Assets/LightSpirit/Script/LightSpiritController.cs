using UnityEngine;

public class LightSpiritController : MonoBehaviour
{
    [Header("References")]
    public Transform spiritCore;
    public Transform spiritInnerGlow;
    public Transform spiritAura;
    public Transform sparkOrbit;
    public Light optionalLight;

    [Header("Floating")]
    public float floatAmplitude = 0.08f;
    public float floatSpeed = 1.2f;

    [Header("Pulse")]
    public float pulseAmplitude = 0.08f;
    public float pulseSpeed = 1.8f;

    [Header("Orbit")]
    public float orbitSpeed = 20f;

    [Header("Light Flicker")]
    public float lightBaseIntensity = 1.2f;
    public float lightFlickerAmount = 0.2f;
    public float lightFlickerSpeed = 2.5f;

    private Vector3 startPos;
    private Vector3 coreBaseScale;
    private Vector3 innerGlowBaseScale;
    private Vector3 auraBaseScale;

    void Start()
    {
        startPos = transform.localPosition;

        if (spiritCore != null)
            coreBaseScale = spiritCore.localScale;

        if (spiritInnerGlow != null)
            innerGlowBaseScale = spiritInnerGlow.localScale;

        if (spiritAura != null)
            auraBaseScale = spiritAura.localScale;
    }

    void Update()
    {
        FloatMotion();
        PulseMotion();
        RotateOrbit();
        FlickerLight();
    }

    void FloatMotion()
    {
        float y = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.localPosition = startPos + new Vector3(0f, y, 0f);
    }

    void PulseMotion()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;

        if (spiritCore != null)
            spiritCore.localScale = coreBaseScale * pulse;

        if (spiritInnerGlow != null)
            spiritInnerGlow.localScale = innerGlowBaseScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude * 0.6f);

        if (spiritAura != null)
            spiritAura.localScale = auraBaseScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude * 0.35f);
    }

    void RotateOrbit()
    {
        if (sparkOrbit != null)
        {
            sparkOrbit.Rotate(0f, orbitSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }

    void FlickerLight()
    {
        if (optionalLight != null)
        {
            float flicker = Mathf.Sin(Time.time * lightFlickerSpeed) * lightFlickerAmount;
            optionalLight.intensity = lightBaseIntensity + flicker;
        }
    }
}