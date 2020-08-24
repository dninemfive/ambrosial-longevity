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
        public float AgeFloor, AgeCeiling, ToleranceDivisor, AgeChangePerInterval, TargetAge;

        public override void CompPostMake()
        {
            base.CompPostMake();
            SetAges();
            Update();
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

        public void Update()
        {
            Hediff tolerance = base.Pawn.health.hediffSet.GetFirstHediffOfDef(Props.toleranceDef);
            ToleranceDivisor = Mathf.Clamp(Props.toleranceEffectCurve.Evaluate(tolerance?.Severity ?? 0f), Props.minToleranceFactor, Props.maxToleranceFactor);
            AgeChangePerInterval = Props.severityEffectCurve.Evaluate(base.parent.Severity) / ToleranceDivisor; 
            Pawn_AgeTracker at = base.Pawn.ageTracker;
            TargetAge = AgeFloor + ((at.AgeChronologicalYearsFloat - at.AgeBiologicalYearsFloat) % (AgeCeiling - AgeFloor));
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            ticksTo--;
            if (ticksTo <= 0)
            {
                //TODO: subtract partial year amounts using floats so I don't need all this <= 0 ? 1 business
                int ageY = base.Pawn.ageTracker.AgeBiologicalYears;
                if (ageY < targetAge) base.Pawn.ageTracker.AgeBiologicalTicks += GenDate.TicksPerYear;
                else if (ageY > targetAge) base.Pawn.ageTracker.AgeBiologicalTicks -= GenDate.TicksPerYear;
                if (ageY < ageCeiling) RemoveRandOldAgeHediff();
                //TODO: Facial Stuff compat (make hair less gray, remove wrinkles); assign random non-gray hair colors for vanilla?
                //wrt above PawnHairColors.RandomHairColor(pawn.story.SkinColor, pawn.ageTracker.AgeBiologicalYears);
                Reset();
            }
        }

        public void RemoveRandOldAgeHediff()
        {
            if (base.Pawn.health.hediffSet.hediffs.Where(IsOldAgeRelated).TryRandomElement(out Hediff hediff))
            {
                if (hediff != null)
                {
                    hediff.Severity = 0f;
                    if (PawnUtility.ShouldSendNotificationAbout(base.Pawn))
                    {
                        Messages.Message("MessageOldAgeDiseaseHealed".Translate(base.parent.LabelCap, base.Pawn.Possessive(), base.Pawn.Named("PAWN"), hediff.Label), base.Pawn, MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
        }

        private bool IsOldAgeRelated(Hediff h)
        {
            return h.def.HasModExtension<AmbrosiaRemoves>();
        }
    }
    class HediffCompProperties_Longevity : HediffCompProperties
    {
#pragma warning disable CS0649
        public float defaultAgeFloor = 20f, defaultAgeCeiling = 27f, minHours = 3f, maxHours = 9f, minToleranceFactor = 0.5f, maxToleranceFactor = 20f;
        public int tickInterval = 250;
        public HediffDef toleranceDef, withdrawalDef;
        public SimpleCurve toleranceEffectCurve, severityEffectCurve;
#pragma warning restore CS0649
        public HediffCompProperties_Longevity()
        {
            base.compClass = typeof(HediffComp_Longevity);
        }
    }
}
