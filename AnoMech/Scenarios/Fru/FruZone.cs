using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru;

public sealed class FruZone : IZone
{
    public static readonly FruZone Instance = new();

    // FRU P5 phase environment, matching the encounter's client-side state:
    // weather 108, BGM 20099, and only MapEffect slot 0x2F enabled.
    public static readonly Phase P5 = new(Instance, "P5: Pandora", 108, 20099, InitP5Arena);

    public string Name => "The Futures Rewritten";
    public uint TerritoryId => 1238;
    public Vector3 Origin => new(100f, 0f, 100f);
    public byte Level => FruConstants.Level;

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
    [
        new WaymarkLayout("NAUR / Standard",
        [
            new(WaymarkSlot.A,     new Vector3(     0f, 0f,    -10f)),
            new(WaymarkSlot.B,     new Vector3(    10f, 0f,      0f)),
            new(WaymarkSlot.C,     new Vector3(     0f, 0f,     10f)),
            new(WaymarkSlot.D,     new Vector3(   -10f, 0f,      0f)),
            new(WaymarkSlot.One,   new Vector3( -7.07f, 0f,  -7.07f)),
            new(WaymarkSlot.Two,   new Vector3(  7.07f, 0f,  -7.07f)),
            new(WaymarkSlot.Three, new Vector3(  7.07f, 0f,   7.07f)),
            new(WaymarkSlot.Four,  new Vector3( -7.07f, 0f,   7.07f)),
        ])
    ];

    // FRU's earlier-phase centrepiece can retain a SharedGroup collider after
    // switching map state client-side. Drop spawn-area colliders around centre
    // so P5 movement is unobstructed.
    public IReadOnlyList<Vector3> ColliderRemovalPoints => [Vector3.Zero];

    public void Run(SimWorld world) => world.EnforceArenaBoundary(FruConstants.Geometry.ArenaRadius);

    private static void InitP5Arena(SimWorld world) => world.Events.Add(1f, () =>
    {
        // FRU has 0x35 MapEffect slots. 0x4 is the encounter's hidden/default
        // state. P5 enables only 0x2F, the outer memory/flashback scenery.
        for (byte slot = 0; slot < 0x35; slot++)
        {
            ushort state = slot == 0x2F ? (ushort)0x2 : (ushort)0x4;
            var flags = (byte)(state & 0xFF);
            if (flags == 0) flags = 0x01;
            world.Map.AddEffect(((uint)state << 16) | flags, slot);
        }
    });
}
