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

public sealed class UcobP5GoldenScenario : IScenario
{
    public string Name => "Full Golden Bahamut";
    public IPhase Phase => UcobZone.P5;
    public bool SupportsSolo => true;
    public IReadOnlyList<IScenarioAi> AiStrats => [new UcobNaurStrat()];

    private SimWorld world = null!;
    private SimParty party = null!;
    private SimEnemy? baha;
    private readonly Random rng = new();
    private readonly List<SimEnemy?> exaHelpers = [];

    public void Run(SimWorld w, int? selectedAi)
    {
        world = w;
        party = w.Party;
        exaHelpers.Clear();

        baha = UcobScenarioUtil.Spawn(w, BNpcBaseId.BahamutPrime, new Vector3(0,0,-7), 0, true);
        baha?.SetTarget(party.Get(PartyRole.MainTank), follow: false);

        // Golden timeline follows the live state sequence:
        // MA1 > AM1(3) > Exa1 > AM2(4) > MA2 > Exa2 > MA3 > AM3(5)
        // > Exa3 > MA4 > AM4(6) > Exa4 > MA5 > enrage.
        MornAfah(1.0f, 1);
        AkhMorn(9.2f, hits: 3, order: 0, selectedAi != null);
        Exaflare(18.3f, 1);

        AkhMorn(43.0f, hits: 4, order: 1, selectedAi != null);
        MornAfah(54.3f, 2);
        Exaflare(68.5f, 2);

        MornAfah(91.0f, 3);
        AkhMorn(105.2f, hits: 5, order: 2, selectedAi != null);
        Exaflare(117.4f, 3);

        MornAfah(140.0f, 4);
        AkhMorn(154.2f, hits: 6, order: 3, selectedAi != null);
        Exaflare(166.3f, 4);

        MornAfah(189.0f, 5);
        Enrage(197.1f);
    }

    private void MornAfah(float at, int number)
    {
        world.Events.Add(at, () =>
        {
            var targetCandidates = Enumerable.Range(2, 6).Select(i => (PartyRole)i).ToArray();
            var target = targetCandidates[rng.Next(targetCandidates.Length)];
            baha?.Cast(ActionId.MornAfah, castSeconds: 6f, targetId: party.Get(target)?.GameObjectId);

            world.Events.Add(6f, () =>
            {
                var t = party.Get(target);
                if (t == null) return;
                var inside = UcobScenarioUtil.Alive(party).Count(x => UcobScenarioUtil.HorizontalDistance(x.member.Position, t.Position) <= 5f);
                if (inside < 8)
                    foreach (var (_, m) in UcobScenarioUtil.Alive(party).Where(x => UcobScenarioUtil.HorizontalDistance(x.member.Position, t.Position) <= 5f).ToArray())
                        m.Die($"Morn Afah {number}: incomplete party stack");
            });
        });
    }

    private void AkhMorn(float at, int hits, int order, bool aiEnabled)
    {
        world.Events.Add(at, () =>
        {
            var targetRole = order == 2 ? PartyRole.OffTank : PartyRole.MainTank;
            var target = party.Get(targetRole);
            baha?.Cast(ActionId.AkhMorn, castSeconds: 4f, targetId: target?.GameObjectId);

            // NAUR order: Share > MT invuln > OT invuln > Share.
            if (aiEnabled && order == 1 && party.PlayerRole != PartyRole.MainTank)
                party.GiveInvuln(PartyRole.MainTank, 10f);
            if (aiEnabled && order == 2 && party.PlayerRole != PartyRole.OffTank)
                party.GiveInvuln(PartyRole.OffTank, 10f);

            for (var hit = 0; hit < hits; hit++)
            {
                var h = hit;
                world.Events.Add(4f + h * 1.1f, () =>
                {
                    var anchor = party.Get(targetRole);
                    if (anchor == null || !anchor.IsAlive()) return;

                    var tanksIn = new[] { PartyRole.MainTank, PartyRole.OffTank }
                        .Count(r => party.Get(r) is { } m && m.IsAlive() && UcobScenarioUtil.HorizontalDistance(m.Position, anchor.Position) <= 4f);

                    // Shared sets require both tanks. Invuln sets require the designated tank's invulnerability.
                    if (order is 0 or 3)
                    {
                        if (tanksIn < 2)
                            anchor.Die($"Akh Morn {order + 1}, hit {h + 1}: not shared by both tanks");
                    }
                    else if (!anchor.HasStatus(SimParty.InvulnStatusId))
                    {
                        anchor.Die($"Akh Morn {order + 1}, hit {h + 1}: assigned invuln missing");
                    }

                    // Non-tanks inside the buster are also killed.
                    foreach (var (role, m) in UcobScenarioUtil.Alive(party).ToArray())
                        if (!role.IsTank() && UcobScenarioUtil.HorizontalDistance(m.Position, anchor.Position) <= 4f)
                            m.Die($"Akh Morn {order + 1}: clipped tankbuster");
                });
            }
        });
    }

