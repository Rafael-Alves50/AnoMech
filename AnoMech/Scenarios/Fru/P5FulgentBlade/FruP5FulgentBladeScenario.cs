using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// FRU P5 Fulgent Blade / Exalines.
//
// Native timing/visual sequence:
// - all six octagram line EObjs are created at mechanic start but remain dormant
// - t=8.0: every base line reveals (timeline bit 0x2)
// - t=10/14/18: successive adjacent pairs begin the progressive glow (0x10)
// - each glowing line runs its 7s Path of Light/Darkness first casts
// - +7.0s: line switches to its arrow/travel animation (0x8)
// - travelling hits then advance 5y every 2s
//
// Pattern geometry and line ordering match the FRU P5 implementation used by
// Simulant/BossMod; action IDs are the live FRU actions.
public sealed class FruP5FulgentBladeScenario : IScenario
{
    public string Name => "Fulgent Blade (Exalines)";
    public IPhase Phase => FruZone.P5;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruP5FulgentBladeAi()];

    private SimWorld world = null!;
    private SimParty party = null!;
    private FruP5FulgentBladeState state = null!;
    private SimEnemy? pandora;

    // One visible native Fulgent Blade EventObject per base line.
    private readonly Dictionary<int, SimEventObject?> lineVisuals = [];

    // One Light + one Darkness invisible helper per base line. These drive the
    // real Path casts/VFX; the EObjs above drive the progressive line animation.
    private readonly Dictionary<(int Line, bool Light), SimEnemy?> helpers = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new FruP5FulgentBladeState();
        helpers.Clear();
        lineVisuals.Clear();

        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<FruP5FulgentBladeState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0.1f, SpawnPandora);

        // The six line actors exist from mechanic start but are dormant/invisible
        // until their SharedGroup animation is kicked at t=8.
        world.Events.Add(0.4f, SpawnLineVisuals);
        world.Events.Add(0.5f, () => pandora?.Cast(ActionId.FulgentBlade, castSeconds: 6f));

        // Real sequence: all six faint base lines reveal together.
        world.Events.Add(8.0f, RevealAllBaseLines);

        // Adjacent pairs begin glowing in order, every four seconds.
        foreach (var pair in state.Lines.GroupBy(l => l.Index / 2))
        {
            var activation = 10f + 4f * pair.Key;
            foreach (var line in pair)
            {
                var captured = line;
                world.Events.Add(activation, () => StartLine(captured));

                // Seven seconds after glow begins, the line switches to the native
                // arrow/travel timeline just as the first Path cast resolves.
                world.Events.Add(activation + 7.0f, () => ShowTravelArrow(captured.Index));
            }
        }

        // Every stripe gets a damage snapshot. The initial Path casts release
        // 0.2s after snapshot; rest actions repeat every 2s as the fronts advance.
        foreach (var stripe in state.Stripes)
        {
            var captured = stripe;
            world.Events.Add(stripe.SnapshotTime, () => Snapshot(captured));
            if (stripe.Step > 0)
                world.Events.Add(stripe.SnapshotTime + 0.2f, () => FireRestVisual(captured));
        }

        world.Events.Add(41f, DespawnAll);
    }

    private void SpawnPandora()
    {
        pandora = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Pandora,
            NameId: BNpcNameId.Pandora,
            Level: Level,
            Targetable: true,
            EnemyList: EnemyListMode.Always,
            Placement: new Placement(Vector3.Zero, MathF.PI)));
    }

    private void SpawnLineVisuals()
    {
        foreach (var line in state.Lines)
        {
            var visual = world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.FulgentBladeLine,
                Placement = new Placement(line.Position, line.Heading),
                TimelineState = 0,
                SpawnVisible = true,
                TargetableStatus = 1,
                Radius = 0.5f,
            });
            lineVisuals[line.Index] = visual;
        }
    }

    private void RevealAllBaseLines()
    {
        foreach (var line in lineVisuals.Values)
            line?.PlayAnimation(0x2);
    }

    private void StartLine(FruFulgentLine line)
    {
        // Native line animation handles the gradual brightening over ~7 seconds.
        lineVisuals.GetValueOrDefault(line.Index)?.PlayAnimation(0x10);

        StartFront(line, isLight: true, line.Heading);
        StartFront(line, isLight: false, line.Heading + MathF.PI);
    }

    private void ShowTravelArrow(int lineIndex)
        => lineVisuals.GetValueOrDefault(lineIndex)?.PlayAnimation(0x8);

    private void StartFront(FruFulgentLine line, bool isLight, float heading)
    {
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Helper,
            NameId: 0,
            Level: Level,
            Targetable: false,
            EnemyList: EnemyListMode.Never,
            IsVisible: false,
            Placement: new Placement(line.Position, heading)));

        helpers[(line.Index, isLight)] = helper;

        var action = isLight ? ActionId.PathOfLightFirst : ActionId.PathOfDarknessFirst;
        helper?.Cast(action, castSeconds: 7f, animationLock: 0f);
    }

    private void Snapshot(FruFulgentStripe stripe)
    {
        if (helpers.TryGetValue((stripe.LineIndex, stripe.IsLight), out var helper))
        {
            helper?.SetPosition(stripe.Edge);
            helper?.SetRotation(RotationFor(stripe.Move));
        }

        var margin = Geometry.ExalineHitboxMargin;
        for (var i = 0; i < 8; i++)
        {
            var member = party.Get(i);
            if (member is null || !member.IsAlive()) continue;
            if (!FruP5FulgentBladeState.Contains(stripe, member.Position, margin)) continue;

            member.Die($"Died to {(stripe.IsLight ? "Path of Light" : "Path of Darkness")} " +
                       $"(line {stripe.LineIndex + 1}, step {stripe.Step + 1})");
        }
    }

    private void FireRestVisual(FruFulgentStripe stripe)
    {
        if (!helpers.TryGetValue((stripe.LineIndex, stripe.IsLight), out var helper) || helper is null)
            return;

        helper.SetPosition(stripe.Edge);
        helper.SetRotation(RotationFor(stripe.Move));
        helper.Cast(
            stripe.IsLight ? ActionId.PathOfLightRest : ActionId.PathOfDarknessRest,
            castSeconds: 0f,
            animationLock: 0f);
    }

    private void DespawnAll()
    {
        pandora?.Despawn();

        foreach (var helper in helpers.Values)
            helper?.Despawn();
        helpers.Clear();

        foreach (var line in lineVisuals.Values)
            line?.Despawn();
        lineVisuals.Clear();
    }

    private static float RotationFor(Vector3 direction) => MathF.Atan2(direction.X, direction.Z);
}
