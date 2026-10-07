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

internal static class UcobP3
{
    private static readonly PartyRole[] QmOrder =
    [
        PartyRole.MainTank, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.PhysRangedDps,
        PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.MeleeDpsB, PartyRole.CasterDps
    ];

    public static PartyRole RoleForGroup(int group) => QmOrder[group];

    public static void MoveQuickmarch(SimParty party, float northDeg, float radius, bool spread)
    {
        for (var group = 0; group < 8; group++)
        {
            var left = group < 4;
            var order = group & 3;
            var off = spread ? 90f + (order - 1.5f) * 35f : 60f + order * 20f;
            var dir = northDeg + (left ? off : -off);
            var pos = UcobScenarioUtil.Polar(radius, dir);
            var role = RoleForGroup(group);
            if (role != party.PlayerRole)
                party.Get(role)?.MoveTo(pos, 9f);
        }
    }

    public static void SpawnTwisters(SimWorld world, SimParty party, string cause = "Twister")
    {
        var points = UcobScenarioUtil.Alive(party).Select(x => x.member.Position).ToArray();
        foreach (var p in points)
            world.SpawnEventObject(new EventObjectSpawnConfig
            {
                EObjId = EObjId.Twister,
                Placement = new Placement(p, 0),
                TimelineState = 0,
                SpawnVisible = true,
                TargetableStatus = 1,
                Radius = 0.5f
            });
        world.Events.Add(0.3f, () =>
        {
            foreach (var p in points)
                UcobScenarioUtil.KillInCircle(party, p, Geometry.TwisterRadius, cause);
        });
    }

    public static void ResolveDive(SimParty party, Vector3 source, Vector3 target, float halfWidth, string cause)
        => UcobScenarioUtil.KillInRect(party, source, UcobScenarioUtil.HeadingTo(source, target), 60f, halfWidth, cause);

    public static void SpawnHypernova(SimWorld world, SimParty party, Vector3 pos)
    {
        world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.Hypernova,
            Placement = new Placement(pos, 0),
            TimelineState = 0,
            SpawnVisible = true,
            TargetableStatus = 1,
            Radius = 0.5f
        });
        UcobScenarioUtil.KillInCircle(party, pos, Geometry.HypernovaRadius, "Hypernova");
    }

    public static void ResolveEarthShakers(SimParty party, Vector3 source, IReadOnlyList<PartyRole> targets)
    {
        foreach (var r in targets)
        {
            var p = party.Get(r);
            if (p != null)
                UcobScenarioUtil.KillInCone(party, source, UcobScenarioUtil.HeadingTo(source, p.Position), 60, Geometry.EarthShakerHalfAngle, "Earth Shaker");
        }
    }

    public static Vector3[] RingTowers(int count, float radius, float firstDeg)
        => Enumerable.Range(0, count).Select(i => UcobScenarioUtil.Polar(radius, firstDeg + 360f * i / count)).ToArray();

    public static void ResolveTowers(SimParty party, IEnumerable<Vector3> towers, float radius = 3f)
    {
        foreach (var t in towers)
            UcobScenarioUtil.ResolveTower(party, t, radius, 1, "Unsoaked Megaflare tower");
    }
}

public sealed class UcobP3CoreScenario : IScenario
{
    public string Name => "Bahamut core rotation";
    public IPhase Phase => UcobZone.P3;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    public void Run(SimWorld world, int? selectedAi)
    {
        var party = world.Party;
        var baha = UcobScenarioUtil.Spawn(world, BNpcBaseId.BahamutPrime, new(0, 0, -9), 0, true);
        baha?.SetTarget(party.Get(PartyRole.MainTank), follow: false);

        world.Events.Add(2f, () => baha?.Cast(ActionId.SeventhUmbralEra, castSeconds: 0f));
        world.Events.Add(2f, () => party.Knockback(Vector3.Zero, 11f));
        world.Events.Add(7f, () =>
        {
            baha?.Cast(ActionId.FlareBreath, castSeconds: 0f);
            UcobScenarioUtil.KillInCone(party, baha?.Position ?? Vector3.Zero, 0, 25f, 50f * MathF.PI / 180f, "Flare Breath");
        });
        world.Events.Add(11f, () => baha?.Cast(ActionId.Flatten, castSeconds: 4f, targetId: party.Get(PartyRole.MainTank)?.GameObjectId));
        world.Events.Add(18f, () => baha?.Cast(ActionId.Gigaflare, castSeconds: 6f));
    }
}

