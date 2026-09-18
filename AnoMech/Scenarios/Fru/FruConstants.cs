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

    public static class BNpcNameId
    {
        public const uint Pandora = 13561;
    }

    public static class EObjId
    {
        // FRU P5 Fulgent Blade line / octagram edge EventObject.
        public const uint FulgentBladeLine = 2014199;
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

        // Path of Light/Darkness is a 5y-deep, 80y-wide rectangle. Subsequent
        // exaline hits advance by exactly 5y every 2s.
        public const float ExalineStep = 5f;
        public const float ExalineActiveRadius = 21f;
        public const float ExalineHitboxMargin = 0.35f;

        // Offset of the six-line octagram's centre from the arena centre: 5*sqrt(2).
        public const float StarCenterRadius = 7.0710678f;
    }
}
