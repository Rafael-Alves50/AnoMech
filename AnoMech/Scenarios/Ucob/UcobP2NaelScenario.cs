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

public sealed class UcobP2NaelScenario : IScenario
{
    public string Name => "Full phase";
    public IPhase Phase => UcobZone.P2;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    private SimWorld world = null!;
    private SimParty party = null!;
    private SimEnemy? nael;
    private readonly Random rng = new();

    private static readonly uint[][] QuotePairs =
    [
        [ActionId.LunarDynamo, ActionId.IronChariot],
        [ActionId.LunarDynamo, ActionId.ThermionicBeam],
        [ActionId.ThermionicBeam, ActionId.IronChariot],
        [ActionId.ThermionicBeam, ActionId.LunarDynamo],
        [ActionId.RavenDive, ActionId.IronChariot],
        [ActionId.RavenDive, ActionId.LunarDynamo],
        [ActionId.MeteorStream, ActionId.DalamudDive],
        [ActionId.DalamudDive, ActionId.ThermionicBeam],
    ];

    public void Run(SimWorld w, int? selectedAi)
    {
        world = w;
        party = w.Party;
        nael = UcobScenarioUtil.Spawn(w, BNpcBaseId.Nael, Vector3.Zero, MathF.PI, true);
        nael?.SetTarget(party.Get(PartyRole.MainTank), follow: false);

        if (selectedAi != null)
            NaurClockSpots(1.0f);

        // Opening: Heavensfall knockback -> rotating Thermionic Burst -> two waves of Meteor Stream.
        world.Events.Add(5.0f, () => nael?.Cast(ActionId.Heavensfall, castSeconds: 0f));
        world.Events.Add(5.0f, () => party.Knockback(Vector3.Zero, 11f));
        ThermionicBurst(6.0f);
        MeteorStreamWave(9.3f, outer: false);
        MeteorStreamWave(12.4f, outer: true);
        world.Events.Add(15.0f, () => nael?.Cast(ActionId.DalamudDive, castSeconds: 0f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId));

        // Bahamut's Favor: four fire cycles, ice assignments, lightning, doom/cleanse,
        // followed by the three Cauterize baits. NAUR rotates CW around Nael.
        world.Events.Add(18.0f, () => nael?.Cast(ActionId.BahamutsFavor, castSeconds: 3f));
        DragonCycle(23.0f, fireOut: false, cycle: 0);
        DragonCycle(30.0f, fireOut: true, cycle: 1);
        DragonCycle(37.0f, fireOut: false, cycle: 2);
        DragonCycle(44.0f, fireOut: false, cycle: 3);

        ResolveQuote(52.0f, QuotePairs[rng.Next(QuotePairs.Length)]);
        ResolveQuote(61.0f, QuotePairs[rng.Next(QuotePairs.Length)]);
        world.Events.Add(70.0f, () => nael?.Cast(ActionId.Ravensbeak, castSeconds: 4f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId));

        CauterizeSequence(77.0f);
        ResolveQuote(91.0f, QuotePairs[rng.Next(QuotePairs.Length)]);
        ResolveQuote(100.0f, QuotePairs[rng.Next(QuotePairs.Length)]);
    }

    private void NaurClockSpots(float at)
    {
        world.Events.Add(at, () =>
        {
            UcobScenarioUtil.MoveBots(party,
                (PartyRole.MainTank, UcobScenarioUtil.Polar(5, 337.5f)),
                (PartyRole.OffTank, UcobScenarioUtil.Polar(5, 22.5f)),
                (PartyRole.RegenHealer, UcobScenarioUtil.Polar(10, 292.5f)),
                (PartyRole.ShieldHealer, UcobScenarioUtil.Polar(10, 67.5f)),
                (PartyRole.MeleeDpsA, UcobScenarioUtil.Polar(5, 247.5f)),
                (PartyRole.MeleeDpsB, UcobScenarioUtil.Polar(5, 112.5f)),
                (PartyRole.PhysRangedDps, UcobScenarioUtil.Polar(10, 202.5f)),
                (PartyRole.CasterDps, UcobScenarioUtil.Polar(10, 157.5f)));
        });
    }

