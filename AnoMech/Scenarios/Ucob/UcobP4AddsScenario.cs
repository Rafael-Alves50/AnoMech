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

public sealed class UcobP4AddsScenario : IScenario
{
    public string Name => "Full Adds phase";
    public IPhase Phase => UcobZone.P4;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    private SimWorld world = null!;
    private SimParty party = null!;
    private SimEnemy? twin;
    private SimEnemy? nael;
    private Rng rng = Rng.Detached;
    private bool? _firstQuoteWasIn;

    public void Run(SimWorld w, int? selectedAi)
    {
        world = w;
        rng = w.Rng; party = w.Party;
        _firstQuoteWasIn = null;

        // NAUR: MT Twin, OT Nael; bosses held between 1/3, slightly NE.
        twin = UcobScenarioUtil.Spawn(w, BNpcBaseId.Twintania, new Vector3(-4,0,-5), 0, true);
        nael = UcobScenarioUtil.Spawn(w, BNpcBaseId.Nael, new Vector3(4,0,-5), 0, true);
        twin?.SetTarget(party.Get(PartyRole.MainTank), follow: false);
        nael?.SetTarget(party.Get(PartyRole.OffTank), follow: false);

        if (selectedAi != null)
        {
            world.Events.Add(0.5f, () =>
            {
                UcobScenarioUtil.MoveBots(party,
                    (PartyRole.MainTank, new(-4,0,-2)),
                    (PartyRole.OffTank, new(4,0,-2)));
                foreach (var r in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps })
                    if (r != party.PlayerRole) party.Get(r)?.MoveTo(Vector3.Zero, 9f);
            });
        }

        // Rotation 1.
        PlummetAndClaw(4f);
        LiquidHells(7f);
        HatchTwister(14f);
        QuoteTwister(19f, firstLoop: true);
        Megaflare(27f);
        DoubleTankbuster(35f);

