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
    [SerializeField] Vector2 _personInterval = new Vector2(1.0f, 1.5f);
    [Space]
    [SerializeField] Vector2Int _squadCount = new Vector2Int(4, 4);
    [SerializeField] Vector2Int _squadInterval = new Vector2Int(2, 3);
    [Space]
    [SerializeField] float _swingFrequency = 1.0f;

    NativeArray<Matrix4x4> _matrices;
    NativeArray<Color> _colors;
    GraphicsBuffer _colorBuffer;

    void Start()
    {
        var icount = _squadSize.x * _squadSize.y *
                     _squadCount.x * _squadCount.y;

        _matrices = new NativeArray<Matrix4x4>
          (icount, Allocator.Persistent,
           NativeArrayOptions.UninitializedMemory);

        _colors = new NativeArray<Color>
          (icount, Allocator.Persistent,
           NativeArrayOptions.UninitializedMemory);

        _colorBuffer = new GraphicsBuffer
          (GraphicsBuffer.Target.Structured, icount, sizeof(float) * 4);
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
        return new Color(math.frac(x * 0.9f), math.frac(z * 0.9f), 1, 1) * 20;
    }

    void Update()
    {
        var offs = 0;

        for (var sx = 0; sx < _squadCount.x; sx++)
        {
            for (var sy = 0; sy < _squadCount.y; sy++)
            {
                for (var px = 0; px < _squadSize.x; px++)
                {
                    for (var py = 0; py < _squadSize.y; py++)
                    {
                        var xi = sx * _squadSize.x + px;
                        var yi = sy * _squadSize.y + py;

                        var x = _personInterval.x * (xi - (_squadCount.x * _squadSize.x - 1) * 0.5f);
                        var y = _personInterval.y * (yi - (_squadCount.y * _squadSize.y - 1) * 0.5f);

                        _matrices[offs] = GetStickMatrix(x, y);
                        _colors[offs] = GetColor(x, y);

                        offs++;
                    }
                }
            }
        }

        _colorBuffer.SetData(_colors);
        _material.SetBuffer("_InstanceColorBuffer", _colorBuffer);

        var rparams = new RenderParams(_material);
        var perDraw = _squadSize.x * _squadSize.y;
        offs = 0;

        for (var sx = 0; sx < _squadCount.x; sx++)
        {
            for (var sy = 0; sy < _squadCount.y; sy++)
            {
                Graphics.RenderMeshInstanced(rparams, _mesh, 0, _matrices, perDraw, offs);
                offs += perDraw;
            }
        }
    }
}
