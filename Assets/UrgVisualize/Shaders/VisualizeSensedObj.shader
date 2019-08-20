Shader "Unlit/VisualizeSensedObj"
{
	Properties{
		_Color("color", Color) = (1,1,1,1)
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

			half4 _Color;
			uniform StructuredBuffer<float3> _VBuffer;
			uniform int _VCount;

            v2f vert (appdata v, uint iidx : SV_InstanceID, uint vidx : SV_VertexID)
            {
				v.vertex.xyz = _VBuffer[vidx + iidx*_VCount].xyz;

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
