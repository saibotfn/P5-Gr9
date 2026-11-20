using UnityEngine;

// One script that owns the wind. It sets the wind strength on the tree
// materials and drives the wind sound from the same number, so what you
// see and what you hear always match.
// Wind Strength 0 = completely calm: no sway and no sound.
public class WindController : MonoBehaviour
{
    // The shader's Reference name for Wind Strength (Polytope Studio vegetation shaders)
    const string StrengthProperty = "_WindStrength";

    [Header("Tree materials (drag them in from the Project window)")]
    public Material[] materials;

    [Header("Wind")]
    [Range(0f, 1f)] public float windStrength = 0.6f;      // master control, 0 = calm
    [Range(0f, 1f)] public float gustAmount = 0.4f;        // how much it swells and fades
    public float gustSpeed = 0.15f;                        // how fast gusts come and go
    public float smoothing = 2f;                           // higher = reacts faster
    [Range(0f, 1f)] public float maxShaderStrength = 0.7f; // shader value at full wind

    [Header("Audio (optional)")]
    public AudioSource windAudio;
    [Range(0f, 1f)] public float maxVolume = 0.6f;         // volume at full wind
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    // Current wind level, 0..1. Other scripts can read this (particles, etc.)
    public float Level { get; private set; }

    int strengthId;
    float[] originals;

    void OnEnable()
    {
        strengthId = Shader.PropertyToID(StrengthProperty);
        originals = new float[materials.Length];

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;

            if (!materials[i].HasProperty(strengthId))
            {
                Debug.LogWarning($"{materials[i].name} has no property '{StrengthProperty}'.");
                continue;
            }

            originals[i] = materials[i].GetFloat(strengthId);
        }

        if (windAudio != null)
            windAudio.loop = true;
    }

    void OnDisable()
    {
        // Put the materials back, since edits to a material asset can stick in the editor
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && materials[i].HasProperty(strengthId))
                materials[i].SetFloat(strengthId, originals[i]);
        }

        if (windAudio != null)
            windAudio.Stop();
    }

    void Update()
    {
        // Perlin noise gives smooth, natural-feeling gusts
        float gust = Mathf.PerlinNoise(Time.time * gustSpeed, 0f);  // 0..1
        float variation = 1f + (gust - 0.5f) * 2f * gustAmount;     // 1 +/- gustAmount
        float target = Mathf.Clamp01(windStrength * variation);     // 0 stays exactly 0

        Level = Mathf.Lerp(Level, target, Mathf.Clamp01(Time.deltaTime * smoothing));
        if (target < 0.001f && Level < 0.001f) Level = 0f;          // snap to fully calm

        float strength = Level * maxShaderStrength;
        foreach (var m in materials)
        {
            if (m != null && m.HasProperty(strengthId))
                m.SetFloat(strengthId, strength);
        }

        if (windAudio != null)
        {
            if (Level > 0f)
            {
                if (!windAudio.isPlaying) windAudio.Play();
                windAudio.volume = Level * maxVolume;
                windAudio.pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, Level);
            }
            else if (windAudio.isPlaying)
            {
                windAudio.Stop();
            }
        }
    }
}