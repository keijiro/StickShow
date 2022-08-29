#if UNITY_ANY_INSTANCING_ENABLED

StructuredBuffer<float4> _InstanceColorBuffer;

void GetInstanceColor_float(float id, out float4 color)
{
    color = _InstanceColorBuffer[(uint)id];
}

#else

void GetInstanceColor_float(float id, out float4 color)
{
    color = float4(1, 0, 0, 1);
}

#endif