    private void ThermionicBurst(float at)
    {
        var start = rng.Next(8) * 45f + 11.25f;
        var cw = rng.Next(2) == 0;
        for (var wave = 0; wave < 4; wave++)
        {
            var w = wave;
            world.Events.Add(at + wave * 1.1f, () =>
            {
                var baseDeg = start + (cw ? 22.5f : -22.5f) * w;
                for (var i = 0; i < 4; i++)
                {
                    var h = (baseDeg + 90f * i) * MathF.PI / 180f;
                    UcobScenarioUtil.KillInCone(party, Vector3.Zero, h, 24.5f, 11.25f * MathF.PI / 180f, "Thermionic Burst");
                }
            });
        }
    }

    private void MeteorStreamWave(float at, bool outer)
    {
        world.Events.Add(at, () =>
        {
            var targets = Enumerable.Range(0, 8).Select(i => (PartyRole)i).ToArray();
            UcobScenarioUtil.ResolveSpread(party, targets, Geometry.MeteorStreamRadius * 2, "Meteor Stream overlap");
        });

        if (outer)
            world.Events.Add(MathF.Max(0f, at - 1.0f), () =>
            {
                UcobScenarioUtil.MoveBots(party,
                    (PartyRole.MainTank, UcobScenarioUtil.Polar(9, 337.5f)),
                    (PartyRole.OffTank, UcobScenarioUtil.Polar(9, 22.5f)),
                    (PartyRole.RegenHealer, UcobScenarioUtil.Polar(18, 337.5f)),
                    (PartyRole.ShieldHealer, UcobScenarioUtil.Polar(18, 22.5f)),
                    (PartyRole.MeleeDpsA, UcobScenarioUtil.Polar(9, 292.5f)),
                    (PartyRole.MeleeDpsB, UcobScenarioUtil.Polar(9, 67.5f)),
                    (PartyRole.PhysRangedDps, UcobScenarioUtil.Polar(18, 292.5f)),
                    (PartyRole.CasterDps, UcobScenarioUtil.Polar(18, 67.5f)));
            });
    }

    private void DragonCycle(float at, bool fireOut, int cycle)
    {
        world.Events.Add(at, () =>
        {
            var target = (PartyRole)rng.Next(2, 8);
            nael?.Cast(ActionId.FireballP2, castSeconds: 0f, targetId: party.Get(target)?.GameObjectId);
            world.Events.Add(5.1f, () =>
            {
                if (fireOut)
                {
                    // "Out" fire: only the tethered player should be away from the group.
                    var t = party.Get(target);
                    if (t != null && UcobScenarioUtil.HorizontalDistance(t.Position, Vector3.Zero) < 7f)
                        t.Die("Fire tether was IN during an OUT fire");
                }
                else
                {
                    UcobScenarioUtil.ResolveStack(party, target, Geometry.FireballRadius, 4, "Fireball stack failed");
                }
            });
        });

        // Ice hits are distributed through the phase; overlapping repeated ice/fire is lethal in game.
        for (var i = 0; i < 4; i++)
        {
            var offset = at + 1.9f + i * 0.5f;
            world.Events.Add(offset, () =>
            {
                var role = (PartyRole)rng.Next(0, 8);
                nael?.Cast(ActionId.Iceball, castSeconds: 0f, targetId: party.Get(role)?.GameObjectId);
            });
        }

        // Chain lightning hits two random players; 5y separation is required.
        world.Events.Add(at + 3.8f, () =>
        {
            var a = (PartyRole)rng.Next(0, 8);
            PartyRole b;
            do b = (PartyRole)rng.Next(0, 8); while (b == a);
            nael?.Cast(ActionId.ChainLightning, castSeconds: 0f);
            world.Events.Add(1.0f, () =>
            {
                var pa = party.Get(a); var pb = party.Get(b);
                if (pa != null) UcobScenarioUtil.KillInCircle(party, pa.Position, 5f, "Thunderstruck");
                if (pb != null) UcobScenarioUtil.KillInCircle(party, pb.Position, 5f, "Thunderstruck");
            });
        });

        // Doom + Wings of Salvation. Three cleanse puddles are placed in order.
        if (cycle is 1 or 3)
        {
            world.Events.Add(at + 4.8f, () =>
            {
                var doom = Enumerable.Range(0, 8).OrderBy(_ => rng.Next()).Take(3).Select(i => (PartyRole)i).ToArray();
                for (var i = 0; i < doom.Length; i++)
                {
                    var role = doom[i];
                    var puddle = UcobScenarioUtil.Polar(13, 210 + i * 30);
                    world.SpawnEventObject(new EventObjectSpawnConfig
                    {
                        EObjId = EObjId.Salvation,
                        Placement = new Placement(puddle, 0),
                        TimelineState = 0,
                        SpawnVisible = true,
                        TargetableStatus = 1,
                        Radius = 0.5f,
                    });
                    world.Events.Add(5.0f + i * 1.0f, () =>
                    {
                        var m = party.Get(role);
                        if (m != null && UcobScenarioUtil.HorizontalDistance(m.Position, puddle) > 1.5f)
                            m.Die("Doom was not cleansed");
                    });
                }
            });
        }
    }