public sealed class UcobP3QuickmarchScenario : IScenario
{
    public string Name => "Trio 1 — Quickmarch";
    public IPhase Phase => UcobZone.P3;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    public void Run(SimWorld world, int? selectedAi)
    {
        var party = world.Party;
        var rng = new Random();
        var north = rng.Next(8) * 45f;
        var bahaPos = UcobScenarioUtil.Polar(21, north);
        var naelPos = UcobScenarioUtil.Polar(21, north + 120f);
        var twinPos = UcobScenarioUtil.Polar(21, north - 120f);
        var baha = UcobScenarioUtil.Spawn(world, BNpcBaseId.BahamutPrime, Vector3.Zero, 0, true);
        var nael = UcobScenarioUtil.Spawn(world, BNpcBaseId.Nael, naelPos, UcobScenarioUtil.HeadingTo(naelPos, Vector3.Zero), false);
        var twin = UcobScenarioUtil.Spawn(world, BNpcBaseId.Twintania, twinPos, UcobScenarioUtil.HeadingTo(twinPos, Vector3.Zero), false);

        world.Events.Add(1f, () => baha?.Cast(ActionId.QuickmarchTrio, castSeconds: 4f));
        if (selectedAi != null)
            world.Events.Add(5.0f, () => UcobP3.MoveQuickmarch(party, north, 20f, false));

        // Boss reappears relative north; triple dives snapshot ~5.1s after reveal.
        world.Events.Add(6.1f, () => baha?.SetPosition(bahaPos));
        world.Events.Add(10.9f, () =>
        {
            UcobP3.ResolveDive(party, twinPos, Vector3.Zero, Geometry.DiveHalfWidthTwinNael, "Quickmarch: Twisting Dive");
            UcobP3.ResolveDive(party, naelPos, Vector3.Zero, Geometry.DiveHalfWidthTwinNael, "Quickmarch: Lunar Dive");
            UcobP3.ResolveDive(party, bahaPos, Vector3.Zero, Geometry.DiveHalfWidthBahamut, "Quickmarch: Megaflare Dive");
            UcobP3.SpawnTwisters(world, party);
        });

        if (selectedAi != null)
            world.Events.Add(11.8f, () => UcobP3.MoveQuickmarch(party, north, 12f, true));

        world.Events.Add(12.3f, () => UcobScenarioUtil.ResolveSpread(party, Enumerable.Range(0, 8).Select(i => (PartyRole)i), 10f, "Quickmarch: Megaflare spread"));
        world.Events.Add(14.8f, () =>
        {
            var stack = (PartyRole)rng.Next(0, 8);
            UcobScenarioUtil.ResolveStack(party, stack, 5f, 3, "Quickmarch: Megaflare stack");
        });

        world.Events.Add(17.0f, () =>
        {
            var targets = new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer };
            UcobP3.ResolveEarthShakers(party, bahaPos, targets);
        });

        world.Events.Add(20.0f, () =>
        {
            // Tempest Wing: tanks take two random tethers and separate.
            foreach (var tank in new[] { PartyRole.MainTank, PartyRole.OffTank })
            {
                var p = party.Get(tank);
                if (p != null) UcobScenarioUtil.KillInCircle(party, p.Position, 5f, "Quickmarch: Tempest Wing clipped party");
            }
        });
    }
}

