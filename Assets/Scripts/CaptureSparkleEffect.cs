using System.Collections;
using UnityEngine;

/// <summary>執行時產生粒子、擴散光圈與點光源，不需要另外安裝 VFX Graph。</summary>
public class CaptureSparkleEffect : MonoBehaviour
{
    private static Material particleMaterial;
    private static Material lineMaterial;

    private static Material GetParticleMaterial()
    {
        if (particleMaterial == null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            particleMaterial = new Material(shader) { name = "Runtime Sparkle" };
        }
        return particleMaterial;
    }

    public static void Burst(Vector3 center, Color color, int amount, float speed)
    {
        GameObject go = new GameObject("Capture particles");
        go.transform.position = center;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1.15f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
        main.startColor = color;
        main.gravityModifier = 0.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(new[] {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(color, 0.30f),
            new GradientColorKey(color, 1f)
        }, new[] {
            new GradientAlphaKey(0f, 0f),
            new GradientAlphaKey(1f, 0.10f),
            new GradientAlphaKey(0f, 1f)
        });
        col.color = new ParticleSystem.MinMaxGradient(grad);
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.45f), new Keyframe(0.28f, 1f),
            new Keyframe(1f, 0f));
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = GetParticleMaterial();
        ps.Play();
        ps.Emit(amount);
        Destroy(go, 3f);
    }

    public static void Ring(Vector3 center, Color color, float maxRadius)
    {
        GameObject go = new GameObject("Capture expanding ring");
        go.transform.position = center;
        var ring = go.AddComponent<LineRenderer>();
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            lineMaterial = new Material(shader) { name = "Runtime ring" };
        }
        ring.sharedMaterial = lineMaterial;
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 72;
        ring.widthMultiplier = 0.065f;
        ring.numCapVertices = 3;
        ring.startColor = color;
        ring.endColor = color;
        var driver = go.AddComponent<CaptureSparkleEffect>();
        driver.StartCoroutine(driver.ExpandRing(ring, color, maxRadius));
    }

    private IEnumerator ExpandRing(LineRenderer line, Color color, float maxRadius)
    {
        float elapsed = 0;
        const float length = 0.75f;
        while (elapsed < length)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / length);
            float radius = Mathf.Lerp(0.05f, maxRadius, Mathf.SmoothStep(0f, 1f, t));
            // 光圈在 XY 平面，正對攝影機（攝影機朝 +Z）。
            for (int i = 0; i < 72; i++)
            {
                float theta = (i / 72f) * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(theta) * radius,
                                                 Mathf.Sin(theta) * radius, 0f));
            }
            Color c = color;
            c.a = 1f - t;
            line.startColor = line.endColor = c;
            line.widthMultiplier = 0.085f * (1f - 0.55f * t);
            yield return null;
        }
        Destroy(gameObject);
    }

    public static void Flash(Vector3 position, Color color)
    {
        GameObject go = new GameObject("Capture flash light");
        go.transform.position = position;
        var lightComponent = go.AddComponent<Light>();
        lightComponent.type = LightType.Point;
        lightComponent.color = color;
        lightComponent.range = 7f;
        lightComponent.intensity = 0f;
        var driver = go.AddComponent<CaptureSparkleEffect>();
        driver.StartCoroutine(driver.FadeLight(lightComponent));
    }

    private IEnumerator FadeLight(Light lightComponent)
    {
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float normal = Mathf.Clamp01(t / 0.6f);
            lightComponent.intensity = Mathf.Sin(normal * Mathf.PI) * 5.5f;
            yield return null;
        }
        Destroy(gameObject);
    }
}
