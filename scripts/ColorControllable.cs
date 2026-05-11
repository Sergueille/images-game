using System.Collections.Generic;
using Godot;

public partial class ColorControllable : Node
{
    public enum MaterialProperty { Hue, Saturation, Brightness };

    [Export] protected CanvasItem canvasItem;
    [Export] ShaderMaterial spriteMaterial;

    Vector2 handlePlacementMultiplier;

    public Dictionary<string, float> properties;

    public void SetMaterialProperty(MaterialProperty property, bool plusOrMinus)
    {
        string param = property switch
        {
            MaterialProperty.Hue => "hueShift",
            MaterialProperty.Saturation => "saturation",
            MaterialProperty.Brightness => "brightness",
            _ => throw new  System.Diagnostics.UnreachableException(),
        };

        float currentValue = (float)spriteMaterial.GetShaderParameter(param);
        var (min, max, step) = GetMinMaxStep(property);
        float newValue = currentValue + (plusOrMinus ? step : -step);

        if (newValue < min) newValue = min;
        if (newValue > max) newValue = max;

        SetProp(param, newValue);
    }

    public void ResetMaterialPropertyToWhite()
    {
        SetProp("hueShift", 0.0f);
        SetProp("saturation", -1.0f);
        SetProp("brightness", 1.0f);
    }

    public (float, float, float) GetMinMaxStep(MaterialProperty property)
    {
        return property switch
        {
            MaterialProperty.Hue => (float.MinValue, float.MaxValue, 0.025f),
            MaterialProperty.Saturation => (-1.0f, 3.0f, 0.2f),
            MaterialProperty.Brightness => (-1.0f, 1.0f, 0.1f),
            _ => throw new  System.Diagnostics.UnreachableException(),
        };
    }

    public void SetProp(string propName, float val)
    {
        spriteMaterial.SetShaderParameter(propName, val);
        
        if (properties == null) properties = new Dictionary<string, float>();
        properties[propName] = val;
    }

    public void ApplyProperties(Dictionary<string, float> props)
    {
        if (props == null)
        {
            GD.PushWarning("Applied ColorControllable properties with null dictionary");
            return;
        }

        foreach ((string p, float v) in props)
        {
            SetProp(p, v);
        }
    }
}