public sealed class UcobP3BlackfireScenario : IScenario
{
    public string Name => "Trio 2 — Blackfire";
    public IPhase Phase => UcobZone.P3;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    public void Run(SimWorld world, int? selectedAi)
    {
        var party = world.Party;
        var rng = new Random();
        var north = rng.Next(8) * 45f;
        var naelPos = UcobScenarioUtil.Polar(21, north);
        var baha = UcobScenarioUtil.Spawn(world, BNpcBaseId.BahamutPrime, Vector3.Zero, 0, true);
        var nael = UcobScenarioUtil.Spawn(world, BNpcBaseId.Nael, naelPos, UcobScenarioUtil.HeadingTo(naelPos, Vector3.Zero), false);

        world.Events.Add(1f, () => baha?.Cast(ActionId.BlackfireTrio, castSeconds: 4f));
        if (selectedAi != null)
        {
            // NAUR: DPS CCW of relative north, supports CW.
            world.Events.Add(5f, () =>
            {
                var ccw = UcobScenarioUtil.Polar(8, north - 90);
                var cw = UcobScenarioUtil.Polar(8, north + 90);
                foreach (var r in new[] { PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps })
                    if (r != party.PlayerRole) party.Get(r)?.MoveTo(ccw, 9f);
                foreach (var r in new[] { PartyRole.MainTank, PartyRole.OffTank, PartyRole.RegenHealer, PartyRole.ShieldHealer })
                    if (r != party.PlayerRole) party.Get(r)?.MoveTo(cw, 9f);
            });
        }

        // Five Liquid Hells are baited along the north-south split.
        for (var i = 0; i < 5; i++)
        {
            var n = i;
            world.Events.Add(6f + i * 1.2f, () =>
            {
                var bait = party.Get(PartyRole.PhysRangedDps);
                if (bait == null) return;
                var p = bait.Position;
                UcobScenarioUtil.KillInCircle(party, p, Geometry.LiquidHellRadius, "Blackfire: Liquid Hell");
                if (n < 4) UcobP3.SpawnHypernova(world, party, p);
            });
        }

        world.Events.Add(8.5f, () => UcobScenarioUtil.ResolveStack(party, PartyRole.RegenHealer, 4f, 8, "Blackfire: Thermionic Beam"));
        world.Events.Add(13f, () =>
        {
            var towers = new[]
            {
                UcobScenarioUtil.Polar(10, north - 45), UcobScenarioUtil.Polar(10, north + 45),
                UcobScenarioUtil.Polar(10, north - 135), UcobScenarioUtil.Polar(10, north + 135)
            };
            UcobP3.ResolveTowers(party, towers);
        });

        world.Events.Add(16f, () =>
        {
            var stackTargets = Enumerable.Range(0, 8).OrderBy(_ => rng.Next()).Take(4).Select(i => (PartyRole)i).ToArray();
            var target = stackTargets[0];
            UcobScenarioUtil.ResolveStack(party, target, 5f, 4, "Blackfire: Megaflare stack");
        });

        world.Events.Add(18f, () =>
        {
            var target = party.Get(PartyRole.PhysRangedDps)?.Position ?? Vector3.Zero;
            UcobP3.ResolveDive(party, naelPos, target, Geometry.DiveHalfWidthTwinNael, "Blackfire: Megaflare/Lunar dive");
        });
    }
}

