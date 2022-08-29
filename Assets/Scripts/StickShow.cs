using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;

sealed class StickShow : MonoBehaviour
{
    [SerializeField] Mesh _mesh = null;
    [SerializeField] Material _material = null;
    [SerializeField] Legion _config = Legion.Default();

    NativeArray<Matrix4x4> _matrices;
    NativeArray<Color> _colors;
    GraphicsBuffer _colorBuffer;

    void Start()
    {
        _matrices = new NativeArray<Matrix4x4>
          (_config.TotalInstanceCount, Allocator.Persistent,
           NativeArrayOptions.UninitializedMemory);

        _colors = new NativeArray<Color>
          (_config.TotalInstanceCount, Allocator.Persistent,
           NativeArrayOptions.UninitializedMemory);

        _colorBuffer = new GraphicsBuffer
          (GraphicsBuffer.Target.Structured,
           _config.TotalInstanceCount, sizeof(float) * 4);
    }

    void OnDestroy()
    {
        _matrices.Dispose();
        _colors.Dispose();
        _colorBuffer.Dispose();
    }

    void Update()
    {
        Profiler.BeginSample("Stick Update");

        var i = 0;

        for (var sxi = 0; sxi < _config.squadCount.x; sxi++)
        {
            for (var syi = 0; syi < _config.squadCount.y; syi++)
            {
                for (var pxi = 0; pxi < _config.squadSize.x; pxi++)
                {
                    for (var pyi = 0; pyi < _config.squadSize.y; pyi++, i++)
                    {
                        _matrices[i] =
                          _config.GetStickMatrix(sxi, syi, pxi, pyi, Time.time);

                        _colors[i] =
                          _config.GetStickColor(sxi, syi, pxi, pyi, Time.time);
                    }
                }
            }
        }

        Profiler.EndSample();

        _colorBuffer.SetData(_colors);
        _material.SetBuffer("_InstanceColorBuffer", _colorBuffer);

        var rparams = new RenderParams(_material);
        var perDraw = _config.SquadInstanceCount;
        i = 0;

        for (var sx = 0; sx < _config.squadCount.x; sx++)
            for (var sy = 0; sy < _config.squadCount.y; sy++, i += perDraw)
                Graphics.RenderMeshInstanced
                  (rparams, _mesh, 0, _matrices, perDraw, i);
    }
}
