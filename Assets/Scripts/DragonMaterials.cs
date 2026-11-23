using UnityEngine;

/// <summary>
/// Crea materiales a partir de "Resources/DragonBase" (shader Standard).
/// Al estar referenciado desde Resources, el shader se incluye en el APK
/// (los primitivos por defecto salían rosados/magenta en el build).
/// </summary>
public static class DragonMaterials
{
    static Material baseMat;

    public static Material Make(Color color)
    {
        if (baseMat == null) baseMat = Resources.Load<Material>("DragonBase");
        Material m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Standard"));
        m.color = color;
        return m;
    }
}