public sealed class UcobP3FellruinScenario : IScenario
{
    public string Name => "Trio 3 — Fellruin";
    public IPhase Phase => UcobZone.P3;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    public void Run(SimWorld world, int? selectedAi)
    {
        var party = world.Party;
        var rng = new Random();
        var north = rng.Next(8) * 45f;
        var bahaPos = UcobScenarioUtil.Polar(17, north);
        var naelPos = UcobScenarioUtil.Polar(11, north + 180f);
        var baha = UcobScenarioUtil.Spawn(world, BNpcBaseId.BahamutPrime, bahaPos, UcobScenarioUtil.HeadingTo(bahaPos, Vector3.Zero), false);
        var nael = UcobScenarioUtil.Spawn(world, BNpcBaseId.Nael, naelPos, UcobScenarioUtil.HeadingTo(naelPos, Vector3.Zero), false);
        UcobScenarioUtil.Spawn(world, BNpcBaseId.Twintania, UcobScenarioUtil.Polar(21, north + 90), 0, false);

        world.Events.Add(1f, () => baha?.Cast(ActionId.FellruinTrio, castSeconds: 4f));

        // NAUR: party uses the neurolink opposite Bahamut; OT and MT use the adjacent links.
        var links = new[] { UcobScenarioUtil.Polar(8, north - 120), UcobScenarioUtil.Polar(8, north + 180), UcobScenarioUtil.Polar(8, north + 120) };
        foreach (var p in links)
            world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.Neurolink, Placement = new Placement(p, 0), SpawnVisible = true, TargetableStatus = 1, Radius = 1f });

        if (selectedAi != null)
            world.Events.Add(5f, () =>
            {
                UcobScenarioUtil.MoveBots(party,
                    (PartyRole.OffTank, links[0]),
                    (PartyRole.MainTank, links[2]));
                foreach (var r in new[] { PartyRole.RegenHealer, PartyRole.ShieldHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps })
                    if (r != party.PlayerRole) party.Get(r)?.MoveTo(links[1], 9f);
            });

        world.Events.Add(14f, () =>
        {
            foreach (var (role, m) in UcobScenarioUtil.Alive(party).ToArray())
            {
                var required = role == PartyRole.OffTank ? links[0] : role == PartyRole.MainTank ? links[2] : links[1];
                if (UcobScenarioUtil.HorizontalDistance(m.Position, required) > 2.2f)
                    m.Die("Fellruin: Aetheric Profusion outside assigned Neurolink");
            }
        });

        // Random Nael quote after Profusion. Include both two- and three-part legal quote patterns.
        var quotes = new[]
        {
            new uint[] { ActionId.RavenDive, ActionId.IronChariot },
            new uint[] { ActionId.RavenDive, ActionId.LunarDynamo },
            new uint[] { ActionId.MeteorStream, ActionId.DalamudDive },
            new uint[] { ActionId.IronChariot, ActionId.ThermionicBeam, ActionId.RavenDive },
            new uint[] { ActionId.LunarDynamo, ActionId.RavenDive, ActionId.MeteorStream },
        };
        var quote = quotes[rng.Next(quotes.Length)];
        for (var i = 0; i < quote.Length; i++)
        {
            var a = quote[i];
            world.Events.Add(16f + i * 3.1f, () =>
            {
                if (a == ActionId.IronChariot) UcobScenarioUtil.KillInCircle(party, naelPos, Geometry.ChariotRadius, "Fellruin: Chariot");
                else if (a == ActionId.LunarDynamo) UcobScenarioUtil.KillInDonut(party, naelPos, Geometry.DynamoInner, Geometry.DynamoOuter, "Fellruin: Dynamo");
                else if (a is ActionId.RavenDive or ActionId.MeteorStream) UcobScenarioUtil.ResolveSpread(party, Enumerable.Range(0,8).Select(x => (PartyRole)x), a == ActionId.RavenDive ? 6 : 8, "Fellruin: spread");
                else if (a == ActionId.ThermionicBeam) UcobScenarioUtil.ResolveStack(party, PartyRole.RegenHealer, 4, 8, "Fellruin: stack");
                else if (a == ActionId.DalamudDive) UcobScenarioUtil.KillInCircle(party, party.Get(PartyRole.MainTank)?.Position ?? Vector3.Zero, 5, "Fellruin: Dalamud Dive");
            });
        }
    }
}

