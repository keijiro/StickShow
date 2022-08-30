using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;

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

        var job = new LegionJob()
          { matrices = _matrices, colors = _colors,
            config = _config, time = Time.time };

        job.Schedule(_config.TotalInstanceCount, 64).Complete();

        Profiler.EndSample();

        _colorBuffer.SetData(_colors);
        _material.SetBuffer("_InstanceColorBuffer", _colorBuffer);

        var rparams = new RenderParams(_material);
        var perDraw = _config.SquadInstanceCount;
        var i = 0;

        for (var sx = 0; sx < _config.squadCount.x; sx++)
            for (var sy = 0; sy < _config.squadCount.y; sy++, i += perDraw)
                Graphics.RenderMeshInstanced
                  (rparams, _mesh, 0, _matrices, perDraw, i);
    }
}
