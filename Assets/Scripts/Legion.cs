using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[System.Serializable]
struct Legion
{
    #region Editable attributes

    public int2 squadSize;
    public float2 seatInterval;
    [Space]
    public int2 squadCount;
    public float2 squadInterval;
    [Space]
    public float swingFrequency;
    public float swingOffset;

    public static Legion Default()
      => new Legion()
        { squadSize = math.int2(8, 12),
          seatInterval = math.float2(0.5f, 0.9f),
          squadCount = math.int2(7, 3),
          squadInterval = math.float2(0.8f, 1.2f),
          swingFrequency = 0.5f,
          swingOffset = 0.2f };

    #endregion

    #region Helper functions

    public int SquadInstanceCount
      => squadSize.x * squadSize.y;

    public int TotalInstanceCount
      => squadSize.x * squadSize.y * squadCount.x * squadCount.y;

    public (int2 squad, int2 seat) GetCoordinates(int index)
    {
        var si = index / SquadInstanceCount;
        var pi = index - SquadInstanceCount * si;
        var sy = si / squadCount.x;
        var sx = si - squadCount.x * sy;
        var py = pi / squadSize.x;
        var px = pi - squadSize.x * py;
        return (math.int2(sx, sy), math.int2(px, py));
    }

    public float2 GetPosition(int2 squad, int2 seat)
      => seatInterval * (seat - (float2)(squadSize - 1) * 0.5f)
          + (seatInterval * (squadSize - 1) + squadInterval)
            * (squad - (float2)(squadCount - 1) * 0.5f);

    #endregion

    #region Stick animation

    public float4x4 GetStickMatrix(float2 pos, float time)
    {
        var phase = 2 * math.PI * swingFrequency * time;
        var ntime = time * 0.234f;
        var nvalue1 = noise.snoise(math.float3(pos.x, ntime, pos.y));
        var nvalue2 = noise.snoise(math.float3(pos.x, ntime + 100, pos.y));
        var angle = math.cos(phase + nvalue1);
        var origin = math.float3(pos.x, 0, pos.y);
        var axis = math.normalize(math.float3(nvalue2, 0, 1));
        var m1 = float4x4.Translate(origin);
        var m2 = float4x4.AxisAngle(axis, angle);
        var m3 = float4x4.Translate(math.float3(0, swingOffset, 0));
        return math.mul(math.mul(m1, m2), m3);
    }

    public Color GetStickColor(float2 pos, float time)
    {
        var hue = math.frac(math.sin(pos.x * 23.13f + pos.y * 134.782f) * 44.583f);
        return Color.HSVToRGB(hue, 1, 30);
    }

    #endregion
}

[BurstCompile]
struct LegionJob : IJobParallelFor
{
    // Input
    public Legion config;
    public float time;

    // Output
    public NativeSlice<Matrix4x4> matrices;
    public NativeSlice<Color> colors;

    // Job bridge
    public void Execute(int i)
    {
        var (squad, seat) = config.GetCoordinates(i);
        var pos = config.GetPosition(squad, seat);
        matrices[i] = config.GetStickMatrix(pos, time);
        colors[i] = config.GetStickColor(pos, time);
    }
}
