Shader "Obsession/Corridor"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Varyings { float4 position : SV_POSITION; half4 color : COLOR; };
            Varyings Vert(float3 position : POSITION, half4 color : COLOR)
            {
                Varyings o;
                o.position = TransformObjectToHClip(position);
                o.color = color;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target { return i.color; }
            ENDHLSL
        }
    }
}
