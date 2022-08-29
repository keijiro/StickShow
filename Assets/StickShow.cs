using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

sealed class StickShow : MonoBehaviour
{
    [SerializeField] Mesh _mesh = null;
    [SerializeField] Vector3 _meshScale = new Vector3(0.1f, 0.25f, 0.1f);
    [SerializeField] Material _material = null;
    [Space]
    [SerializeField] Vector2Int _squadSize = new Vector2Int(8, 12);
    [SerializeField] Vector2 _interval = new Vector2(1.0f, 1.5f);
    [Space]
    [SerializeField] float _swingFrequency = 1.0f;

    NativeArray<Matrix4x4> _matrices;
    NativeArray<Color> _colors;
    GraphicsBuffer _colorBuffer;

    void Start()
    {
        var instanceCount = _squadSize.x * _squadSize.y;

        _matrices = new NativeArray<Matrix4x4>
          (instanceCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        _colors = new NativeArray<Color>
          (instanceCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        _colorBuffer = new GraphicsBuffer
          (GraphicsBuffer.Target.Structured, instanceCount, sizeof(float) * 4);
    }

    void OnDestroy()
    {
        _matrices.Dispose();
        _colors.Dispose();
        _colorBuffer.Dispose();
    }

    float4x4 GetStickMatrix(float x, float z)
    {
        var phase = 2 * math.PI * _swingFrequency * Time.time;
        var ntime = Time.time * 0.234f;
        var nvalue1 = noise.snoise(math.float3(x, ntime, z));
        var nvalue2 = noise.snoise(math.float3(x, ntime + 100, z));
        var angle = math.cos(phase + nvalue1);
        var origin = math.float3(x, 0, z);
        var offset = math.float3(0, _meshScale.y * 2, 0);
        var axis = math.normalize(math.float3(nvalue2, 0, 1));
        var m1 = float4x4.Translate(origin);
        var m2 = float4x4.AxisAngle(axis, angle);
        var m3 = float4x4.Translate(offset);
        var m4 = float4x4.Scale(_meshScale);
        return math.mul(math.mul(math.mul(m1, m2), m3), m4);
    }

    Color GetColor(float x, float z)
    {
        return new Color(math.frac(x * 0.9f), math.frac(z * 0.9f), 1, 1);
    }

    void Update()
    {
        var mc = 0;
        for (var i = 0; i < _squadSize.y; i++)
        {
            var z = _interval.y * (i - (_squadSize.y - 1) * 0.5f);
            for (var j = 0; j < _squadSize.x; j++)
            {
                var x = _interval.x * (j - (_squadSize.x - 1) * 0.5f);
                _matrices[mc] = GetStickMatrix(x, z);
                _colors[mc] = GetColor(x, z);
                mc++;
            }
        }

        _colorBuffer.SetData(_colors);
        _material.SetBuffer("_InstanceColorBuffer", _colorBuffer);

        var rparams = new RenderParams(_material);
        Graphics.RenderMeshInstanced(rparams, _mesh, 0, _matrices, _matrices.Length);
    }
}
