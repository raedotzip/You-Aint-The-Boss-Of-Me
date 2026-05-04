using UnityEngine;

public class RainbowColor : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private string colorProperty = "_Color";

    private Material _material;
    private float _hue;

    void Start()
    {
        _material = GetComponent<Renderer>().material;
    }

    void Update()
    {
        _hue = (_hue + speed * Time.deltaTime) % 1f;
        _material.SetColor(colorProperty, Color.HSVToRGB(_hue, 1f, 1f));
    }

    void OnDestroy()
    {
        Destroy(_material);
    }
}
