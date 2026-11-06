using System.Collections.Generic;
using UnityEngine;

// Runtime-built particle effects. They run on unscaled time because the game often sets timeScale to 0 or close to it.
public class BoardFx
{
    readonly Material alpha;
    readonly Material additive;
    readonly Transform parent;
    readonly List<KeyValuePair<float, GameObject>> expiring = new List<KeyValuePair<float, GameObject>>();

    // Removes finished one-shot effects. Object.Destroy's delay uses scaled time, which often stands still here.
    public void Tick()
    {
        for (int i = expiring.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime < expiring[i].Key)
                continue;

            Object.Destroy(expiring[i].Value);
            expiring.RemoveAt(i);
        }
    }

    public BoardFx(BoardArt art, Transform parent)
    {
        this.parent = parent;
        Texture2D soft = BuildSoftCircle();
        alpha = new Material(art.particleAlpha);
        additive = new Material(art.particleAdditive);
        alpha.SetTexture("_BaseMap", soft);
        additive.SetTexture("_BaseMap", soft);
    }

    static Texture2D BuildSoftCircle()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size - 0.5f;
                float dy = (y + 0.5f) / size - 0.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                byte a = (byte)(255f * Mathf.Pow(Mathf.Clamp01(1f - distance), 1.3f));
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    ParticleSystem Create(string name, Vector3 position, bool glow)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = glow ? additive : alpha;
        return system;
    }

    public void Burst(Vector3 position, Color color, int count, float size, float speed, float life, bool glow = true)
    {
        ParticleSystem system = Create("Burst", position, glow);
        var main = system.main;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.maxParticles = count;

        var emission = system.emission;
        emission.rateOverTime = 0f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        var fade = system.colorOverLifetime;
        fade.enabled = true;
        fade.color = FadeOut();
        system.Play();
        system.Emit(count);
        expiring.Add(new KeyValuePair<float, GameObject>(Time.unscaledTime + life + 0.2f, system.gameObject));
    }

    public void Explosion(Vector3 position, float scale)
    {
        Burst(position, new Color(1f, 0.85f, 0.45f), 14, 1.1f * scale, 1.5f * scale, 0.45f);
        Burst(position, new Color(1f, 0.35f, 0.08f), 18, 0.8f * scale, 2.6f * scale, 0.8f);
        Burst(position, new Color(0.12f, 0.11f, 0.11f, 0.8f), 12, 1.2f * scale, 1.2f * scale, 1.8f, false);
    }

    public ParticleSystem Smoke(Vector3 position, Transform follow, float rate, float size, Color color)
    {
        ParticleSystem system = Create("Smoke", position, false);
        if (follow != null)
            system.transform.SetParent(follow, true);

        var main = system.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.maxParticles = Mathf.CeilToInt(rate * 3f);

        var emission = system.emission;
        emission.rateOverTime = rate;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.04f;
        system.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        var fade = system.colorOverLifetime;
        fade.enabled = true;
        fade.color = FadeOut();
        system.Play();
        return system;
    }

    public ParticleSystem Flame(Vector3 position, Transform follow, float rate, float size)
    {
        ParticleSystem system = Create("Flame", position, true);
        if (follow != null)
            system.transform.SetParent(follow, true);

        var main = system.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        Color color = new Color(1f, 0.5f, 0.12f);
        main.startColor = color;
        main.maxParticles = Mathf.CeilToInt(rate);

        var emission = system.emission;
        emission.rateOverTime = rate;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 10f;
        shape.radius = 0.05f;
        system.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        var fade = system.colorOverLifetime;
        fade.enabled = true;
        fade.color = FadeOut();
        system.Play();
        return system;
    }

    static ParticleSystem.MinMaxGradient FadeOut()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
        return gradient;
    }
}
