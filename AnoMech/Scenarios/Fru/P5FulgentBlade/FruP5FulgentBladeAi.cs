using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// First-pass bot choreography: mechanically safe auto-solver.
// A named NA/EU PF strategy can replace/add to this once its exact Fulgent
// movement is verified against live logs.
public sealed class FruP5FulgentBladeAi : IScenarioAi<FruP5FulgentBladeState>
{
    public string Name => "Auto-safe (development)";
    public string? Group => "Dev";

    public void Run(FruP5FulgentBladeState state, SimWorld world)
    {
        var party = world.Party;

        for (var i = 0; i < state.BotSafePath.Count; i++)
        {
            var safe = state.BotSafePath[i];
            // Give the party a long preposition window for the first bundle.
            // Later moves are all under ~6y and get 1s at 9y/s.
            var moveAt = i == 0 ? 8f : safe.SnapshotTime - 1f;
            var target = safe.Position;
            world.Events.Add(moveAt, () => MoveBots(party, target));
        }
    }

    private static void MoveBots(SimParty party, Vector3 target)
    {
        for (var i = 0; i < 8; i++)
        {
            var role = (PartyRole)i;
            if (role == party.PlayerRole) continue;
            party.Get(role)?.MoveTo(target, 9f);
        }
    }
}
