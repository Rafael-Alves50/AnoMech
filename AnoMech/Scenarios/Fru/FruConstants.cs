using System;

namespace AnoMech.Scenarios.Fru;

// IDs and geometry used by FRU scenarios.
// Action/BNpc ids are taken from the live FRU encounter data used by BossMod.
// Fulgent Blade movement geometry/timing follows the GPL-3.0 WCGH FRU-Sim implementation.
public static class FruConstants
{
    public const byte Level = 100;

    public static class BNpcBaseId
    {
        public const uint Helper = 0x233C;  // generic invisible encounter helper
        public const uint Pandora = 0x45AF; // P5 boss
    }

    public static class ActionId
    {
        public const uint FulgentBlade = 40306;
        public const uint PathOfLightFirst = 40307;
        public const uint PathOfLightRest = 40308;
        public const uint PathOfDarknessFirst = 40118;
        public const uint PathOfDarknessRest = 40309;
    }

    public static class Geometry
    {
        public const float ArenaRadius = 22f;

        // WCGH FRU-Sim P5 wave width. The visual bar is 140y long, so inside a
        // 22y-radius arena we can treat it as infinite across its long axis.
        public const float ExalineWidth = 11.855f;
        public const int ExalineHits = 7;

        // Controller anchors from WCGH FRU-Sim.
        public static readonly (float X, float Z)[] ControllerAnchors =
        [
            (0f, 17f),
            (0f, -17f),
            (17f, 0f),
            (-17f, 0f),
        ];
    }
}