public sealed class UcobP3HeavensfallScenario : IScenario
{
    public string Name => "Trio 4 — Heavensfall";
    public IPhase Phase => UcobZone.P3;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    public void Run(SimWorld world, int? selectedAi)
    {
        var party = world.Party;
        var rng = new Random();
        // Heavensfall always spawns the trio adjacent along one random edge, with
        // all 6 boss orders possible. Nael can therefore be left, middle, or right.
        var anchor = rng.Next(8) * 45f;
        var slots = new[] { anchor - 22.5f, anchor, anchor + 22.5f };
        var order = new[] { BNpcBaseId.BahamutPrime, BNpcBaseId.Nael, BNpcBaseId.Twintania }.OrderBy(_ => rng.Next()).ToArray();
        var posById = new Dictionary<uint, Vector3>();
        for (var i = 0; i < 3; i++)
            posById[order[i]] = UcobScenarioUtil.Polar(21, slots[i]);

        var naelPos = posById[BNpcBaseId.Nael];
        var twinPos = posById[BNpcBaseId.Twintania];
        var bahaPos = posById[BNpcBaseId.BahamutPrime];
        var naelDeg = MathF.Atan2(naelPos.X, -naelPos.Z) * 180f / MathF.PI;

        var baha = UcobScenarioUtil.Spawn(world, BNpcBaseId.BahamutPrime, Vector3.Zero, 0, true);
        UcobScenarioUtil.Spawn(world, BNpcBaseId.Nael, naelPos, UcobScenarioUtil.HeadingTo(naelPos, Vector3.Zero), false);
        UcobScenarioUtil.Spawn(world, BNpcBaseId.Twintania, twinPos, UcobScenarioUtil.HeadingTo(twinPos, Vector3.Zero), false);

        world.Events.Add(1f, () => baha?.Cast(ActionId.HeavensfallTrio, castSeconds: 4f));

        if (selectedAi != null)
            world.Events.Add(7.5f, () => UcobScenarioUtil.MoveAllBots(party, Vector3.Zero));

        world.Events.Add(9.5f, () =>
        {
            UcobP3.ResolveDive(party, twinPos, Vector3.Zero, Geometry.DiveHalfWidthTwinNael, "Heavensfall: Twisting Dive");
            UcobP3.ResolveDive(party, bahaPos, Vector3.Zero, Geometry.DiveHalfWidthBahamut, "Heavensfall: Megaflare Dive");
        });

        world.Events.Add(10.5f, () =>
        {
            UcobP3.SpawnTwisters(world, party, "Heavensfall: Twister");
            party.Knockback(Vector3.Zero, 11f);
        });

        // Eight towers are 22.5deg apart; NAUR's R1 tower is directly under Nael.
        var towers = UcobP3.RingTowers(8, 15f, naelDeg);
        if (selectedAi != null)
            world.Events.Add(11f, () =>
            {
                // Assignment preset from BossMod/NA standard: H1, H2, M1, M2, R1, R2, MT, OT in CW tower priority.
                var priority = new[]
                {
                    PartyRole.PhysRangedDps, PartyRole.MainTank, PartyRole.MeleeDpsB, PartyRole.RegenHealer,
                    PartyRole.MeleeDpsA, PartyRole.ShieldHealer, PartyRole.OffTank, PartyRole.CasterDps
                };
                for (var i = 0; i < 8; i++)
                {
                    var r = priority[i];
                    if (r != party.PlayerRole) party.Get(r)?.MoveTo(towers[i], 9f);
                }
            });
        world.Events.Add(15f, () => UcobP3.ResolveTowers(party, towers));

        world.Events.Add(17f, () =>
        {
            var stack = (PartyRole)rng.Next(0, 8);
            UcobScenarioUtil.ResolveStack(party, stack, 4f, 8, "Heavensfall: Fireball");
        });

        // Rotating Thermionic Burst.
        var cw = rng.Next(2) == 0;
        for (var i = 0; i < 8; i++)
        {
            var j = i;
            world.Events.Add(20f + 0.45f * i, () =>
            {
                var deg = naelDeg + (cw ? 22.5f : -22.5f) * j;
                UcobScenarioUtil.KillInCone(party, Vector3.Zero, deg * MathF.PI / 180f, 24.5f, 11.25f * MathF.PI / 180f, "Heavensfall: Thermionic Burst");
            });
        }
    }
}

