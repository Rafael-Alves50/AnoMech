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
    private readonly Random rng = new();

    public void Run(SimWorld w, int? selectedAi)
    {
        world = w; party = w.Party;

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
        QuoteTwister(19f);
        Megaflare(27f);
        DoubleTankbuster(35f);

        // Rotation 2.
        PlummetAndClaw(43f);
        LiquidHells(49f);
        HatchTwister(56f);
        QuoteTwister(61f);
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
        world.Events.Add(at, () => twin?.Cast(ActionId.Generate, castSeconds: 3f));
        world.Events.Add(at + 1.1f, () => twin?.Cast(ActionId.Twister, castSeconds: 2f));
        world.Events.Add(at + 3.1f, () => UcobP3.SpawnTwisters(world, party, "Adds: Twister"));

        world.Events.Add(at + 3.2f, () =>
        {
            // In Adds, hatch targets still need a neurolink. Use the south link as the training reference.
            var target = (PartyRole)rng.Next(2, 8);
            var m = party.Get(target);
            var link = new Vector3(0,0,9);
            world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.Neurolink, Placement = new Placement(link,0), SpawnVisible = true, TargetableStatus = 1, Radius = 1f });
            if (m != null && UcobScenarioUtil.HorizontalDistance(m.Position, link) > 2.5f)
                m.Die("Adds: Hatch not intercepted in Neurolink");
        });
    }

    private void QuoteTwister(float at)
    {
        var quote = rng.Next(4) switch
        {
            0 => new uint[] { ActionId.LunarDynamo, ActionId.IronChariot },
            1 => new uint[] { ActionId.LunarDynamo, ActionId.ThermionicBeam },
            2 => new uint[] { ActionId.ThermionicBeam, ActionId.IronChariot },
            _ => new uint[] { ActionId.RavenDive, ActionId.LunarDynamo },
        };

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
        world.Events.Add(at + 0.2f, () => UcobP3.SpawnTwisters(world, party, "Adds quote: Twister"));
    }

    private void Megaflare(float at)
    {
        world.Events.Add(at, () =>
        {
            // All NAUR stacks are resolved in arena center.
            var stack = (PartyRole)rng.Next(0, 8);
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
