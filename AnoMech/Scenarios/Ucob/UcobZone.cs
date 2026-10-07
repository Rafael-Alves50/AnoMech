using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Ucob;

public sealed class UcobZone : IZone
{
    public static readonly UcobZone Instance = new();

    public static readonly Phase P1 = new(Instance, "P1: Twintania", null, 0);
    public static readonly Phase P2 = new(Instance, "P2: Nael", null, 0);
    public static readonly Phase P3 = new(Instance, "P3: Bahamut Prime", null, 0);
    public static readonly Phase P4 = new(Instance, "P4: Adds", null, 0);
    public static readonly Phase P5 = new(Instance, "P5: Golden Bahamut", null, 226);

    public string Name => "The Unending Coil of Bahamut (Ultimate)";
    public uint TerritoryId => 733;
    public Vector3 Origin => new(0f, 0f, 0f);
    public byte Level => UcobConstants.Level;
    public ushort ItemLevel => UcobConstants.ItemLevel;

    public IReadOnlyList<WaymarkLayout> WaymarkPresets { get; } =
        [new WaymarkLayout("NAUR / Aether Markers", UcobConstants.AetherWaymarks)];

    public void Run(SimWorld world)
    {
        world.EnforceArenaBoundary(UcobConstants.Geometry.ArenaRadius);

        // Keep the upstream UCOB rendering workaround: without a duty director,
        // this gravel layer overlaps the intended floor and causes visible z-fighting.
        world.Events.Add(1f, () => world.Map.SuppressLayer(0x1360));
    }
}
