using System;
using System.Numerics;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// Per-pull randomization mirrors WCGH FRU-Sim:
// - one of four cardinal controller anchors
// - one of four quarter-turn rotations
// - east or west exawave is highlighted first
public sealed class FruP5FulgentBladeState
{
    private readonly Rng rng = new();

    public int AnchorIndex { get; }
    public int QuarterTurn { get; }
    public bool EastFirst { get; }

    public Vector3 ControllerPosition
    {
        get
        {
            var p = FruConstants.Geometry.ControllerAnchors[AnchorIndex];
            return new Vector3(p.X, 0f, p.Z);
        }
    }

    public float ControllerRotation => QuarterTurn * MathF.PI / 2f;

    public FruP5FulgentBladeState()
    {
        AnchorIndex = rng.NextInt(4);
        QuarterTurn = rng.NextInt(4);
        EastFirst = rng.NextBool();
    }

    // Rotation convention matches AnoMech: 0 = +Z (south), +pi/2 = +X (east).
    // This is also the transform needed for WCGH's Vector2.rotated(-controllerYaw).
    public Vector3 Rotate(Vector3 v)
    {
        var a = ControllerRotation;
        var c = MathF.Cos(a);
        var s = MathF.Sin(a);
        return new Vector3(v.X * c + v.Z * s, v.Y, -v.X * s + v.Z * c);
    }

    public Vector3 ToControllerSpace(Vector3 local) => ControllerPosition + Rotate(local);
}
