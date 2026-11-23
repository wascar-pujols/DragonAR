using UnityEngine;

/// <summary>Bola de fuego simple: avanza, crece, cambia de amarillo a rojo y desaparece.</summary>
public class Fireball : MonoBehaviour
{
    public Vector3 velocity;

    const float MaxLife = 0.9f;
    float life;
    Renderer rend;
    Vector3 startScale;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material = DragonMaterials.Make(Color.white);
        startScale = transform.localScale;
    }

    void Update()
    {
        life += Time.deltaTime;
        float k = life / MaxLife;
        if (k >= 1f)
        {
            Destroy(rend.material);
            Destroy(gameObject);
            return;
        }

        transform.position += velocity * Time.deltaTime;
        transform.localScale = startScale * (1f + 2f * k) * (1f - k * 0.6f);
        rend.material.color = Color.Lerp(new Color(1f, 0.9f, 0.2f), new Color(0.8f, 0.1f, 0f), k);
    }
}
