Shader "Custom/GridShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        [Space]_LineWidth("Line Width",Float) = 0.1
        _Scale("Scale",Float) = 10
        _LineColor("Color",Color) = (1,1,1,1)
        _LineOffset("Line Offset",Vector) = (0,0,0,0)

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        float _LineWidth;
        float _Scale;
        float4 _LineColor;
        float2 _LineOffset;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        //iquilezles.org/www/articles/filterableprocedurals/filterableprocedurals.htm
        float filteredGrid(float2 p,float lineWidth)
        {
            float lw = lineWidth + 1;
            float2 w = fwidth(p);
            float2 a = p + 0.5 * w;
            float2 b = p - 0.5 * w;
            float2 i = (floor(a) + min(frac(a) * lw, 1.0) -
                floor(b) - min(frac(b) * lw, 1.0)) / (lw * w);
            return 1 - i.x * i.y;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            float l = filteredGrid(_LineOffset + IN.uv_MainTex * _Scale, _LineWidth) * _LineColor
                + filteredGrid(_LineOffset + IN.uv_MainTex * _Scale * 10, _LineWidth * 0.5) * _LineColor;
            o.Albedo += l;

            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
