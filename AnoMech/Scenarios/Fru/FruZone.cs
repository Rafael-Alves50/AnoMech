using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru;

public sealed class FruZone : IZone
{
    public static readonly FruZone Instance = new();

    // Weather/BGM are deliberately left unset for the first runnable FRU port.
    // They are cosmetic and should be filled only after we verify the exact rows in game.
    public static readonly Phase P5 = new(Instance, "P5: Pandora", null, 0);

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

    public void Run(SimWorld world) => world.EnforceArenaBoundary(FruConstants.Geometry.ArenaRadius);
}
