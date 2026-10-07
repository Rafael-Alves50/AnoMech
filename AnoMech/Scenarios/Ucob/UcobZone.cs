using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Ucob;

public sealed class UcobZone : IZone
{
    public static readonly UcobZone Instance = new();

    // UCoB keeps the same arena throughout. The phases are split only for the menu.
    public static readonly Phase P1 = new(Instance, "P1: Twintania", null, 0);
    public static readonly Phase P2 = new(Instance, "P2: Nael", null, 0);
    public static readonly Phase P3 = new(Instance, "P3: Bahamut Prime", null, 0);
    public static readonly Phase P4 = new(Instance, "P4: Adds", null, 0);
    public static readonly Phase P5 = new(Instance, "P5: Golden Bahamut", null, 0);

    public string Name => "The Unending Coil of Bahamut (Ultimate)";
    public uint TerritoryId => 733;
    public Vector3 Origin => Vector3.Zero;
    public byte Level => UcobConstants.Level;
    public ushort ItemLevel => UcobConstants.ItemLevel;

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
    [
        new WaymarkLayout("NAUR / Good Aether Markers", UcobConstants.NaurWaymarks)
    ];

    public void Run(SimWorld world) => world.EnforceArenaBoundary(UcobConstants.Geometry.ArenaRadius);
}
