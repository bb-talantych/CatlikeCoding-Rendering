using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Codice.Client.BaseCommands;

public class MyCatlikeCodingShaderGUI8 : MyBaseShaderGUI
{
    Material target;
    MaterialEditor editor;
    MaterialProperty[] properties;

    enum TessellationMode
    {
        Uniform, Edge
    }
    enum SmoothnessSource
    {
        Uniform, Albedo, Metallic
    }
    enum RenderingMode
    {
        Opaque, Cutout, Fade, Transparent
    }

    bool shouldShowAlphaCutoff;

    struct RenderingSettings
    {
        public RenderQueue queue;
        public string renderType;
        public BlendMode srcBlend, dstBlend;
        public bool zWrite;

        public static RenderingSettings[] modes =
        {
            new RenderingSettings() { queue = RenderQueue.Geometry, renderType = "",
                srcBlend =  BlendMode.One, dstBlend = BlendMode.Zero, zWrite = true},
            new RenderingSettings() { queue = RenderQueue.AlphaTest, renderType = "TransparentCutout",
            srcBlend =  BlendMode.One, dstBlend = BlendMode.Zero, zWrite = true},
            new RenderingSettings() { queue = RenderQueue.Transparent, renderType = "Transparent",
            srcBlend =  BlendMode.SrcAlpha, dstBlend = BlendMode.OneMinusSrcAlpha, zWrite = false},
            new RenderingSettings() { queue = RenderQueue.Transparent, renderType = "Transparent",
                srcBlend = BlendMode.One, dstBlend = BlendMode.OneMinusSrcAlpha,zWrite = false
            }
        };
    }

    public override void OnGUI(MaterialEditor _materialEditor, MaterialProperty[] _properties)
    {
        base.OnGUI(editor, properties);

        DoRenderingMode();
        if (target.HasProperty("_TessellationUniform"))
        {
            DoTessellation();
        }
        if (target.HasProperty("_WireframeColor"))
        {
            DoWireframe();
        }
        DoMain();
        DoSecondary();
        DoAdvanced();
    }

    void DoRenderingMode()
    {
        RenderingMode mode = RenderingMode.Opaque;
        shouldShowAlphaCutoff = false;

        if (IsKeywordEnabled("_RENDERING_CUTOUT"))
        {
            mode = RenderingMode.Cutout;
            shouldShowAlphaCutoff = true;
        }
        else if (IsKeywordEnabled("_RENDERING_FADE"))
        {
            mode = RenderingMode.Fade;
        }
        else if (IsKeywordEnabled("_RENDERING_TRANSPARENT"))
        {
            mode = RenderingMode.Transparent;
        }

        EditorGUI.BeginChangeCheck();

        mode = (RenderingMode)EditorGUILayout.EnumPopup(MakeLabel("Rendering Mode"), mode);

        if (EditorGUI.EndChangeCheck())
        {
            RecordAction("Rendering Mode");
            SetKeyword("_RENDERING_CUTOUT", mode == RenderingMode.Cutout);
            SetKeyword("_RENDERING_FADE", mode == RenderingMode.Fade);
            SetKeyword("_RENDERING_TRANSPARENT", mode == RenderingMode.Transparent);

            RenderingSettings settings = RenderingSettings.modes[(int)mode];
            foreach (Material m in editor.targets)
            {
                m.renderQueue = (int)settings.queue;
                m.SetOverrideTag("RenderType", settings.renderType);
                m.SetInt("_SrcBlend", (int)settings.srcBlend);
                m.SetInt("_DstBlend", (int)settings.dstBlend);
                m.SetInt("_ZWrite", settings.zWrite ? 1 : 0);
            }
        }

        if(mode == RenderingMode.Fade || mode == RenderingMode.Transparent)
        {
            DoSemitransparentShadows();
        }

    }
    void DoTessellation()
    {
        GUILayout.Label("Tessellation", EditorStyles.boldLabel);
        EditorGUI.indentLevel += 2;

        TessellationMode mode = TessellationMode.Uniform;
        if (IsKeywordEnabled("_TESSELLATION_EDGE"))
        {
            mode = TessellationMode.Edge;
        }
        EditorGUI.BeginChangeCheck();
        mode = (TessellationMode)EditorGUILayout.EnumPopup(MakeLabel("Mode"), mode);
        if (EditorGUI.EndChangeCheck())
        {
            RecordAction("Tessellation Mode");
            SetKeyword("_TESSELLATION_EDGE", mode == TessellationMode.Edge);
        }

        if (mode == TessellationMode.Uniform)
        {
            editor.ShaderProperty(
                FindProperty("_TessellationUniform"),
                MakeLabel("Uniform")
            );
        }
        else
        {
            editor.ShaderProperty(
                FindProperty("_TessellationEdgeLength"),
                MakeLabel("Edge Length")
            );
        }
        EditorGUI.indentLevel -= 2;
    }
    void DoSemitransparentShadows()
    {
        GUIContent label = MakeLabel("Semitransp. Shadows", "Semitransparent Shadows");

        EditorGUI.BeginChangeCheck();

        bool semitransparentShadows = EditorGUILayout.Toggle
            (label, IsKeywordEnabled("_SEMITRANSPARENT_SHADOWS"));

        if(EditorGUI.EndChangeCheck())
        {
            SetKeyword("_SEMITRANSPARENT_SHADOWS", semitransparentShadows);
        }

        if (!semitransparentShadows)
        {
            shouldShowAlphaCutoff = true;
        }
    }