public sealed class UcobP3TenstrikeScenario : IScenario
{
    public string Name => "Trio 5 — Tenstrike";
    public IPhase Phase => UcobZone.P3;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    public void Run(SimWorld world, int? selectedAi)
    {
        var party = world.Party;
        var rng = new Random();
        var baha = UcobScenarioUtil.Spawn(world, BNpcBaseId.BahamutPrime, Vector3.Zero, 0, true);
        world.Events.Add(1f, () => baha?.Cast(ActionId.TenstrikeTrio, castSeconds: 4f));

        // Six hatch targets in two waves; two players remain untargeted.
        var shuffled = Enumerable.Range(0, 8).OrderBy(_ => rng.Next()).Select(i => (PartyRole)i).ToArray();
        var hatchTargets = shuffled.Take(6).ToArray();
        var links = new[] { new Vector3(0,0,-8), new Vector3(-8,0,5), new Vector3(8,0,5) };
        foreach (var p in links)
            world.SpawnEventObject(new EventObjectSpawnConfig { EObjId = EObjId.Neurolink, Placement = new Placement(p,0), SpawnVisible = true, TargetableStatus = 1, Radius = 1f });

        for (var i = 0; i < 6; i++)
        {
            var index = i;
            world.Events.Add(7f + 1.0f * i, () =>
            {
                var m = party.Get(hatchTargets[index]);
                var link = links[index % 3];
                if (m != null && UcobScenarioUtil.HorizontalDistance(m.Position, link) > 2.5f)
                    m.Die("Tenstrike: Hatch not taken through Neurolink");
            });
        }

        // Meteor streams on everyone while hatches resolve.
        world.Events.Add(8.5f, () => UcobScenarioUtil.ResolveSpread(party, Enumerable.Range(0,8).Select(i => (PartyRole)i), 8f, "Tenstrike: Meteor Stream"));

        // Two sets of four Earthshakers. NAUR keeps marker 1 as the guaranteed non-bait safe.
        var first = shuffled.Take(4).ToArray();
        var second = shuffled.Skip(4).Take(4).ToArray();
        world.Events.Add(15f, () => UcobP3.ResolveEarthShakers(party, Vector3.Zero, first));
        world.Events.Add(18.5f, () => UcobP3.ResolveEarthShakers(party, Vector3.Zero, second));

        if (selectedAi != null)
        {
            world.Events.Add(14f, () =>
            {
                // Baiters fan from north; non-baiters hold around marker 1.
                for (var i = 0; i < first.Length; i++)
                {
                    var r = first[i];
                    if (r != party.PlayerRole) party.Get(r)?.MoveTo(UcobScenarioUtil.Polar(19, -90 + i * 60), 9f);
                }
                foreach (var r in Enumerable.Range(0,8).Select(i => (PartyRole)i).Except(first))
                    if (r != party.PlayerRole) party.Get(r)?.MoveTo(new Vector3(0,0,-8), 9f);
            });
        }
    }
}

