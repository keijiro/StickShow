using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

[System.Serializable]
struct Audience
{
    #region Editable attributes

    public int2 seatPerBlock;
    public float2 seatPitch;
    [Space]
    public int2 blockCount;
    public float2 aisleWidth;
    [Space]
    public float swingFrequency;
    public float swingOffset;

    public static Audience Default()
      => new Audience()
        { seatPerBlock = math.int2(8, 12),
          seatPitch = math.float2(0.4f, 0.8f),
          blockCount = math.int2(7, 3),
          aisleWidth = math.float2(0.7f, 1.2f),
          swingFrequency = 0.5f,
          swingOffset = 0.3f };

    #endregion

    #region Helper functions

    public int BlockSeatCount
      => seatPerBlock.x * seatPerBlock.y;

    public int TotalSeatCount
      => seatPerBlock.x * seatPerBlock.y * blockCount.x * blockCount.y;

    public (int2 block, int2 seat) GetCoordinatesFromIndex(int i)
    {
        var si = i / BlockSeatCount;
        var pi = i - BlockSeatCount * si;
        var sy = si / blockCount.x;
        var sx = si - blockCount.x * sy;
        var py = pi / seatPerBlock.x;
        var px = pi - seatPerBlock.x * py;
        return (math.int2(sx, sy), math.int2(px, py));
    }

    public float2 GetPositionOnPlane(int2 block, int2 seat)
      => seatPitch * (seat - (float2)(seatPerBlock - 1) * 0.5f)
          + (seatPitch * (seatPerBlock - 1) + aisleWidth)
            * (block - (float2)(blockCount - 1) * 0.5f);

    #endregion

    #region Stick animation

    public float4x4 GetStickMatrix
      (float2 pos, float4x4 xform, float time, uint seed)
    {
        var rand = new Random(seed);
        rand.NextUInt4();

        // Cyclic animation phase parameter
        var phase = 2 * math.PI * swingFrequency * time;
        var nr1 = rand.NextFloat(-1000, 1000);
        phase += noise.snoise(math.float2(nr1, time * 0.27f));

        // Animation origin (shoulder position)
        var origin = float3.zero;
        origin.xz = pos + rand.NextFloat2(-0.3f, 0.3f) * seatPitch;
        origin.y = rand.NextFloat(-0.2f, 0.2f);

        // Swing angle
        var angle = math.cos(phase);
        var angle_unsmooth = math.smoothstep(-1, 1, angle) * 2 - 1;
        angle = math.lerp(angle, angle_unsmooth, rand.NextFloat());
        angle *= rand.NextFloat(0.3f, 1.0f);

        // Swing axis
        var nr2 = rand.NextFloat(-1000, 1000);
        var dx = noise.snoise(math.float2(nr2, time * 0.23f + 100));
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
struct AudienceAnimationJob : IJobParallelFor
{
    // Input
    public Audience config;
    public Matrix4x4 xform;
    public float time;

    // Output
    public NativeSlice<Matrix4x4> matrices;
    public NativeSlice<Color> colors;

    public void Execute(int i)
    {
        var (block, seat) = config.GetCoordinatesFromIndex(i);
        var pos = config.GetPositionOnPlane(block, seat);
        var seed = (uint)i * 2u + 123u;
        matrices[i] = config.GetStickMatrix(pos, xform, time, seed++);
        colors[i] = config.GetStickColor(pos, time, seed++);
    }
}
