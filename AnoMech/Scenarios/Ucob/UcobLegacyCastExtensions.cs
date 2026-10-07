using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Ucob;

// Compatibility shim for the older UCOB trainer code. Upstream moved cast timing into EnemyAction;
// keep the scenario choreography readable while using the new cast pipeline underneath.
internal static class UcobLegacyCastExtensions
{
    public static EnemyActionCast Cast(this SimEnemy enemy, uint actionId, float castSeconds = 0f, object? targetId = null)
        => enemy.Cast(new EnemyAction(actionId)
        {
            Cast = new CastSpec { CastSeconds = castSeconds }
        });

    public static EnemyActionCast Cast(this SimEnemy enemy, uint actionId, Vector3 position, float castSeconds = 0f, object? targetId = null)
        => enemy.Cast(new EnemyAction(actionId)
        {
            Cast = new CastSpec { CastSeconds = castSeconds }
        }, position);
}