public sealed class UcobP3GrandOctetScenario : IScenario
{
    public string Name => "Trio 6 — Grand Octet";
    public IPhase Phase => UcobZone.P3;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    public void Run(SimWorld world, int? selectedAi)
    {
        var party = world.Party;
        var rng = new Random();
        // Grand Octet occupies all eight fixed card/intercardinal slots exactly once:
        // Bahamut, Nael, Twin, and the five elemental drakes.
        var slotPermutation = Enumerable.Range(0, 8).OrderBy(_ => rng.Next()).ToArray();
        var bahaIndex = slotPermutation[0];
        var naelIndex = slotPermutation[1];
        var twinIndex = slotPermutation[2];
        var bahaDeg = bahaIndex * 45f;
        var naelDeg = naelIndex * 45f;
        var diveDirection = (bahaIndex & 1) != 0 ? -1 : 1; // intercardinal -> CW, cardinal -> CCW

        var bahaPos = UcobScenarioUtil.Polar(21, bahaDeg);
        var naelPos = UcobScenarioUtil.Polar(21, naelDeg);
        var twinPos = UcobScenarioUtil.Polar(21, twinIndex * 45f);

        var baha = UcobScenarioUtil.Spawn(world, BNpcBaseId.BahamutPrime, Vector3.Zero, 0, true);
        var nael = UcobScenarioUtil.Spawn(world, BNpcBaseId.Nael, naelPos, 0, false);
        var twin = UcobScenarioUtil.Spawn(world, BNpcBaseId.Twintania, twinPos, 0, false);

        var drakeIds = new[] { BNpcBaseId.Firehorn, BNpcBaseId.Iceclaw, BNpcBaseId.Thunderwing, BNpcBaseId.TailOfDarkness, BNpcBaseId.FangOfLight };
        var drakes = new List<(SimEnemy? enemy, Vector3 pos)>();
        for (var i = 0; i < 5; i++)
        {
            var pos = UcobScenarioUtil.Polar(24, slotPermutation[i + 3] * 45f);
            drakes.Add((UcobScenarioUtil.Spawn(world, drakeIds[i], pos, 0, false), pos));
        }

        world.Events.Add(1f, () => baha?.Cast(ActionId.GrandOctet, castSeconds: 4f));
        world.Events.Add(5.5f, () => baha?.SetPosition(bahaPos));

        // Initial safe is opposite Bahamut; if Nael is there, shift one octant in dive direction.
        var safeDeg = (bahaDeg + 180f) % 360f;
        if (MathF.Abs(((safeDeg - naelDeg + 540f) % 360f) - 180f) < 5f)
            safeDeg += diveDirection * 45f;
        var safe = UcobScenarioUtil.Polar(20, safeDeg);
        if (selectedAi != null)
            world.Events.Add(5.6f, () => UcobScenarioUtil.MoveAllBots(party, safe));

        // Legal dive order is Nael -> 5 drakes ordered from Bahamut around arena -> Bahamut -> Twintania.
        var orderedDrakes = drakes.OrderBy(d =>
        {
            var deg = MathF.Atan2(d.pos.X, -d.pos.Z) * 180f / MathF.PI;
            if (deg < 0) deg += 360;
            var dist = diveDirection > 0 ? (deg - bahaDeg + 360) % 360 : (bahaDeg - deg + 360) % 360;
            return dist;
        }).ToArray();

        var sources = new List<(Vector3 pos, float width, string name)> { (naelPos, Geometry.DiveHalfWidthTwinNael, "Lunar Dive") };
        sources.AddRange(orderedDrakes.Select((d, i) => (d.pos, Geometry.DrakeDiveHalfWidth, $"Cauterize {i + 1}")));
        sources.Add((bahaPos, Geometry.DiveHalfWidthBahamut, "Megaflare Dive"));
        sources.Add((twinPos, Geometry.DiveHalfWidthTwinNael, "Twisting Dive"));

        var baitOrder = Enumerable.Range(0, 8).OrderBy(_ => rng.Next()).Select(i => (PartyRole)i).ToArray();
        for (var i = 0; i < sources.Count; i++)
        {
            var n = i;
            world.Events.Add(8f + n * 2.0f, () =>
            {
                var bait = party.Get(baitOrder[n]);
                var target = bait?.Position ?? Vector3.Zero;
                UcobP3.ResolveDive(party, sources[n].pos, target, sources[n].width, "Grand Octet: " + sources[n].name);
            });
        }

        // Four stack targets + four towers at the end.
        world.Events.Add(24.5f, () =>
        {
            var stackRoles = baitOrder.Take(4).ToArray();
            UcobScenarioUtil.ResolveStack(party, stackRoles[0], 5f, 4, "Grand Octet: Megaflare stack");
            var towers = UcobP3.RingTowers(4, 14f, safeDeg + 45);
            UcobP3.ResolveTowers(party, towers);
        });
    }
}