    void DoWireframe()
    {
        GUILayout.Label("Wireframe", EditorStyles.boldLabel);
        EditorGUI.indentLevel += 2;
        editor.ShaderProperty(
            FindProperty("_WireframeColor"),
            MakeLabel("Color")
        );
        editor.ShaderProperty(
            FindProperty("_WireframeSmoothing"),
            MakeLabel("Smoothing", "In screen space.")
        );
        editor.ShaderProperty(
            FindProperty("_WireframeThickness"),
            MakeLabel("Thickness", "In screen space.")
        );
        EditorGUI.indentLevel -= 2;
    }
    void DoMain()
    {
        GUILayout.Label("Main Maps", EditorStyles.boldLabel);

        MaterialProperty mainTex = FindProperty("_MainTex");
        GUIContent albedoLabel = MakeLabel(mainTex, "Albedo (RGB)");
        MaterialProperty color = FindProperty("_Color");


        editor.TexturePropertySingleLine(albedoLabel, mainTex, color);


        if (shouldShowAlphaCutoff)
        {
            DoAlphaCutoff();
        }
        DoMetallic();
        DoSmoothness();
        DoNormals();
        DoParallax();
        DoOcclusion();
        DoEmission();
        DoDetailMask();

        editor.TextureScaleOffsetProperty(mainTex);

    }
    void DoAlphaCutoff()
    {
        MaterialProperty alphaCutoff = FindProperty("_Cutoff");
        GUIContent aplhaCutoffLabel = MakeLabel(alphaCutoff);

        EditorGUI.indentLevel += 2;

        editor.ShaderProperty(alphaCutoff, aplhaCutoffLabel);

        EditorGUI.indentLevel -= 2;
    }
    void DoNormals()
    {
        MaterialProperty normalMap = FindProperty("_NormalMap");
        GUIContent normalLabel = MakeLabel(normalMap);
        MaterialProperty bumpScale = FindProperty("_BumpScale");

        Texture tex = normalMap.textureValue;

        EditorGUI.BeginChangeCheck();

        editor.TexturePropertySingleLine(normalLabel, normalMap, normalMap.textureValue ? bumpScale : null);

        if (EditorGUI.EndChangeCheck() && tex != normalMap.textureValue)
        {
            SetKeyword("_NORMAL_MAP", normalMap.textureValue);
        }
    }
    void DoMetallic()
    {
        MaterialProperty metalicMap = FindProperty("_MetallicMap");
        MaterialProperty metalicSlider = FindProperty("_Metallic");
        GUIContent metalicLabel = MakeLabel(metalicMap, "Metallic (R)");

        Texture tex = metalicMap.textureValue;

        EditorGUI.BeginChangeCheck();

        editor.TexturePropertySingleLine(metalicLabel, metalicMap, metalicMap.textureValue ? null : metalicSlider);

        if (EditorGUI.EndChangeCheck() && tex != metalicMap.textureValue)
        {
            SetKeyword("_METALLIC_MAP", metalicMap.textureValue);
        }
    }
    void DoSmoothness()
    {
        SmoothnessSource source = SmoothnessSource.Uniform;

        if (IsKeywordEnabled("_SMOOTHNESS_ALBEDO"))
        {
            source = SmoothnessSource.Albedo;
        }
        else if (IsKeywordEnabled("_SMOOTHNESS_METALLIC"))
        {
            source = SmoothnessSource.Metallic;
        }

        MaterialProperty smoothnessSlider = FindProperty("_Smoothness");
        GUIContent smoothnessLabel = MakeLabel(smoothnessSlider);

        EditorGUI.indentLevel += 2;
        editor.ShaderProperty(smoothnessSlider, smoothnessLabel);
        EditorGUI.indentLevel += 1;
        EditorGUI.BeginChangeCheck();
        source = (SmoothnessSource)EditorGUILayout.EnumPopup(("Source"), source);
        if (EditorGUI.EndChangeCheck())
        {
            RecordAction("Smoothness Source");
            SetKeyword("_SMOOTHNESS_ALBEDO", source == SmoothnessSource.Albedo);
            SetKeyword("_SMOOTHNESS_METALLIC", source == SmoothnessSource.Metallic);
        }
        EditorGUI.indentLevel -= 3;
    }
    void DoParallax()
    {
        MaterialProperty parallaxMap = FindProperty("_ParallaxMap");
        GUIContent parallaxLabel = MakeLabel(parallaxMap, "Parallax (G)");
        Texture tex = parallaxMap.textureValue;

        EditorGUI.BeginChangeCheck();
        editor.TexturePropertySingleLine(parallaxLabel, parallaxMap, 
            tex ? FindProperty("_ParallaxStrength") : null);

        if (EditorGUI.EndChangeCheck() && tex != parallaxMap.textureValue)
        {
            SetKeyword("_PARALLAX_MAP", parallaxMap.textureValue);
        }
    }
    void DoOcclusion()
    {
        MaterialProperty occlusionMap = FindProperty("_OcclusionMap");
        MaterialProperty occlusionSlider = FindProperty("_OcclusionStrenght");
        GUIContent occlusionLabel = MakeLabel(occlusionMap, "Occlusion (G)");

        Texture tex = occlusionMap.textureValue;

        EditorGUI.BeginChangeCheck();

        editor.TexturePropertySingleLine(occlusionLabel, occlusionMap, occlusionMap.textureValue ? occlusionSlider : null);

        if (EditorGUI.EndChangeCheck() && tex != occlusionMap.textureValue)
        {
            SetKeyword("_OCCLUSION_MAP", occlusionMap.textureValue);
        }
    }
    void DoEmission()
    {
        MaterialProperty emissionMap = FindProperty("_EmissionMap");
        MaterialProperty emissionColor = FindProperty("_Emission");
        GUIContent emissionLabel = MakeLabel("Emission (RGB)");

        Texture tex = emissionMap.textureValue;

        EditorGUI.BeginChangeCheck();

        editor.TexturePropertyWithHDRColor
            (emissionLabel, emissionMap, emissionColor, false);
        editor.LightmapEmissionProperty(2);

        if (EditorGUI.EndChangeCheck())
        {
            if (tex != emissionMap.textureValue)
            {
                SetKeyword("_EMISSION_MAP", emissionMap.textureValue);
            }

            foreach (Material m in editor.targets)
            {
                m.globalIlluminationFlags &=
                    ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }
    void DoDetailMask()
    {
        MaterialProperty detailMaskMap = FindProperty("_DetailMask");
        GUIContent detailMaskLabel = MakeLabel(detailMaskMap, "Detail Mask (A)");

        EditorGUI.BeginChangeCheck();

        editor.TexturePropertySingleLine(detailMaskLabel, detailMaskMap);

        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword("_DETAIL_MASK", detailMaskMap.textureValue);
        }
    }
    void DoSecondary()
    {
        GUILayout.Label("Secondary Maps", EditorStyles.boldLabel);

        MaterialProperty detailTex = FindProperty("_DetailTex");
        GUIContent detailLabel = MakeLabel(detailTex, "Albedo (RGB) multiplied by 2");

        EditorGUI.BeginChangeCheck();

        editor.TexturePropertySingleLine(detailLabel, detailTex);

        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword("_DETAIL_ALBEDO_MAP", detailTex.textureValue);
        }

        DoSecondaryNormals();

        editor.TextureScaleOffsetProperty(detailTex);

    }
    void DoSecondaryNormals()
    {
        MaterialProperty detailNormalMap = FindProperty("_DetailNormalMap");
        GUIContent detailNormalLabel = MakeLabel(detailNormalMap);

        Texture tex = detailNormalMap.textureValue;

        EditorGUI.BeginChangeCheck();

        editor.TexturePropertySingleLine(detailNormalLabel, detailNormalMap,
            detailNormalMap.textureValue ? FindProperty("_DetailBumpScale") : null);

        if (EditorGUI.EndChangeCheck() && tex != detailNormalMap.textureValue)
        {
            SetKeyword("_DETAIL_NORMAL_MAP", detailNormalMap.textureValue);
        }
    }
    void DoAdvanced()
    {
        GUILayout.Label("Advanced Options", EditorStyles.boldLabel);

        editor.EnableInstancingField();
    }

}
