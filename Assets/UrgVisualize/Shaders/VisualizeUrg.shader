Shader "Unlit/VisualizeUrg"
{
    Properties
    {
		_UrgPos ("sensor local position(x,y), urgAngle", Vector) = (0,0,0,0)
		_UrgProps ("prop(steps, angleOffset(rad), angleDelta(rad))", Vector) = (0,0,0,0)
		_AreaProps ("sensing area props(width, height)", Vector) = (1,1,0,0)
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
			#pragma target 5.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
				float2 lPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			StructuredBuffer<float> _UrgData;
			float4 _UrgPos,_UrgProps,_AreaProps;

            v2f vert (appdata v)
            {
				v.vertex.xy *= _AreaProps.xy;

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.lPos = v.vertex.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float2 vec = i.lPos - _UrgPos.xy;
				float angle = atan2(vec.y,vec.x) - _UrgPos.z;
				//float angle = acos(dot(normalize(vec), float2(1,0)))*sign(vec.y) - _UrgPos.z;
				float dist = length(vec);

				angle += (angle < -UNITY_PI)? 2*UNITY_PI:0;
				angle -= (UNITY_PI < angle) ? 2*UNITY_PI:0;

				float t = angle + _UrgProps.y;
				t *= _UrgProps.z;

				int step = floor(t);
				t = frac(t);
				float d0 = _UrgData[step-1];
				float d1 = _UrgData[step];
				float delta = d1 - d0;

				float data = lerp(d0, lerp(d0,d1,t), abs(delta) < 0.2);

                return  (dist < data) * (half4(1,1,1,1) - half4(0,1,1,0) * (delta < -0.2) - half4(1,1,0,0) * (0.2 < delta));
            }
            ENDCG
        }
    }
}
