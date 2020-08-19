using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace AmbrosiaLongevity
{
    class HediffComp_Longevity : HediffComp
    {
        public override void CompPostMake()
        {
            base.CompPostMake();
            GetAges(out int tAgeFloor, out int tAgeCeiling);
            ageFloor = tAgeFloor;
            ageCeiling = tAgeCeiling;
            Reset();
        }
    }
    class HediffCompProperties_Longevity : HediffCompProperties
    {
#pragma warning disable CS0649
        public int defaultAgeFloor = 20, defaultAgeCeiling = 27, minHours = 3, maxHours = 9;
        public HediffDef highDef, withdrawalDef;
#pragma warning restore CS0649
        public HediffCompProperties_Longevity()
        {
            base.compClass = typeof(HediffComp_Longevity);
        }
    }
}
