using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

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
          seatInterval = math.float2(0.4f, 0.8f),
          squadCount = math.int2(7, 3),
          squadInterval = math.float2(0.7f, 1.2f),
          swingFrequency = 0.5f,
          swingOffset = 0.3f };

    #endregion

    #region Helper functions

    public int SquadSeatCount
      => squadSize.x * squadSize.y;

    public int TotalSeatCount
      => squadSize.x * squadSize.y * squadCount.x * squadCount.y;

    public (int2 squad, int2 seat) GetCoordinatesFromIndex(int i)
    {
        var si = i / SquadSeatCount;
        var pi = i - SquadSeatCount * si;
        var sy = si / squadCount.x;
        var sx = si - squadCount.x * sy;
        var py = pi / squadSize.x;
        var px = pi - squadSize.x * py;
        return (math.int2(sx, sy), math.int2(px, py));
    }

    public float2 GetPositionOnPlane(int2 squad, int2 seat)
      => seatInterval * (seat - (float2)(squadSize - 1) * 0.5f)
          + (seatInterval * (squadSize - 1) + squadInterval)
            * (squad - (float2)(squadCount - 1) * 0.5f);

    #endregion

    #region Stick animation

    public float4x4 GetStickMatrix
      (float2 pos, float4x4 xform, float time, uint seed)
    {
        var rand = new Random(seed);
        rand.NextUInt4();

        // Cyclic animation phase parameter
        var phase = 2 * math.PI * swingFrequency * time;
        phase += noise.snoise(math.float3(pos, time * 0.57f));

        // Animation origin (shoulder position)
        var origin = float3.zero;
        origin.xz = pos + rand.NextFloat2(-0.3f, 0.3f) * seatInterval;
        origin.y = rand.NextFloat(-0.2f, 0.2f);

        // Swing angle
        var angle = math.cos(phase);
        var angle_unsmooth = math.smoothstep(-1, 1, angle) * 2 - 1;
        angle = math.lerp(angle, angle_unsmooth, rand.NextFloat());
        angle *= rand.NextFloat(0.3f, 1.0f);

        // Swing axis
        var dx = noise.snoise(math.float3(pos.yx, time * 0.23f + 100));
        var axis = math.normalize(math.float3(dx, 0, 1));

        // Stick offset (arm length)
        var offset = swingOffset * rand.NextFloat(0.75f, 1.25f);

        // Matrix composition
        var m1 = float4x4.Translate(origin);
        var m2 = float4x4.AxisAngle(axis, angle);
        var m3 = float4x4.Translate(math.float3(0, offset, 0));
        return math.mul(math.mul(math.mul(xform, m1), m2), m3);
    }

    public Color GetStickColor(float2 pos, float time, uint seed)
    {
        var rand = new Random(seed);
        rand.NextUInt4();

        // Wave animation
        var wave = math.distance(pos, math.float2(0, 16));
        wave = math.sin(wave * 0.53f - time * 2.8f) * 0.5f + 0.5f;

        // Hue / brightness
        var hue = math.frac(rand.NextFloat() + time * 0.83f);
        var br = wave * wave * 50 + 0.1f;

        return Color.HSVToRGB(hue, 1, br);
    }

    #endregion
}

[BurstCompile]
struct LegionJob : IJobParallelFor
{
    // Input
    public Legion config;
    public Matrix4x4 xform;
    public float time;

    // Output
    public NativeSlice<Matrix4x4> matrices;
    public NativeSlice<Color> colors;

    public void Execute(int i)
    {
        var (squad, seat) = config.GetCoordinatesFromIndex(i);
        var pos = config.GetPositionOnPlane(squad, seat);
        var seed = (uint)i * 2u + 123u;
        matrices[i] = config.GetStickMatrix(pos, xform, time, seed++);
        colors[i] = config.GetStickColor(pos, time, seed++);
    }
}
