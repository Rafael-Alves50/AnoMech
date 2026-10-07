using System;
using System.Numerics;
using AnoMech.Core.Game;

namespace AnoMech.Scenarios.Ucob;

public static class UcobConstants
{
    public const byte Level = 70;
    public const ushort ItemLevel = 345;

    public static class BNpcBaseId
    {
        public const uint Twintania = 0x1FDF;
        public const uint Oviform = 0x1FE0;
        public const uint Nael = 0x1FE1;
        public const uint NaelGeminus = 0x1FE2;
        public const uint Firehorn = 0x1FE3;
        public const uint Iceclaw = 0x1FE4;
        public const uint Thunderwing = 0x1FE5;
        public const uint TailOfDarkness = 0x1FE6;
        public const uint FangOfLight = 0x1FE7;
        public const uint BahamutPrime = 0x1FE8;
        public const uint Phoenix = 0x1FE9;
        public const uint Helper = 0x18D6;
    }

    public static class EObjId
    {
        public const uint Twister = 0x1E8910;
        public const uint LiquidHell = 0x1E88FE;
        public const uint Neurolink = 0x1E88FF;
        public const uint Salvation = 0x1E91D4;
        public const uint Hypernova = 0x1E91C1;
        public const uint BahamutMoon = 0x1EA7E5;
        public const uint EarthShaker = 0x1E9663;
    }

    public static class ActionId
    {
        public const uint Plummet = 9896;
        public const uint DeathSentence = 9897;
        public const uint Twister = 9898;
        public const uint TwisterAOE = 9899;
        public const uint Fireball = 9900;
        public const uint LiquidHell = 9901;
        public const uint Generate = 9902;
        public const uint Hatch = 9903;
        public const uint AethericProfusion = 9905;
        public const uint TwistingDive = 9906;

        public const uint BahamutsClaw = 9909;
        public const uint Ravensbeak = 9910;
        public const uint Heavensfall = 9912;
        public const uint ThermionicBurst = 9913;
        public const uint MegaflareRaidwide = 9914;
        public const uint IronChariot = 9915;
        public const uint LunarDynamo = 9916;
        public const uint ThermionicBeam = 9917;
        public const uint RavenDive = 9918;
        public const uint Hypernova = 9919;
        public const uint MeteorStream = 9920;
        public const uint DalamudDive = 9921;
        public const uint BahamutsFavor = 9922;
        public const uint LunarDive = 9923;
        public const uint FireballP2 = 9925;
        public const uint Iceball = 9926;
        public const uint ChainLightning = 9927;
        public const uint ChainLightningAOE = 9928;
        public const uint Deathstorm = 9929;
        public const uint WingsOfSalvation = 9930;
        public const uint Cauterize1 = 9931;
        public const uint Cauterize2 = 9932;
        public const uint Cauterize3 = 9933;
        public const uint Cauterize4 = 9934;
        public const uint Cauterize5 = 9935;

        public const uint SeventhUmbralEra = 9937;
        public const uint CalamitousFlame = 9938;
        public const uint CalamitousBlaze = 9939;
        public const uint FlareBreath = 9940;
        public const uint Flatten = 9941;
        public const uint Gigaflare = 9942;
        public const uint TempestWing = 9943;
        public const uint TempestWingAOE = 9944;
        public const uint EarthShaker = 9945;
        public const uint EarthShakerAOE = 9946;
        public const uint MegaflareSpread = 9948;
        public const uint MegaflarePuddle = 9949;
        public const uint MegaflareStack = 9950;
        public const uint MegaflareTower = 9951;
        public const uint MegaflareStrike = 9952;
        public const uint MegaflareDive = 9953;
        public const uint QuickmarchTrio = 9954;
        public const uint BlackfireTrio = 9955;
        public const uint FellruinTrio = 9956;
        public const uint HeavensfallTrio = 9957;
        public const uint TenstrikeTrio = 9958;
        public const uint GrandOctet = 9959;

        public const uint MornAfah = 9964;
        public const uint Enrage = 9965;
        public const uint EnrageAOE = 9966;
        public const uint Exaflare = 9967;
        public const uint ExaflareFirst = 9968;
        public const uint ExaflareRest = 9969;
        public const uint AkhMorn = 9962;
        public const uint AkhMornAOE = 9963;
    }

    public static class Geometry
    {
        public const float ArenaRadius = 21f;
        public const float TwisterRadius = 1.25f;
        public const float LiquidHellRadius = 6f;
        public const float FireballRadius = 4f;
        public const float HatchRadius = 8f;
        public const float ThermionicBeamRadius = 4f;
        public const float HypernovaRadius = 5f;
        public const float MeteorStreamRadius = 4f;
        public const float ChariotRadius = 8.55f;
        public const float DynamoInner = 6f;
        public const float DynamoOuter = 22f;
        public const float DiveHalfWidthTwinNael = 4f;
        public const float DiveHalfWidthBahamut = 6f;
        public const float DrakeDiveHalfWidth = 10f;
        public const float EarthShakerHalfAngle = MathF.PI / 4f;
        public const float ExaflareRadius = 6f;
        public const float ExaflareStep = 8f;
        public const float ExaflareInterval = 1.5f;
    }

    public static readonly Waymark[] NaurWaymarks =
    [
        new(WaymarkSlot.A,     new Vector3( 20.986f, 0f, -11.020f)),
        new(WaymarkSlot.B,     new Vector3(  9.315f, 0f,  21.971f)),
        new(WaymarkSlot.C,     new Vector3(-19.999f, 0f,  11.772f)),
        new(WaymarkSlot.D,     new Vector3(  0.000f, 0f,   9.000f)),
        new(WaymarkSlot.One,   new Vector3(  0.000f, 0f,  -8.000f)),
        new(WaymarkSlot.Two,   new Vector3( -8.000f, 0f,   5.000f)),
        new(WaymarkSlot.Three, new Vector3(  8.000f, 0f,   5.000f)),
        new(WaymarkSlot.Four,  new Vector3(  0.000f, 0f,   0.000f)),
    ];
}
