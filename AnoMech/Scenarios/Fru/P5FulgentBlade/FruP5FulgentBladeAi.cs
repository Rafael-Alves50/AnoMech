using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;

namespace AnoMech.Scenarios.Fru.P5FulgentBlade;

// Bot choreography ported from WCGH FRU-Sim's fb_positions.gd.
// The human player is never moved by AI.
public sealed class FruP5FulgentBladeAi : IScenarioAi<FruP5FulgentBladeState>
{
    public string Name => "NAUR / Waju Fulgent";
    public string? Group => "NA";

    private const float CosNne = 2.77f;
    private const float SinNne = 1.15f;

    private static readonly IReadOnlyDictionary<string, Vector3> DodgeOffsets =
        new Dictionary<string, Vector3>
        {
            ["ene"] = new(SinNne, 0f, CosNne),
            ["wsw"] = new(-SinNne, 0f, -CosNne),
            ["sse"] = new(-CosNne, 0f, SinNne),
            ["nnw"] = new(CosNne, 0f, -SinNne),
        };

    private static readonly string[] EastFirstPattern = ["wsw", "ene", "sse", "nnw", "ene", "wsw"];
    private static readonly string[] WestFirstPattern = ["ene", "wsw", "sse", "nnw", "wsw", "ene"];

    public void Run(FruP5FulgentBladeState state, SimWorld world)
    {
        var party = world.Party;

        // Same timestamps as the reference FRU-Sim Fulgent sequence.
        world.Events.Add(8.0f, () => MoveBots(party, state.ControllerPosition, 8f));

        var pattern = state.EastFirst ? EastFirstPattern : WestFirstPattern;
        var times = new[] { 12f, 16f, 18f, 20f, 22f, 24f };

        for (var i = 0; i < times.Length; i++)
        {
            var target = state.ToControllerSpace(DodgeOffsets[pattern[i]]);
            world.Events.Add(times[i], () => MoveBots(party, target, 8f));
        }
    }

    private static void MoveBots(SimParty party, Vector3 target, float speed)
    {
        for (var i = 0; i < 8; i++)
        {
            var role = (PartyRole)i;
            if (role == party.PlayerRole) continue;
            party.Get(role)?.MoveTo(target, speed);
        }
    }
}
