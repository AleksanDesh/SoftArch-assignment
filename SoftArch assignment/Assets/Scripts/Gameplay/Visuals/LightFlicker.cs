using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Intensity (units are additive around original)")]
    public float intensityAmplitude = 0.5f;   // max +/- change from original intensity
    public float intensityFrequency = 1.0f;   // cycles per second
    public bool usePerlinForIntensity = true; // toggle between sine and Perlin

    [Header("Color (HSV deltas around original)")]
    [Range(0f, 0.5f)] public float hueAmplitude = 0.02f;        // how much to shift hue (wraps)
    [Range(0f, 1f)] public float saturationAmplitude = 0.05f;  // +/- sat
    [Range(0f, 1f)] public float valueAmplitude = 0.05f;       // +/- value (brightness)
    public float colorFrequency = 0.5f;
    public bool usePerlinForColor = true;

    [Header("General")]
    public float randomSeed = 0f; // set nonzero for different random offsets per light

    // cached
    Light _light;
    float originalIntensity;
    Color originalColor;
    float origHue, origSat, origVal;
    float intensityPhase;
    float colorPhase;

    void Awake()
    {
        _light = GetComponent<Light>();
        originalIntensity = _light.intensity;
        originalColor = _light.color;
        Color.RGBToHSV(originalColor, out origHue, out origSat, out origVal);

        // random offsets so multiple lights don't flicker identically
        if (randomSeed == 0f) randomSeed = Random.value * 1000f;
        intensityPhase = randomSeed + Random.Range(0f, 10f);
        colorPhase = randomSeed + Random.Range(10f, 20f);
    }

    void Update()
    {
        float t = Time.time;

        // --- Intensity ---
        float intensityFactor = 0f; // -1 .. 1
        if (usePerlinForIntensity)
        {
            // PerlinNoise returns 0..1 -> remap to -1..1
            float p = Mathf.PerlinNoise(intensityPhase, t * intensityFrequency);
            intensityFactor = p * 2f - 1f;
        }
        else
        {
            // sine wave: cycles per second = intensityFrequency
            intensityFactor = Mathf.Sin(2f * Mathf.PI * intensityFrequency * t + intensityPhase);
        }

        float newIntensity = originalIntensity + intensityFactor * intensityAmplitude;
        newIntensity = Mathf.Max(0f, newIntensity); // don't go negative
        _light.intensity = newIntensity;

        // --- Color (HSV) ---
        float colorFactorH = 0f, colorFactorS = 0f, colorFactorV = 0f;
        if (usePerlinForColor)
        {
            float ph = Mathf.PerlinNoise(colorPhase, t * colorFrequency);
            float ps = Mathf.PerlinNoise(colorPhase + 10f, t * colorFrequency);
            float pv = Mathf.PerlinNoise(colorPhase + 20f, t * colorFrequency);
            colorFactorH = ph * 2f - 1f;
            colorFactorS = ps * 2f - 1f;
            colorFactorV = pv * 2f - 1f;
        }
        else
        {
            float s = Mathf.Sin(2f * Mathf.PI * colorFrequency * t + colorPhase);
            float s2 = Mathf.Sin(2f * Mathf.PI * colorFrequency * t * 1.37f + colorPhase + 1f);
            float s3 = Mathf.Sin(2f * Mathf.PI * colorFrequency * t * 0.73f + colorPhase + 2f);
            colorFactorH = s;
            colorFactorS = s2;
            colorFactorV = s3;
        }

        float h = origHue + colorFactorH * hueAmplitude;
        h = Mathf.Repeat(h, 1f); // wrap hue
        float sNew = Mathf.Clamp01(origSat + colorFactorS * saturationAmplitude);
        float vNew = Mathf.Clamp01(origVal + colorFactorV * valueAmplitude);

        Color newColor = Color.HSVToRGB(h, sNew, vNew);
        // preserve alpha from original color (usually 1)
        newColor.a = originalColor.a;
        _light.color = newColor;
    }
}
