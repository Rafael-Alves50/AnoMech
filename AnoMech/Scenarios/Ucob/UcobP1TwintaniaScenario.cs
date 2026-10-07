using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Ucob.UcobConstants;

namespace AnoMech.Scenarios.Ucob;

public sealed class UcobP1TwintaniaScenario : IScenario
{
    public string Name => "Full phase";
    public IPhase Phase => UcobZone.P1;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    private SimWorld world = null!;
    private SimParty party = null!;
    private SimEnemy? twin;
    private readonly List<Vector3> neurolinks = [];

    public void Run(SimWorld w, int? selectedAi)
    {
        world = w;
        party = w.Party;
        neurolinks.Clear();

        twin = UcobScenarioUtil.Spawn(w, BNpcBaseId.Twintania, new Vector3(0, 0, -9), 0, true);
        twin?.SetTarget(party.Get(PartyRole.MainTank), follow: false);

        if (selectedAi != null)
        {
            // NAUR prepull: L1/R1 tanks, L2/R2 healers, L3/R3 melee, L4/R4 ranged.
            UcobScenarioUtil.MoveBots(party,
                (PartyRole.MainTank, new(-2.5f, 0, -4)),
                (PartyRole.OffTank, new(2.5f, 0, -4)),
                (PartyRole.RegenHealer, new(-5, 0, 1)),
                (PartyRole.ShieldHealer, new(5, 0, 1)),
                (PartyRole.MeleeDpsA, new(-3, 0, 5)),
                (PartyRole.MeleeDpsB, new(3, 0, 5)),
                (PartyRole.PhysRangedDps, new(-7, 0, 8)),
                (PartyRole.CasterDps, new(7, 0, 8)));
        }

        // P1-1: Plummet > Twister + Fireball > Death Sentence, then repeat.
        w.Events.Add(5.0f, () => twin?.Cast(ActionId.Plummet, castSeconds: 0f));
        TwisterFireball(8.7f, first: true);
        w.Events.Add(15.2f, () => twin?.Cast(ActionId.DeathSentence, castSeconds: 4f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId));
        TwisterFireball(24.4f, first: false);
        w.Events.Add(31.0f, () => twin?.Cast(ActionId.DeathSentence, castSeconds: 4f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId));

        // Neurolinks: NAUR normal triangle N -> SW -> SE (1,2,3).
        DropNeurolink(36.0f, new Vector3(0, 0, -8));

        // P1-2: liquid hell cycles + hatch + twister.
        LiquidHells(40.0f);
        Hatch(47.3f, 0);
        LiquidHells(53.7f);
        w.Events.Add(60.0f, () => twin?.Cast(ActionId.DeathSentence, castSeconds: 4f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId));
        Hatch(66.0f, 0);
        Twister(67.1f);
        DropNeurolink(76.0f, new Vector3(-8, 0, 5));

        // P1-3: same core mechanics, with fireball overlaps.
        LiquidHells(80.0f);
        Hatch(87.3f, 1);
        Fireball(91.0f, minPlayers: 4);
        LiquidHells(94.0f);
        w.Events.Add(100.0f, () => twin?.Cast(ActionId.DeathSentence, castSeconds: 4f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId));
        Twister(107.0f);
        Hatch(107.1f, 1);
        DropNeurolink(118.0f, new Vector3(8, 0, 5));
    }

    private void TwisterFireball(float at, bool first)
    {
        Twister(at);
        Fireball(at + 0.2f, first ? 5 : 4);
    }

    private void Twister(float at)
    {
        world.Events.Add(at, () => twin?.Cast(ActionId.Twister, castSeconds: 2f));
        world.Events.Add(at + 2f, () =>
        {
            var snapshots = UcobScenarioUtil.Alive(party).Select(x => x.member.Position).ToArray();
            for (var i = 0; i < snapshots.Length; i++)
            {
                var p = snapshots[i];
                world.SpawnEventObject(new EventObjectSpawnConfig
                {
                    EObjId = EObjId.Twister,
                    Placement = new Placement(p, 0),
                    TimelineState = 0,
                    SpawnVisible = true,
                    TargetableStatus = 1,
                    Radius = 0.5f,
                });
            }
            world.Events.Add(0.35f, () =>
            {
                foreach (var p in snapshots)
                    UcobScenarioUtil.KillInCircle(party, p, Geometry.TwisterRadius, "Twister");
            });
        });
    }

    private void Fireball(float at, int minPlayers)
    {
        world.Events.Add(at, () =>
        {
            var candidates = new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps };
            var target = candidates[Random.Shared.Next(candidates.Length)];
            twin?.Cast(ActionId.Fireball, castSeconds: 0f, targetId: party.Get(target)?.GameObjectId);
            world.Events.Add(4.8f, () => UcobScenarioUtil.ResolveStack(party, target, Geometry.FireballRadius, minPlayers, "Fireball stack failed"));
        });
    }

    private void LiquidHells(float at)
    {
        for (var i = 0; i < 5; i++)
        {
            var t = at + i * 1.2f;
            world.Events.Add(t, () =>
            {
                // NAUR: physical ranged baits liquid hells. Snapshot where the baiter is.
                var bait = party.Get(PartyRole.PhysRangedDps);
                if (bait == null) return;
                var pos = bait.Position;
                twin?.Cast(ActionId.LiquidHell, pos, castSeconds: 0f);
                world.Events.Add(0.1f, () => UcobScenarioUtil.KillInCircle(party, pos, Geometry.LiquidHellRadius, "Liquid Hell"));
            });
        }
    }

    private void Hatch(float at, int linkIndex)
    {
        world.Events.Add(at, () =>
        {
            twin?.Cast(ActionId.Generate, castSeconds: 3f);
            var candidates = new[] { PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB };
            var target = candidates[Random.Shared.Next(candidates.Length)];
            var targetMember = party.Get(target);
            world.Events.Add(3.1f, () =>
            {
                if (targetMember == null || !targetMember.IsAlive()) return;
                var safe = neurolinks.Count > linkIndex && UcobScenarioUtil.HorizontalDistance(targetMember.Position, neurolinks[linkIndex]) <= 4f;
                if (!safe) targetMember.Die("Hatch was not intercepted in a Neurolink");
            });
        });
    }

    private void DropNeurolink(float at, Vector3 pos)
    {
        world.Events.Add(at, () =>
        {
            neurolinks.Add(pos);
            world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.Neurolink,
                Placement = new Placement(pos, 0),
                TimelineState = 0,
                SpawnVisible = true,
                TargetableStatus = 1,
                Radius = 1f,
            });
        });
    }
}
