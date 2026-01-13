using UnityEngine;
//using UnityEngine.Rendering;
using UnityEditor;

public class MyBaseShaderGUI : ShaderGUI
{

    static GUIContent staticLabel = new GUIContent();

    protected Material target;
    protected MaterialEditor editor;

    MaterialProperty[] properties;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        this.target = editor.target as Material;
        this.editor = editor;
        this.properties = properties;

    }

    #region Convinience
    protected MaterialProperty FindProperty(string _name)
    {
        return FindProperty(_name, properties);
    }
    protected static GUIContent staticLable = new GUIContent();
    protected static GUIContent MakeLabel(MaterialProperty _property, string _tooltip = null)
    {
        staticLable.text = _property.displayName;
        staticLable.tooltip = _tooltip;
        return staticLable;
    }
    protected static GUIContent MakeLabel(string _property, string _tooltip = null)
    {
        staticLable.text = _property;
        staticLable.tooltip = _tooltip;
        return staticLable;
    }
    protected void SetKeyword(string _keyword, bool _state)
    {
        if (_state)
        {
            foreach (Material m in editor.targets)
            {
                m.EnableKeyword(_keyword);
            }
        }
        else
        {
            foreach (Material m in editor.targets)
            {
                m.DisableKeyword(_keyword);
            }
        }
    }
    protected bool IsKeywordEnabled(string _keyword)
    {
        return target.IsKeywordEnabled(_keyword);
    }
    protected void RecordAction(string _lable)
    {
        editor.RegisterPropertyChangeUndo(_lable);
    }

    #endregion
}
