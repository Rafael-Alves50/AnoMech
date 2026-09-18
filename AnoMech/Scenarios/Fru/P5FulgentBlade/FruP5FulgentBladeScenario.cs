using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// FRU P5 Fulgent Blade / Exalines.
//
// The scenario deliberately ends after Fulgent Blade. It uses the live FRU
// action IDs and the six real base-line geometry; each base line launches
// Light and Darkness fronts in opposite directions and advances them 5y every 2s.
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

    // One light + one dark helper per base line. Helpers are invisible; native
    // action omens/effects provide the visible telegraphs.
    private readonly Dictionary<(int Line, bool Light), SimEnemy?> helpers = [];

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        party = world.Party;
        state = new FruP5FulgentBladeState();
        helpers.Clear();

        if (selectedAi is { } idx && idx >= 0 && idx < AiStrats.Count)
            ((IScenarioAi<FruP5FulgentBladeState>)AiStrats[idx]).Run(state, world);

        world.Events.Add(0.1f, SpawnPandora);
        world.Events.Add(0.5f, () => pandora?.Cast(ActionId.FulgentBlade, castSeconds: 6f));

        // Base lines become relevant as three pairs at t=10/14/18.
        foreach (var pair in state.Lines.GroupBy(l => l.Index / 2))
        {
            var activation = 10f + 4f * pair.Key;
            foreach (var line in pair)
            {
                var captured = line;
                world.Events.Add(activation, () => StartLine(captured));
            }
        }

        // Every stripe gets a damage snapshot. The initial Path casts release
        // naturally 0.2s later; rest actions are fired at that visible-hit time.
        foreach (var stripe in state.Stripes)
        {
            var captured = stripe;
            world.Events.Add(stripe.SnapshotTime, () => Snapshot(captured));
            if (stripe.Step > 0)
                world.Events.Add(stripe.SnapshotTime + 0.2f, () => FireRestVisual(captured));
        }

        // Last possible snapshot is ~38.8s; leave enough time for the VFX to finish.
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

    private void StartLine(FruFulgentLine line)
    {
        StartFront(line, isLight: true, line.Heading);
        StartFront(line, isLight: false, line.Heading + MathF.PI);
    }

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
        // Subsequent fronts have advanced to a new 5y segment.
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
    }

    private static float RotationFor(Vector3 direction) => MathF.Atan2(direction.X, direction.Z);
}