    private void ResolveQuote(float at, uint[] sequence)
    {
        for (var i = 0; i < sequence.Length; i++)
        {
            var action = sequence[i];
            var t = at + 5.1f + i * 3.1f;
            world.Events.Add(t, () => ResolveQuoteAction(action));
        }
    }

    private void ResolveQuoteAction(uint action)
    {
        switch (action)
        {
            case ActionId.IronChariot:
                nael?.Cast(action, castSeconds: 0f);
                UcobScenarioUtil.KillInCircle(party, nael?.Position ?? Vector3.Zero, Geometry.ChariotRadius, "Quote: Iron Chariot (OUT)");
                break;
            case ActionId.LunarDynamo:
                nael?.Cast(action, castSeconds: 0f);
                UcobScenarioUtil.KillInDonut(party, nael?.Position ?? Vector3.Zero, Geometry.DynamoInner, Geometry.DynamoOuter, "Quote: Lunar Dynamo (IN)");
                break;
            case ActionId.ThermionicBeam:
                var stack = (PartyRole)rng.Next(0, 8);
                nael?.Cast(action, castSeconds: 0f, targetId: party.Get(stack)?.GameObjectId);
                UcobScenarioUtil.ResolveStack(party, stack, Geometry.ThermionicBeamRadius, 8, "Quote: Thermionic Beam stack failed");
                break;
            case ActionId.RavenDive:
            case ActionId.MeteorStream:
                nael?.Cast(action, castSeconds: 0f);
                UcobScenarioUtil.ResolveSpread(party, Enumerable.Range(0, 8).Select(i => (PartyRole)i), action == ActionId.RavenDive ? 6f : 8f, "Quote: spread overlap");
                break;
            case ActionId.DalamudDive:
                nael?.Cast(action, castSeconds: 0f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId);
                UcobScenarioUtil.KillInCircle(party, party.Get(PartyRole.MainTank)?.Position ?? Vector3.Zero, 5f, "Quote: Dalamud Dive cleaved party");
                break;
        }
    }

    private void CauterizeSequence(float at)
    {
        // Five drakes occupy five of eight inter/cardinal directions. The relative arrangement
        // is randomized each run; the three NAUR bait positions stay at the standard edge spots.
        var start = rng.Next(8);
        var dirs = Enumerable.Range(0, 8).OrderBy(_ => rng.Next()).Take(5).ToArray();
        var dragons = new List<SimEnemy?>();
        for (var i = 0; i < 5; i++)
        {
            var pos = UcobScenarioUtil.Polar(26, (start + dirs[i]) * 45);
            dragons.Add(UcobScenarioUtil.Spawn(world, BNpcBaseId.Firehorn + (uint)i, pos, UcobScenarioUtil.HeadingTo(pos, Vector3.Zero), false));
        }

        var baits = new[]
        {
            new Vector3(18.149f, 0, -9.531f),
            new Vector3(8f, 0, 18.874f),
            new Vector3(-17.667f, 0, 10.398f),
        };
        var baiters = new[] { PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.RegenHealer };

        for (var order = 0; order < 3; order++)
        {
            var o = order;
            world.Events.Add(at + order * 5.0f, () =>
            {
                if (baiters[o] != party.PlayerRole)
                    party.Get(baiters[o])?.MoveTo(baits[o], 9f);

                // Choose a currently unused drake and aim through the assigned bait.
                var d = dragons[(o * 2) % dragons.Count];
                if (d == null) return;
                var heading = UcobScenarioUtil.HeadingTo(d.Position, baits[o]);
                d.SetRotation(heading);
                d.Cast(ActionId.Cauterize1 + (uint)((o * 2) % 5), castSeconds: 4f);
                world.Events.Add(4.0f, () => UcobScenarioUtil.KillInRect(party, d.Position, heading, 52f, Geometry.DrakeDiveHalfWidth, "Cauterize"));
            });
        }
    }
}
