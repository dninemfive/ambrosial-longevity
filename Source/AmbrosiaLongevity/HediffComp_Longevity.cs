using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace AmbrosiaLongevity
{
    class HediffComp_Longevity : HediffComp
    {
        public HediffCompProperties_Longevity Props => (HediffCompProperties_Longevity)base.props;
        public float AgeFloor, AgeCeiling;

        public override void CompPostMake()
        {
            base.CompPostMake();
            SetAges();
            Reset();
        }

        public void SetAges()
        {
            AgeFloor = (int)Mathf.Max(1f, GetAgeFloor());
            AgeCeiling = (int)Mathf.Max(AgeFloor * 1.5f, 2f);
            if (AgeFloor == AgeCeiling) AgeCeiling++;
        }

        public float GetAgeFloor()
        {
            List<LifeStageAge> lsas = base.Pawn.def.race.lifeStageAges;            
            float ret = Props.defaultAgeFloor;
            if (!lsas.NullOrEmpty())
            {
                lsas.Reverse();
                foreach (LifeStageAge lsa in lsas)
                {
                    if (lsa.minAge <= base.Pawn.def.race.lifeExpectancy * .5f)
                    {
                        ret = (int)lsa.minAge;
                        break;
                    }
                }
            }
            return ret;
        }
    }
    class HediffCompProperties_Longevity : HediffCompProperties
    {
#pragma warning disable CS0649
        public float defaultAgeFloor = 20f, defaultAgeCeiling = 27f, minHours = 3f, maxHours = 9f;
        public HediffDef highDef, withdrawalDef;
#pragma warning restore CS0649
        public HediffCompProperties_Longevity()
        {
            base.compClass = typeof(HediffComp_Longevity);
        }
    }
}
