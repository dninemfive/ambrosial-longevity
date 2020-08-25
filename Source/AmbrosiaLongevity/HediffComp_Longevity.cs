using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AmbrosiaLongevity
{
    class HediffComp_Longevity : HediffComp
    {
        public HediffCompProperties_Longevity Props => (HediffCompProperties_Longevity)base.props;
        public float AgeFloor, AgeCeiling, ToleranceFactor, AgeChangePerInterval, TargetAge, AgeScale;

        #region cheap hash interval stuff
        private int hashOffset = 0;
        public bool IsCheapIntervalTick(int interval) => (int)(Find.TickManager.TicksGame + hashOffset) % interval == 0;
        #endregion cheap hash interval stuff

        public override void CompPostMake()
        {
            base.CompPostMake();
            hashOffset = base.Pawn.thingIDNumber.HashOffset();
            // Scaling the tick change based on consuming one ambrosia
            AgeScale = (Props.targetYearsPerYear / GenDate.TicksPerYear) / (Props.severityEffectCurve.Evaluate(0.5f) * Props.toleranceEffectCurve.Evaluate(0.032f));
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
            ToleranceFactor = Mathf.Clamp(Props.toleranceEffectCurve.Evaluate(tolerance?.Severity ?? 0f), Props.minToleranceFactor, Props.maxToleranceFactor);
            AgeChangePerInterval = Props.severityEffectCurve.Evaluate(base.parent.Severity) * ToleranceFactor; 
            Pawn_AgeTracker at = base.Pawn.ageTracker;
            TargetAge = AgeFloor + ((at.AgeChronologicalYearsFloat - at.AgeBiologicalYearsFloat) % (AgeCeiling - AgeFloor));
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (IsCheapIntervalTick(Props.tickInterval))
            {
                Log.Message("Age change: " + AgeChangePerInterval);
                Pawn_AgeTracker at = base.Pawn.ageTracker;
                if (at.AgeBiologicalYearsFloat > TargetAge)
                {
                    at.AgeBiologicalTicks -= (long)(AgeChangePerInterval * AgeScale);
                    // TODO: if Facial Stuff, regenerate hair for younger age
                }
                else
                {
                    RemoveRandOldAgeHediff();
                    // TODO: if not Facial Stuff, assign random non-gray hair color
                    // PawnHairColors.RandomHairColor(pawn.story.SkinColor, pawn.ageTracker.AgeBiologicalYears);
                }
                Update();
            }
        }

        public void RemoveRandOldAgeHediff()
        {
            if (base.Pawn.health.hediffSet.hediffs.Where(h => h.def.HasModExtension<AmbrosiaRemoves>()).TryRandomElement(out Hediff hediff))
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
    }
    class HediffCompProperties_Longevity : HediffCompProperties
    {
#pragma warning disable CS0649
        public float defaultAgeFloor = 20f, defaultAgeCeiling = 27f, minToleranceFactor = 0.5f, maxToleranceFactor = 20f, targetYearsPerYear = 30f;
        public int tickInterval = 250;
        public HediffDef toleranceDef;
        public SimpleCurve toleranceEffectCurve, severityEffectCurve;
#pragma warning restore CS0649
        public HediffCompProperties_Longevity()
        {
            base.compClass = typeof(HediffComp_Longevity);
        }
    }
}
