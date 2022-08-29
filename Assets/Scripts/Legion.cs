using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;

[System.Serializable, BurstCompile]
struct Legion
{
    public Vector2Int squadSize;
    public Vector2 seatInterval;
    [Space]
    public Vector2Int squadCount;
    public Vector2 squadInterval;
    [Space]
    public float swingFrequency;
    public float swingOffset;

    public static Legion Default()
      => new Legion()
        { squadSize = new Vector2Int(8, 12),
          seatInterval = new Vector2(0.5f, 0.9f),
          squadCount = new Vector2Int(7, 3),
          squadInterval = new Vector2(0.8f, 1.2f),
          swingFrequency = 0.5f,
          swingOffset = 0.2f };

    public int SquadInstanceCount
      => squadSize.x * squadSize.y;

    public int TotalInstanceCount
      => squadSize.x * squadSize.y * squadCount.x * squadCount.y;

    public float4x4 GetStickMatrix
      (int sx, int sy, int px, int py, float time)
    {
        float4x4 temp;
        CalculateStickMatrixBurst(this, sx, sy, px, py, time, out temp);
        return temp;
    }

    [BurstCompile]
    public static void CalculateStickMatrixBurst
      (in Legion cfg,
       int sx, int sy, int px, int py, float time,
       out float4x4 matrix)
    {
        var x = cfg.seatInterval.x * (px - (cfg.squadSize.x - 1) * 0.5f);
        var y = cfg.seatInterval.y * (py - (cfg.squadSize.y - 1) * 0.5f);

        x += (cfg.seatInterval.x * (cfg.squadSize.x - 1) + cfg.squadInterval.x) * (sx - (cfg.squadCount.x - 1) * 0.5f);
        y += (cfg.seatInterval.y * (cfg.squadSize.y - 1) + cfg.squadInterval.y) * (sy - (cfg.squadCount.y - 1) * 0.5f);

        var phase = 2 * math.PI * cfg.swingFrequency * time;
        var ntime = time * 0.234f;
        var nvalue1 = noise.snoise(math.float3(x, ntime, y));
        var nvalue2 = noise.snoise(math.float3(x, ntime + 100, y));
        var angle = math.cos(phase + nvalue1);
        var origin = math.float3(x, 0, y);
        var axis = math.normalize(math.float3(nvalue2, 0, 1));
        var m1 = float4x4.Translate(origin);
        var m2 = float4x4.AxisAngle(axis, angle);
        var m3 = float4x4.Translate(math.float3(0, cfg.swingOffset, 0));
        matrix = math.mul(math.mul(m1, m2), m3);
    }

    public Color GetStickColor(int sx, int sy, int px, int py, float time)
    {
        var r = math.frac(px * 0.13f) * 80;
        var g = math.frac(py * 0.31f) * 80;
        return new Color(r, g, 1, 1);
    }
}
