using System;
using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// FRU P5 Fulgent Blade / Exalines.
//
// This first port intentionally stops after the Exaline sequence; Akh Morn and
// later P5 mechanics will be separate follow-up work. Geometry and bot movement
// are based on WCGH FRU-Sim, while the real FFXIV action ids are used for casts/VFX.
public sealed class FruP5FulgentBladeScenario : IScenario
{
    public string Name => "Fulgent Blade (Exalines)";
    public IPhase Phase => FruZone.P5;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP5FulgentBladeAi()];

    private const float WaveWidth = Geometry.ExalineWidth;
    private const int WaveHits = Geometry.ExalineHits;

    // WCGH exawave-controller child transforms, in controller-local X/Z.
    private static readonly GroupDef EastGroup = new(new Vector3(9.821f, 0f, 23.71f), 0f);
    private static readonly GroupDef WestGroup = new(new Vector3(-9.821f, 0f, -23.71f), MathF.PI);
    private static readonly GroupDef NorthGroup = new(new Vector3(23.71f, 0f, -9.821f), -MathF.PI / 2f);

    // Four bars in each exawave: dark/light on AB, then dark/light on the 45-degree CD axis.
    private static readonly WaveDef[] WaveDefs =
    [
        new(0f,                         isLight: false),
        new(MathF.PI,                   isLight: true),
        new(MathF.PI + MathF.PI / 4f,   isLight: false),
        new(MathF.PI / 4f,              isLight: true),
    ];

    private SimWorld world = null!;
    private SimParty party = null!;
    private FruP5FulgentBladeState state = null!;
    private SimEnemy? pandora;
    private readonly List<List<WaveLine>> groups = [[], [], []];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new FruP5FulgentBladeState();

        for (var i = 0; i < groups.Count; i++) groups[i].Clear();

        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<FruP5FulgentBladeState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0.1f, SpawnPandora);
        world.Events.Add(0.5f, () => pandora?.Cast(ActionId.FulgentBlade, castSeconds: 6f));

        // WCGH reference sequence:
        // exaline controller starts at t=4.0; E/W highlights at +2.2,
        // N at +6.2, opposite W/E at +10.2. Each first path has a 7s
        // cast whose damage snapshot is ~0.2s before the visible hit.
        var first = state.EastFirst ? EastGroup : WestGroup;
        var third = state.EastFirst ? WestGroup : EastGroup;

        ScheduleGroup(0, first, 6.2f);
        ScheduleGroup(1, NorthGroup, 10.2f);
        ScheduleGroup(2, third, 14.2f);

        // Let the final wave VFX finish, then clean up the fake enemies.
        world.Events.Add(35.2f, DespawnAll);
    }

    private void SpawnPandora()
    {
        pandora = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Pandora,
            NameId: 0,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
    }

    private void ScheduleGroup(int groupIndex, GroupDef def, float start)
    {
        world.Events.Add(start, () => StartGroup(groupIndex, def));

        // Snapshot is 0.2s before the end of the first 7.0s path cast.
        const float firstSnapshot = 6.8f;
        for (var hit = 0; hit < WaveHits; hit++)
        {
            var capturedHit = hit;
            var snapshotAt = start + firstSnapshot + 2f * hit;
            world.Events.Add(snapshotAt, () => SnapshotGroup(groupIndex, capturedHit));

            // First hit's visual release comes from the 7s cast already in progress.
            // Rest actions are instant and are fired 0.2s after snapshot, matching WCGH.
            if (hit > 0)
                world.Events.Add(snapshotAt + 0.2f, () => FireRestVisual(groupIndex));

            // Advance the wave after its visible burst, before the next 2s snapshot.
            if (hit < WaveHits - 1)
                world.Events.Add(snapshotAt + 0.45f, () => AdvanceGroup(groupIndex));
        }
    }

    private void StartGroup(int groupIndex, GroupDef def)
    {
        var origin = state.ToControllerSpace(def.Offset);
        var groupYaw = state.ControllerRotation + def.Yaw;
        var lines = groups[groupIndex];
        lines.Clear();

        foreach (var wave in WaveDefs)
        {
            var yaw = groupYaw + wave.Yaw;
            var forward = Forward(yaw);
            var move = -forward; // WCGH WavePosition moves local -Z each hit.
            var helperFacing = RotationFor(move);
            var firstAction = wave.IsLight ? ActionId.PathOfLightFirst : ActionId.PathOfDarknessFirst;
            var restAction = wave.IsLight ? ActionId.PathOfLightRest : ActionId.PathOfDarknessRest;

            var helper = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: BNpcBaseId.Helper,
                NameId: 0,
                Level: Level,
                Targetable: false,
                EnemyList: EnemyListMode.Never,
                IsVisible: false,
                Placement: new Placement(origin, helperFacing)));

            helper?.Cast(firstAction, castSeconds: 7f, animationLock: 0f);
            lines.Add(new WaveLine(helper, origin, move, helperFacing, restAction));
        }
    }

    private void SnapshotGroup(int groupIndex, int hit)
    {
        var lines = groups[groupIndex];
        foreach (var line in lines)
        {
            // Keep the native helper exactly at the stripe's back edge so action VFX
            // and our collision snapshot share the same geometry.
            line.Helper?.SetPosition(line.Edge);
            line.Helper?.SetRotation(line.Facing);
            KillStripe(line.Edge, line.Move, groupIndex, hit);
        }
    }

    private void FireRestVisual(int groupIndex)
    {
        foreach (var line in groups[groupIndex])
        {
            line.Helper?.SetPosition(line.Edge);
            line.Helper?.SetRotation(line.Facing);
            line.Helper?.Cast(line.RestAction, castSeconds: 0f, animationLock: 0f);
        }
    }

    private void AdvanceGroup(int groupIndex)
    {
        foreach (var line in groups[groupIndex])
            line.Edge += line.Move * WaveWidth;
    }

    // WCGH's wave mesh is 140y long and 11.855y deep. Inside FRU's 22y arena,
    // only the depth test matters; the long axis spans the full arena.
    // WavePosition is the back edge, and the stripe extends WaveWidth along Move.
    private void KillStripe(Vector3 edge, Vector3 move, int groupIndex, int hit)
    {
        for (var i = 0; i < 8; i++)
        {
            var member = party.Get(i);
            if (member is null || !member.IsAlive()) continue;

            var delta = member.Position - edge;
            var along = delta.X * move.X + delta.Z * move.Z;
            if (along < 0f || along > WaveWidth) continue;

            member.Die($"Died to Fulgent Blade Exaline (set {groupIndex + 1}, hit {hit + 1})");
        }
    }

    private void DespawnAll()
    {
        pandora?.Despawn();
        foreach (var group in groups)
        foreach (var line in group)
            line.Helper?.Despawn();
    }

    private static Vector3 Forward(float yaw) => new(MathF.Sin(yaw), 0f, MathF.Cos(yaw));

    private static float RotationFor(Vector3 direction) => MathF.Atan2(direction.X, direction.Z);

    private sealed record GroupDef(Vector3 Offset, float Yaw);
    private sealed record WaveDef(float Yaw, bool IsLight);

    private sealed class WaveLine(
        SimEnemy? helper,
        Vector3 edge,
        Vector3 move,
        float facing,
        uint restAction)
    {
        public SimEnemy? Helper { get; } = helper;
        public Vector3 Edge { get; set; } = edge;
        public Vector3 Move { get; } = move;
        public float Facing { get; } = facing;
        public uint RestAction { get; } = restAction;
    }
}
