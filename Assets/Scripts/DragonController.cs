using System.Collections;
using UnityEngine;

/// <summary>
/// Dragón procedural (hecho con primitivas, sin assets externos).
/// Vuela en círculos sobre un punto, aletea y escupe fuego.
/// </summary>
public class DragonController : MonoBehaviour
{
    public float radius = 0.7f;   // radio del círculo de vuelo (metros)
    public float speed = 0.6f;    // velocidad de vuelo (m/s)
    public float height = 0.45f;  // altura sobre el suelo (metros)

    Transform wingPivotL, wingPivotR, head;
    Vector3 center;
    float angle, t;

    public static DragonController Spawn(Vector3 groundPos)
    {
        var go = new GameObject("Dragon");
        var d = go.AddComponent<DragonController>();
        d.MoveTo(groundPos);
        return d;
    }

    public void MoveTo(Vector3 groundPos)
    {
        center = groundPos;
    }

    void Awake()
    {
        BuildModel();
    }

    void Update()
    {
        t += Time.deltaTime;
        angle += speed / radius * Time.deltaTime;

        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        Vector3 pos = center + offset + Vector3.up * (height + 0.05f * Mathf.Sin(t * 2f));
        Vector3 fwd = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, 0f, -12f);

        // Aleteo
        float a = 30f * Mathf.Sin(t * 8f) + 10f;
        wingPivotR.localRotation = Quaternion.Euler(0f, 0f, a);
        wingPivotL.localRotation = Quaternion.Euler(0f, 0f, -a);
    }

    // ---------- Fuego ----------
    public void BreatheFire()
    {
        StartCoroutine(FireRoutine());
    }

    IEnumerator FireRoutine()
    {
        for (int i = 0; i < 28; i++)
        {
            SpawnFireball();
            yield return new WaitForSeconds(0.03f);
        }
    }

    void SpawnFireball()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.transform.position = head.position + head.forward * 0.08f;
        go.transform.localScale = Vector3.one * 0.06f;
        var f = go.AddComponent<Fireball>();
        Vector3 dir = head.forward + Vector3.down * 0.25f + Random.insideUnitSphere * 0.12f;
        f.velocity = dir.normalized * 1.6f;
    }

    // ---------- Modelo ----------
    void BuildModel()
    {
        Color dark = new Color(0.12f, 0.1f, 0.1f);
        Color red = new Color(0.55f, 0.08f, 0.05f);

        // Cuerpo
        Part(PrimitiveType.Sphere, transform, new Vector3(0, 0, 0), new Vector3(0.22f, 0.18f, 0.5f), dark);
        // Cuello y cabeza
        Part(PrimitiveType.Capsule, transform, new Vector3(0, 0.12f, 0.32f), new Vector3(0.07f, 0.12f, 0.07f), dark, new Vector3(50, 0, 0));
        head = Part(PrimitiveType.Sphere, transform, new Vector3(0, 0.22f, 0.46f), new Vector3(0.12f, 0.1f, 0.18f), dark);
        // Cuernos
        Part(PrimitiveType.Cube, transform, new Vector3(0.04f, 0.29f, 0.42f), new Vector3(0.02f, 0.02f, 0.1f), red, new Vector3(-120, 0, 0));
        Part(PrimitiveType.Cube, transform, new Vector3(-0.04f, 0.29f, 0.42f), new Vector3(0.02f, 0.02f, 0.1f), red, new Vector3(-120, 0, 0));
        // Ojos
        Part(PrimitiveType.Sphere, transform, new Vector3(0.045f, 0.24f, 0.53f), Vector3.one * 0.025f, Color.red);
        Part(PrimitiveType.Sphere, transform, new Vector3(-0.045f, 0.24f, 0.53f), Vector3.one * 0.025f, Color.red);
        // Cola
        Part(PrimitiveType.Sphere, transform, new Vector3(0, 0, -0.35f), Vector3.one * 0.12f, dark);
        Part(PrimitiveType.Sphere, transform, new Vector3(0, 0, -0.5f), Vector3.one * 0.08f, dark);
        Part(PrimitiveType.Sphere, transform, new Vector3(0, 0, -0.62f), Vector3.one * 0.05f, dark);
        Part(PrimitiveType.Cube, transform, new Vector3(0, 0, -0.72f), new Vector3(0.02f, 0.05f, 0.08f), red);

        // Alas (pivotes para el aleteo)
        wingPivotR = new GameObject("WingR").transform;
        wingPivotR.SetParent(transform, false);
        wingPivotR.localPosition = new Vector3(0.08f, 0.1f, 0.05f);
        Part(PrimitiveType.Cube, wingPivotR, new Vector3(0.3f, 0, -0.05f), new Vector3(0.6f, 0.015f, 0.35f), red);
        Part(PrimitiveType.Cube, wingPivotR, new Vector3(0.3f, 0.01f, 0.1f), new Vector3(0.6f, 0.02f, 0.03f), dark);

        wingPivotL = new GameObject("WingL").transform;
        wingPivotL.SetParent(transform, false);
        wingPivotL.localPosition = new Vector3(-0.08f, 0.1f, 0.05f);
        Part(PrimitiveType.Cube, wingPivotL, new Vector3(-0.3f, 0, -0.05f), new Vector3(0.6f, 0.015f, 0.35f), red);
        Part(PrimitiveType.Cube, wingPivotL, new Vector3(-0.3f, 0.01f, 0.1f), new Vector3(0.6f, 0.02f, 0.03f), dark);
    }

    Transform Part(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, Vector3? euler = null)
    {
        var go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        go.GetComponent<Renderer>().material = DragonMaterials.Make(color);
        return go.transform;
    }
}
