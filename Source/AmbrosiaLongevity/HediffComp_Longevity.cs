using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace AmbrosiaLongevity
{
    public class HediffComp_Longevity : HediffComp
    {
        private int ticksTo, targetAge;
        private int dAgeFloor => Props.defaultAgeFloor;
        private int dAgeCeil => Props.defaultAgeCeiling;
        private int ageFloor, ageCeiling;
        private int minHours => Props.minHours;
        private int maxHours => Props.maxHours;
        private double tolFactor;
        public HediffCompProperties_Longevity Props => (HediffCompProperties_Longevity)base.props;
        public override void CompPostMake()
        {
            base.CompPostMake();
            GetAges(out int tAgeFloor, out int tAgeCeiling);
            ageFloor = tAgeFloor;
            ageCeiling = tAgeCeiling;
            reset();
        }
        private void reset()
        {
            //TODO: make withdrawal age pawn (~10yr from beginning to end of normal withdrawal)
            Hediff tol = base.Pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.AmbrosiaTolerance);
            tolFactor = 1f;
            if (tol != null) tolFactor = 1 / Mathf.Max(EffectFactorSeverityCurve.Evaluate(tol.Severity), 0.05f);
            ticksTo = (int)((1-base.parent.Severity) * tolFactor * (Rand.Range(minHours, maxHours) * GenDate.TicksPerHour));            
            Pawn_AgeTracker at = base.Pawn.ageTracker;
            targetAge = ageFloor + ((at.AgeChronologicalYears - at.AgeBiologicalYears) % (ageCeiling - ageFloor));
        }
        //copied from HediffComp_DrugEffectFactor to bypass the privacy restriction on getting the factor
        private static readonly SimpleCurve EffectFactorSeverityCurve = new SimpleCurve
        {
            {
                new CurvePoint(0f, 1f),
                true
            },
            {
                new CurvePoint(1f, 0.25f),
                true
            }
        };
        public override void CompPostTick(ref float severityAdjustment)
        {
            ticksTo--;
            if(ticksTo <= 0)
            {
                //TODO: subtract partial year amounts using floats so I don't need all this <= 0 ? 1 business
                int ageY = base.Pawn.ageTracker.AgeBiologicalYears;    
                if (ageY < targetAge) base.Pawn.ageTracker.AgeBiologicalTicks += GenDate.TicksPerYear;
                else if (ageY > targetAge) base.Pawn.ageTracker.AgeBiologicalTicks -= GenDate.TicksPerYear;
                if (ageY < ageCeiling) RemoveRandOldAgeHediff();
                //TODO: Facial Stuff compat (make hair less gray, remove wrinkles); assign random non-gray hair colors for vanilla?
                //wrt above PawnHairColors.RandomHairColor(pawn.story.SkinColor, pawn.ageTracker.AgeBiologicalYears);
                reset();
            }
        }
        public void RemoveRandOldAgeHediff()
        {
            
            if (base.Pawn.health.hediffSet.hediffs.Where(isOldAgeRelated).TryRandomElement(out Hediff hediff))
            {
                if(hediff != null)
                {
                    hediff.Severity = 0f;
                    if (PawnUtility.ShouldSendNotificationAbout(base.Pawn))
                    {
                        Messages.Message("MessageOldAgeDiseaseHealed".Translate(base.parent.LabelCap, base.Pawn.Possessive(), base.Pawn.LabelShort, hediff.Label, base.Pawn.Named("PAWN")), base.Pawn, MessageTypeDefOf.PositiveEvent, true);
                    }
                }                
            }
        }
        private bool isOldAgeRelated(Hediff h)
        {
            
            return h.def.HasModExtension<AmbrosiaRemoves>();
        }
        private void GetAges(out int ageFloor, out int ageCeiling)
        {
            ageFloor = (int)Mathf.Max(1f, getAgeFloor());
            ageCeiling = (int)Mathf.Max(ageFloor * 1.5f, 2f);
            if (ageFloor == ageCeiling) ageCeiling++;
        }
        private int getAgeFloor()
        {
            List<LifeStageAge> lsas = base.Pawn.def.race.lifeStageAges;
            int ret = dAgeFloor;
            if(lsas != null)for(int i = lsas.Count-1; i >= 0; i--)
                {
                    if(lsas.ElementAt(i).minAge <= base.Pawn.def.race.lifeExpectancy * .5f)
                    {
                        ret = (int)lsas.ElementAt(i).minAge;
                        break;
                    }
                }
            return ret;
        }
        public override void CompExposeData()
        {
            Scribe_Values.Look(ref ticksTo, "ticksToHeal", 0, false);
            Scribe_Values.Look(ref targetAge, "targetAge", 0, false);
            Scribe_Values.Look(ref ageFloor, "ageFloor", dAgeFloor, false);
            Scribe_Values.Look(ref ageCeiling, "ageCeiling", dAgeCeil, false);
        }
        public override string CompDebugString()
        {
            return "ticksToReduce: " + ticksTo + "\ntargetAge: " + targetAge + "\ntolFactor: " + tolFactor + "\nsevFactor: " + (1-base.parent.Severity) + " (severity: " + base.parent.Severity + ")";
        }
    }
}
/*old comments I want to keep but don't want cluttering up readability:
    //IEnumerable<HediffGiver_Birthday> hg = h.def.hediffGivers.OfType<HediffGiver_Birthday>();
    //return hg != null && hg.Any();
    //IEnumerable<Hediff> oldHediffs = from h in Pawn.health.hediffSet.hediffs where h.def.hediffGivers.OfType<HediffGiver_Birthday>().Any() select h;
    //OLD (shouldn't be needed given ModExtension approach) TODO: Include Fluffy_BirdsAndBees.HediffGiver_Birthday_Gender, don't include Logann healing factor
    //targetAge = Rand.Range(20, 33); //TODO: Alien Race compat by making min be age of adulthood and proportionally scaling max
*/
