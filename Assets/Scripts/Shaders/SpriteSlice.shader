Shader "FamilyNinja/SpriteSlice"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _CutPoint ("Cut Point", Vector) = (0,0,0,0)
        [HideInInspector] _CutNormal ("Cut Normal", Vector) = (0,1,0,0)
        [HideInInspector] _KeepSide ("Keep Side", Float) = 1
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SliceVert
            #pragma fragment SliceFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float2 _CutPoint;
            float2 _CutNormal;
            float _KeepSide;

            struct SliceVaryings
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                float2 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            SliceVaryings SliceVert(appdata_t input)
            {
                SliceVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor;
                output.localPosition = input.vertex.xy;
                return output;
            }

            fixed4 SliceFrag(SliceVaryings input) : SV_Target
            {
                float signedDistance = dot(input.localPosition - _CutPoint, _CutNormal);
                clip(signedDistance * _KeepSide);
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