        // Rotation 2.
        PlummetAndClaw(43f);
        LiquidHells(49f);
        HatchTwister(56f);
        QuoteTwister(61f, firstLoop: false);
        DoubleTankbuster(70f);
        Megaflare(81f);
    }

    private void PlummetAndClaw(float at)
    {
        world.Events.Add(at, () =>
        {
            twin?.Cast(ActionId.Plummet, castSeconds: 0f);
            nael?.Cast(ActionId.BahamutsClaw, castSeconds: 0f, targetId: party.Get(PartyRole.OffTank)?.GameObjectId);
            UcobScenarioUtil.KillInCone(party, twin?.Position ?? Vector3.Zero, twin?.Rotation ?? 0f, 8f, 45f * MathF.PI / 180f, "Adds: Plummet");
        });
    }

    private void LiquidHells(float at)
    {
        for (var i = 0; i < 5; i++)
        {
            world.Events.Add(at + i * 1.2f, () =>
            {
                var bait = party.Get(PartyRole.PhysRangedDps);
                if (bait == null) return;
                var p = bait.Position;
                twin?.Cast(ActionId.LiquidHell, p, castSeconds: 0f);
                UcobScenarioUtil.KillInCircle(party, p, Geometry.LiquidHellRadius, "Adds: Liquid Hell");
            });
        }
    }

    private void HatchTwister(float at)
    {
        // Exactly three of the four DPS receive Hatch. D4 is the flex:
        // if D4 is unmarked, D4 leaves; if D4 is marked, D4 fills the missing D1/D2/D3 link.
        var dps = new[] { PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps };
        var marked = dps.OrderBy(_ => rng.Next(int.MaxValue)).Take(3).ToArray();
        var unmarked = dps.Except(marked).Single();

        var assigned = new Dictionary<PartyRole, Vector3>
        {
            [PartyRole.MeleeDpsA] = new Vector3(-8,0,5),
            [PartyRole.MeleeDpsB] = new Vector3(8,0,5),
            [PartyRole.PhysRangedDps] = new Vector3(0,0,-8),
        };
        if (marked.Contains(PartyRole.CasterDps))
            assigned[PartyRole.CasterDps] = assigned[unmarked];

        foreach (var p in assigned.Values.Distinct())
            world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.Neurolink, Placement = new Placement(p,0), SpawnVisible = true, TargetableStatus = 1, Radius = 1f });

        world.Events.Add(at, () => twin?.Cast(ActionId.Generate, castSeconds: 3f));

        if (world.Party.PlayerRole != PartyRole.CasterDps && selectedBot(marked, PartyRole.CasterDps, out var d4Pos))
            party.Get(PartyRole.CasterDps)?.MoveTo(d4Pos, 9f);

        foreach (var role in marked)
            if (role != party.PlayerRole && assigned.TryGetValue(role, out var p))
                party.Get(role)?.MoveTo(p, 9f);

        // Front links resolve Hatch before Twister. Back/N link baits Twister outside first.
        world.Events.Add(at + 3.0f, () =>
        {
            foreach (var role in marked)
            {
                if (!assigned.TryGetValue(role, out var link) || link.Z < 0) continue;
                var m = party.Get(role);
                if (m != null && UcobScenarioUtil.HorizontalDistance(m.Position, link) > 2.5f)
                    m.Die("Adds: front Hatch missed Neurolink");
            }
        });

        world.Events.Add(at + 3.1f, () => twin?.Cast(ActionId.Twister, castSeconds: 2f));
        world.Events.Add(at + 5.1f, () => UcobP3.SpawnTwisters(world, party, "Adds: Twister"));
        world.Events.Add(at + 5.5f, () =>
        {
            foreach (var role in marked)
            {
                if (!assigned.TryGetValue(role, out var link) || link.Z >= 0) continue;
                var m = party.Get(role);
                if (m != null && UcobScenarioUtil.HorizontalDistance(m.Position, link) > 2.5f)
                    m.Die("Adds: back Hatch missed Neurolink after Twister");
            }
        });

        bool selectedBot(PartyRole[] targets, PartyRole role, out Vector3 pos)
        {
            if (targets.Contains(role) && assigned.TryGetValue(role, out pos)) return true;
            pos = UcobScenarioUtil.Polar(19, 180);
            return true;
        }
    }

    private void QuoteTwister(float at, bool firstLoop)
    {
        // Adds has exactly four legal three-part quotes. The second loop always
        // starts with the opposite In/Out family from the first loop.
        var inQuotes = new[]
        {
            new uint[] { ActionId.LunarDynamo, ActionId.RavenDive, ActionId.ThermionicBeam },
            new uint[] { ActionId.LunarDynamo, ActionId.IronChariot, ActionId.RavenDive },
        };
        var outQuotes = new[]
        {
            new uint[] { ActionId.IronChariot, ActionId.ThermionicBeam, ActionId.RavenDive },
            new uint[] { ActionId.IronChariot, ActionId.RavenDive, ActionId.ThermionicBeam },
        };

        // Deterministic family flip per run: first loop random family, second loop opposite.
        if (_firstQuoteWasIn == null)
            _firstQuoteWasIn = rng.Next(2) == 0;
        var useIn = firstLoop ? _firstQuoteWasIn.Value : !_firstQuoteWasIn.Value;
        var pool = useIn ? inQuotes : outQuotes;
        var quote = pool[rng.Next(pool.Length)];

        for (var i = 0; i < quote.Length; i++)
        {
            var a = quote[i];
            world.Events.Add(at + i * 3.1f, () =>
            {
                var p = nael?.Position ?? Vector3.Zero;
                if (a == ActionId.LunarDynamo) UcobScenarioUtil.KillInDonut(party, p, Geometry.DynamoInner, Geometry.DynamoOuter, "Adds quote: Dynamo");
                else if (a == ActionId.IronChariot) UcobScenarioUtil.KillInCircle(party, p, Geometry.ChariotRadius, "Adds quote: Chariot");
                else if (a == ActionId.ThermionicBeam) UcobScenarioUtil.ResolveStack(party, PartyRole.RegenHealer, 4f, 8, "Adds quote: stack");
                else if (a == ActionId.RavenDive) UcobScenarioUtil.ResolveSpread(party, Enumerable.Range(0,8).Select(x => (PartyRole)x), 6f, "Adds quote: spread");
            });
        }
        world.Events.Add(at + quote.Length * 3.1f + 0.2f, () => UcobP3.SpawnTwisters(world, party, "Adds quote: Twister"));
    }

    private void Megaflare(float at)
    {
        world.Events.Add(at, () =>
        {
            // All NAUR stacks are resolved in arena center.
            var stack = (PartyRole)rng.Next(8);
            UcobScenarioUtil.ResolveStack(party, stack, 5f, 4, "Adds: Megaflare stack");
            UcobScenarioUtil.ResolveSpread(party, Enumerable.Range(0,8).Select(x => (PartyRole)x), 10f, "Adds: Megaflare spread");
        });
    }

    private void DoubleTankbuster(float at)
    {
        world.Events.Add(at, () =>
        {
            twin?.Cast(ActionId.DeathSentence, castSeconds: 4f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId);
            nael?.Cast(ActionId.Ravensbeak, castSeconds: 4f, targetId: party.Get(PartyRole.OffTank)?.GameObjectId);
        });
    }
}
