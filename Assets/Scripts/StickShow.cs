using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;

sealed class StickShow : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] Mesh _mesh = null;
    [SerializeField] Material _material = null;
    [SerializeField] Audience _audience = Audience.Default();

    #endregion

    #region Private objects

    NativeArray<Matrix4x4> _matrices;
    NativeArray<Color> _colors;
    GraphicsBuffer _colorBuffer;
    MaterialPropertyBlock _matProps;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _matrices = new NativeArray<Matrix4x4>
          (_audience.TotalSeatCount, Allocator.Persistent,
           NativeArrayOptions.UninitializedMemory);

        _colors = new NativeArray<Color>
          (_audience.TotalSeatCount, Allocator.Persistent,
           NativeArrayOptions.UninitializedMemory);

        _colorBuffer = new GraphicsBuffer
          (GraphicsBuffer.Target.Structured,
           _audience.TotalSeatCount, sizeof(float) * 4);

        _matProps = new MaterialPropertyBlock();
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

        var job = new AudienceAnimationJob()
          { config = _audience, xform = transform.localToWorldMatrix,
            time = Time.time, matrices = _matrices, colors = _colors };
        job.Schedule(_audience.TotalSeatCount, 64).Complete();

        Profiler.EndSample();

        _colorBuffer.SetData(_colors);
        _material.SetBuffer("_InstanceColorBuffer", _colorBuffer);

        var rparams = new RenderParams(_material) { matProps = _matProps };
        var (i, step) = (0, _audience.BlockSeatCount);
        for (var sx = 0; sx < _audience.blockCount.x; sx++)
        {
            for (var sy = 0; sy < _audience.blockCount.y; sy++, i += step)
            {
                _matProps.SetInteger("_InstanceIDOffset", i);
                Graphics.RenderMeshInstanced
                  (rparams, _mesh, 0, _matrices, step, i);
            }
        }
    }

    #endregion
}
