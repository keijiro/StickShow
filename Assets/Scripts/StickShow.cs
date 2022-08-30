using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;

sealed class StickShow : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] Mesh _mesh = null;
    [SerializeField] Material _material = null;
    [SerializeField] Legion _legion = Legion.Default();

    #endregion

    #region Private objects

    NativeArray<Matrix4x4> _matrices;
    NativeArray<Color> _colors;
    GraphicsBuffer _colorBuffer;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _matrices = new NativeArray<Matrix4x4>
          (_legion.TotalSeatCount, Allocator.Persistent,
           NativeArrayOptions.UninitializedMemory);

        _colors = new NativeArray<Color>
          (_legion.TotalSeatCount, Allocator.Persistent,
           NativeArrayOptions.UninitializedMemory);

        _colorBuffer = new GraphicsBuffer
          (GraphicsBuffer.Target.Structured,
           _legion.TotalSeatCount, sizeof(float) * 4);
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
          { config = _legion, time = Time.time,
            matrices = _matrices, colors = _colors };
        job.Schedule(_legion.TotalSeatCount, 64).Complete();

        Profiler.EndSample();

        _colorBuffer.SetData(_colors);
        _material.SetBuffer("_InstanceColorBuffer", _colorBuffer);

        var rparams = new RenderParams(_material);
        var (i, step) = (0, _legion.SquadSeatCount);
        for (var sx = 0; sx < _legion.squadCount.x; sx++)
            for (var sy = 0; sy < _legion.squadCount.y; sy++, i += step)
                Graphics.RenderMeshInstanced
                  (rparams, _mesh, 0, _matrices, step, i);
    }

    #endregion
}
