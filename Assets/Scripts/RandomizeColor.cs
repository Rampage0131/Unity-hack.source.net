using UnityEngine;

public class RandomizeColor : MonoBehaviour
{
    [SerializeField]
    Renderer[] allRenderers;
    [SerializeField]
    Color minRange;
    [SerializeField]
    Color maxRange;

    [Header("Randomizer")]
    [SerializeField]
    Vector2 scaleRange = new Vector2(0.5f, 1.5f);

    [Header("Editor-only")]
    [SerializeField]
    Material[] allMaterials;

    // Use this for initialization
    void Start ()
    {
        if (enabled == true)
        {
            HSBColor min = HSBColor.FromColor(minRange);
            HSBColor max = HSBColor.FromColor(maxRange);
            HSBColor newColor = new HSBColor();

            foreach (Renderer renderer in allRenderers)
            {
                foreach (Material material in renderer.materials)
                {
                    newColor.Hue = Random.value;
                    newColor.Saturation = Random.Range(min.Saturation, max.Saturation);
                    newColor.Brightness = Random.Range(min.Brightness, max.Brightness);
                    material.color = newColor.ToColor();
                }
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Randomize Materials")]
    void MaterialRandomizer()
    {
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer.sharedMaterials.Length > 0)
            {
                Material[] randomMaterials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < renderer.sharedMaterials.Length; ++i)
                {
                    randomMaterials[i] = allMaterials[Random.Range(0, allMaterials.Length)];
                }
                renderer.sharedMaterials = randomMaterials;
            }
        }
    }

    [ContextMenu("Revert Color")]
    void RevertColor()
    {
        int size;
        foreach (Renderer renderer in allRenderers)
        {
            size = renderer.materials.Length;
            for (int i = 0; i < size; ++i)
            {
                renderer.materials[i] = renderer.sharedMaterials[i];
            }
        }
    }

    // Update is called once per frame
    [ContextMenu("Get All Renderers")]
    void GetRenderers()
    {
        allRenderers = GetComponentsInChildren<Renderer>();
    }

    // Update is called once per frame
    [ContextMenu("Random Scale")]
    void RandomizeScale()
    {
        Vector3 angles;
        foreach (Renderer renderer in allRenderers)
        {
            renderer.transform.localScale = Vector3.one * Random.Range(scaleRange.x, scaleRange.y);
            angles = renderer.transform.eulerAngles;
            angles.y = Random.Range(0f, 360f);
            renderer.transform.rotation = Quaternion.Euler(angles);
        }
    }
#endif
}
