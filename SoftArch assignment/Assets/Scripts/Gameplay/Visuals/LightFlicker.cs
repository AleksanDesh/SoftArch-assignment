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
    float _originalIntensity;
    Color _originalColor;
    float _origHue, _origSat, _origVal;
    float _intensityPhase;
    float _colorPhase;

    void Awake()
    {
        _light = GetComponent<Light>();
        _originalIntensity = _light.intensity;
        _originalColor = _light.color;
        Color.RGBToHSV(_originalColor, out _origHue, out _origSat, out _origVal);

        // random offsets so multiple lights don't flicker identically
        if (randomSeed == 0f) randomSeed = Random.value * 1000f;
        _intensityPhase = randomSeed + Random.Range(0f, 10f);
        _colorPhase = randomSeed + Random.Range(10f, 20f);
    }

    void Update()
    {
        float t = Time.time;

        // --- Intensity ---
        float intensityFactor = 0f; // -1 .. 1
        if (usePerlinForIntensity)
        {
            // PerlinNoise returns 0..1 -> remap to -1..1
            float p = Mathf.PerlinNoise(_intensityPhase, t * intensityFrequency);
            intensityFactor = p * 2f - 1f;
        }
        else
        {
            // sine wave: cycles per second = intensityFrequency
            intensityFactor = Mathf.Sin(2f * Mathf.PI * intensityFrequency * t + _intensityPhase);
        }

        float newIntensity = _originalIntensity + intensityFactor * intensityAmplitude;
        newIntensity = Mathf.Max(0f, newIntensity); // don't go negative
        _light.intensity = newIntensity;

        // --- Color (HSV) ---
        float colorFactorH = 0f, colorFactorS = 0f, colorFactorV = 0f;
        if (usePerlinForColor)
        {
            float ph = Mathf.PerlinNoise(_colorPhase, t * colorFrequency);
            float ps = Mathf.PerlinNoise(_colorPhase + 10f, t * colorFrequency);
            float pv = Mathf.PerlinNoise(_colorPhase + 20f, t * colorFrequency);
            colorFactorH = ph * 2f - 1f;
            colorFactorS = ps * 2f - 1f;
            colorFactorV = pv * 2f - 1f;
        }
        else
        {
            float s = Mathf.Sin(2f * Mathf.PI * colorFrequency * t + _colorPhase);
            float s2 = Mathf.Sin(2f * Mathf.PI * colorFrequency * t * 1.37f + _colorPhase + 1f);
            float s3 = Mathf.Sin(2f * Mathf.PI * colorFrequency * t * 0.73f + _colorPhase + 2f);
            colorFactorH = s;
            colorFactorS = s2;
            colorFactorV = s3;
        }

        float h = _origHue + colorFactorH * hueAmplitude;
        h = Mathf.Repeat(h, 1f); // wrap hue
        float sNew = Mathf.Clamp01(_origSat + colorFactorS * saturationAmplitude);
        float vNew = Mathf.Clamp01(_origVal + colorFactorV * valueAmplitude);

        Color newColor = Color.HSVToRGB(h, sNew, vNew);
        // preserve alpha from original color (usually 1)
        newColor.a = _originalColor.a;
        _light.color = newColor;
    }
}
