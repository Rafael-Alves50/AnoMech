using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Ucob.UcobConstants;

namespace AnoMech.Scenarios.Ucob;

internal static class UcobScenarioUtil
{
    public static SimEnemy? Spawn(SimWorld world, uint baseId, Vector3 pos, float rot = MathF.PI, bool targetable = true)
        => world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: baseId,
            Level: Level,
            Targetable: targetable,
            EnemyList: targetable ? EnemyListMode.Always : EnemyListMode.Never,
            Placement: new Placement(pos, rot)));

    public static IEnumerable<(PartyRole role, SimCharacter member)> Alive(SimParty party)
    {
        for (var i = 0; i < 8; i++)
        {
            var m = party.Get(i);
            if (m != null && m.IsAlive())
                yield return ((PartyRole)i, m);
        }
    }

    public static void KillInCircle(SimParty party, Vector3 center, float radius, string cause)
    {
        var r2 = radius * radius;
        foreach (var (_, m) in Alive(party))
        {
            var d = m.Position - center;
            if (d.X * d.X + d.Z * d.Z <= r2)
                m.Die(cause);
        }
    }

    public static void KillOutsideCircle(SimParty party, Vector3 center, float radius, string cause)
    {
        var r2 = radius * radius;
        foreach (var (_, m) in Alive(party))
        {
            var d = m.Position - center;
            if (d.X * d.X + d.Z * d.Z > r2)
                m.Die(cause);
        }
    }

    public static void KillInDonut(SimParty party, Vector3 center, float inner, float outer, string cause)
    {
        var i2 = inner * inner;
        var o2 = outer * outer;
        foreach (var (_, m) in Alive(party))
        {
            var d = m.Position - center;
            var r2 = d.X * d.X + d.Z * d.Z;
            if (r2 >= i2 && r2 <= o2)
                m.Die(cause);
        }
    }

    public static void KillInRect(SimParty party, Vector3 origin, float heading, float length, float halfWidth, string cause)
    {
        var f = new Vector2(MathF.Sin(heading), MathF.Cos(heading));
        var r = new Vector2(f.Y, -f.X);
        foreach (var (_, m) in Alive(party))
        {
            var d3 = m.Position - origin;
            var d = new Vector2(d3.X, d3.Z);
            var along = Vector2.Dot(d, f);
            var side = MathF.Abs(Vector2.Dot(d, r));
            if (along >= 0 && along <= length && side <= halfWidth)
                m.Die(cause);
        }
    }

    public static void KillInCone(SimParty party, Vector3 origin, float heading, float radius, float halfAngle, string cause)
    {
        var f = new Vector2(MathF.Sin(heading), MathF.Cos(heading));
        foreach (var (_, m) in Alive(party))
        {
            var d3 = m.Position - origin;
            var d = new Vector2(d3.X, d3.Z);
            var len = d.Length();
            if (len <= 0.001f || len > radius) continue;
            var dot = Vector2.Dot(Vector2.Normalize(d), f);
            if (dot >= MathF.Cos(halfAngle))
                m.Die(cause);
        }
    }

    public static void ResolveSpread(SimParty party, IEnumerable<PartyRole> roles, float radius, string cause)
    {
        var selected = roles.Select(r => (role: r, member: party.Get(r))).Where(x => x.member != null && x.member.IsAlive()).ToArray();
        for (var i = 0; i < selected.Length; i++)
        for (var j = i + 1; j < selected.Length; j++)
        {
            if (HorizontalDistance(selected[i].member!.Position, selected[j].member!.Position) <= radius)
            {
                selected[i].member!.Die(cause);
                selected[j].member!.Die(cause);
            }
        }
    }

    public static void ResolveStack(SimParty party, PartyRole target, float radius, int minPlayers, string cause)
    {
        var t = party.Get(target);
        if (t == null || !t.IsAlive()) return;
        var count = Alive(party).Count(x => HorizontalDistance(x.member.Position, t.Position) <= radius);
        if (count < minPlayers)
            foreach (var (_, m) in Alive(party).Where(x => HorizontalDistance(x.member.Position, t.Position) <= radius).ToArray())
                m.Die(cause);
    }

    public static void ResolveTower(SimParty party, Vector3 center, float radius, int minPlayers, string cause)
    {
        var inside = Alive(party).Where(x => HorizontalDistance(x.member.Position, center) <= radius).ToArray();
        if (inside.Length < minPlayers)
            party.WipeAllPlayers(cause);
    }

    public static void MoveBots(SimParty party, params (PartyRole role, Vector3 pos)[] moves)
    {
        foreach (var (role, pos) in moves)
            party.Get(role)?.MoveTo(pos, 9f);
    }

    public static void MoveAllBots(SimParty party, Vector3 pos)
    {
        for (var i = 0; i < 8; i++)
            party.Get((PartyRole)i)?.MoveTo(pos, 9f);
    }

    public static Vector3 Polar(float radius, float degrees)
    {
        var a = degrees * MathF.PI / 180f;
        return new Vector3(radius * MathF.Sin(a), 0f, -radius * MathF.Cos(a));
    }

    public static float HeadingTo(Vector3 from, Vector3 to)
    {
        var d = to - from;
        return MathF.Atan2(d.X, d.Z);
    }

    public static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var d = a - b;
        return MathF.Sqrt(d.X * d.X + d.Z * d.Z);
    }
}
