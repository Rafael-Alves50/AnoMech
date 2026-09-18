using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

public readonly record struct FruFulgentLine(Vector3 Position, float Heading, int Index);
public readonly record struct FruFulgentStripe(
    float SnapshotTime,
    Vector3 Edge,
    Vector3 Move,
    bool IsLight,
    int LineIndex,
    int Step);
public readonly record struct FruFulgentSafePoint(float SnapshotTime, Vector3 Position);

// Per-pull Fulgent Blade randomization.
//
// The real pattern is determined by three independent choices:
// - which cardinal direction the octagram centre is offset toward
// - which of eight directions the first glowing line uses
// - clockwise vs counter-clockwise activation order
public sealed class FruP5FulgentBladeState
{
    private readonly Rng rng = new();

    public int CenterDirection { get; }
    public int FirstLineDirection { get; }
    public bool Clockwise { get; }

    public IReadOnlyList<FruFulgentLine> Lines { get; }
    public IReadOnlyList<FruFulgentStripe> Stripes { get; }
    public IReadOnlyList<FruFulgentSafePoint> BotSafePath { get; }

    public FruP5FulgentBladeState()
    {
        CenterDirection = rng.NextInt(4);
        FirstLineDirection = rng.NextInt(8);
        Clockwise = rng.NextBool();

        Lines = BuildLines();
        Stripes = BuildStripes();
        BotSafePath = BuildBotSafePath();
    }

    // Reconstruct the six Fulgent Blade line actors in scenario-local coordinates.
    // +X east, +Z south, heading 0 = south.
    private IReadOnlyList<FruFulgentLine> BuildLines()
    {
        var theta = CenterDirection * MathF.PI / 2f - MathF.PI;
        var starCenter = FruConstants.Geometry.StarCenterRadius *
                         new Vector3(MathF.Sin(theta), 0f, MathF.Cos(theta));

        var result = new List<FruFulgentLine>(6);
        for (var i = 0; i < 6; i++)
        {
            var rawDir = FirstLineDirection + (Clockwise ? -i : i);
            var heading = rawDir * MathF.PI / 4f - MathF.PI;
            var sin = MathF.Sin(heading);
            var cos = MathF.Cos(heading);

            // Start from an octagram edge centre, then project that point onto the
            // radial line for this heading. This is the position of the real line actor.
            var pos = starCenter + 10f * new Vector3(sin, 0f, cos);
            var perpendicular = new Vector3(cos, 0f, -sin);
            var shift = pos.Z * sin - pos.X * cos;
            pos += shift * perpendicular;

            // Adjacent base lines alternate which side is Light vs Darkness.
            if (rawDir % 2 == 0)
                heading += MathF.PI;

            result.Add(new FruFulgentLine(pos, heading, i));
        }

        return result;
    }

    private IReadOnlyList<FruFulgentStripe> BuildStripes()
    {
        var stripes = new List<FruFulgentStripe>();

        foreach (var line in Lines)
        {
            // Lines glow/activate in pairs: 0+1 at 10s, 2+3 at 14s, 4+5 at 18s.
            var activation = 10f + 4f * (line.Index / 2);

            AddFront(stripes, line, activation, isLight: true, line.Heading);
            AddFront(stripes, line, activation, isLight: false, line.Heading + MathF.PI);
        }

        return stripes
            .OrderBy(s => s.SnapshotTime)
            .ThenBy(s => s.LineIndex)
            .ThenBy(s => s.IsLight ? 0 : 1)
            .ToArray();
    }

    private static void AddFront(
        List<FruFulgentStripe> stripes,
        FruFulgentLine line,
        float activation,
        bool isLight,
        float heading)
    {
        var move = new Vector3(MathF.Sin(heading), 0f, MathF.Cos(heading));

        // The first 7s cast snapshots about 0.2s before the visible hit. Rest
        // actions repeat every 2s, advancing by 5y, until their source leaves the arena.
        for (var step = 0; step < 10; step++)
        {
            var edge = line.Position + move * (FruConstants.Geometry.ExalineStep * step);
            if (step > 0 && HorizontalLength(edge) > FruConstants.Geometry.ExalineActiveRadius)
                break;

            stripes.Add(new FruFulgentStripe(
                activation + 6.8f + 2f * step,
                edge,
                move,
                isLight,
                line.Index,
                step));
        }
    }

    // Development bot path: choose the nearest safe point for every unique
    // snapshot bundle. This keeps the fake party mechanically valid without
    // claiming a regional strat before we explicitly implement/verify one.
    private IReadOnlyList<FruFulgentSafePoint> BuildBotSafePath()
    {
        var path = new List<FruFulgentSafePoint>();
        var previous = Vector3.Zero;

        foreach (var bundle in Stripes.GroupBy(s => (int)MathF.Round(s.SnapshotTime * 10f)).OrderBy(g => g.Key))
        {
            var safe = FindNearestSafe(previous, bundle);
            path.Add(new FruFulgentSafePoint(bundle.Key / 10f, safe));
            previous = safe;
        }

        return path;
    }

    private static Vector3 FindNearestSafe(
        Vector3 previous,
        IEnumerable<FruFulgentStripe> bundle)
    {
        Vector3? best = null;
        var bestScore = float.MaxValue;
        var stripes = bundle.ToArray();

        // 1y grid is plenty for bot choreography and leaves substantial clearance.
        for (var x = -20; x <= 20; x++)
        for (var z = -20; z <= 20; z++)
        {
            var p = new Vector3(x, 0f, z);
            if (HorizontalLength(p) > 20f) continue;
            if (stripes.Any(s => Contains(s, p, 0.75f))) continue;

            var score = Vector3.Distance(previous, p) + 0.01f * HorizontalLength(p);
            if (score >= bestScore) continue;
            best = p;
            bestScore = score;
        }

        return best ?? previous;
    }

    public static bool Contains(FruFulgentStripe stripe, Vector3 point, float margin = 0f)
    {
        var delta = point - stripe.Edge;
        var along = delta.X * stripe.Move.X + delta.Z * stripe.Move.Z;
        return along >= -margin &&
               along <= FruConstants.Geometry.ExalineStep + margin;
    }

    private static float HorizontalLength(Vector3 p) => MathF.Sqrt(p.X * p.X + p.Z * p.Z);
}