    private void Exaflare(float at, int set)
    {
        // Pick any of the 8 possible arena orientations.
        var orientation = rng.Next(8) * 45f * MathF.PI / 180f;
        var forward = new Vector3(MathF.Sin(orientation), 0, MathF.Cos(orientation));
        var side = new Vector3(forward.Z, 0, -forward.X);

        // Six lanes are all used. Exactly two lanes belong to each of waves 1/2/3.
        // Shuffle the multiset so patterns such as 221133, 123123, 132231, etc. can appear.
        var waves = new[] { 1, 1, 2, 2, 3, 3 }.OrderBy(_ => rng.Next()).ToArray();
        var offsets = new[] { -15f, -9f, -3f, 3f, 9f, 15f };

        world.Events.Add(at, () => baha?.Cast(ActionId.Exaflare, castSeconds: 4f));

        for (var lane = 0; lane < 6; lane++)
        {
            var laneIndex = lane;
            var wave = waves[lane];
            var firstAt = at + 4f + (wave - 1) * Geometry.ExaflareInterval;
            var start = -forward * 20f + side * offsets[lane];

            world.Events.Add(firstAt - 3.9f, () =>
            {
                var helper = UcobScenarioUtil.Spawn(world, BNpcBaseId.Helper, start, orientation, false);
                exaHelpers.Add(helper);
                helper?.Cast(ActionId.ExaflareFirst, castSeconds: 3.9f);
            });

            for (var step = 0; step < 6; step++)
            {
                var s = step;
                var pos = start + forward * (Geometry.ExaflareStep * step);
                world.Events.Add(firstAt + Geometry.ExaflareInterval * step, () =>
                {
                    UcobScenarioUtil.KillInCircle(party, pos, Geometry.ExaflareRadius, $"Exaflare {set}, wave {wave}, lane {laneIndex + 1}, hit {s + 1}");
                    if (s > 0)
                    {
                        var helper = exaHelpers.LastOrDefault(h => h != null && UcobScenarioUtil.HorizontalDistance(h.Position, pos - forward * Geometry.ExaflareStep) < 1f);
                        helper?.SetPosition(pos);
                        helper?.Cast(ActionId.ExaflareRest, castSeconds: 0f);
                    }
                });
            }
        }
    }

    private void Enrage(float at)
    {
        // Golden enrage is a sequence of 10s casts on players in descending enmity.
        var order = new[]
        {
            PartyRole.MainTank, PartyRole.OffTank, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB,
            PartyRole.PhysRangedDps, PartyRole.CasterDps, PartyRole.RegenHealer, PartyRole.ShieldHealer
        };
        for (var i = 0; i < order.Length; i++)
        {
            var role = order[i];
            var first = i == 0;
            world.Events.Add(at + i * 1.2f, () =>
            {
                var target = party.Get(role);
                baha?.Cast(first ? ActionId.Enrage : ActionId.EnrageAOE, castSeconds: first ? 10f : 0f, targetId: target?.GameObjectId);
                target?.Die("Golden Bahamut enrage");
            });
        }
    }
}
