using ACS_View.Domain.Enums;
using ACS_View.Domain.Entities;
using ACS_View.Domain.ValueObjects;

internal static class VisitFollowUpTests
{
    public static void Run()
    {
        var today = new DateTime(2026,11,5);
        int checks = 0;
        void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
        foreach (var line in new[] {VisitCareLineType.Diabetes,VisitCareLineType.Hypertension,VisitCareLineType.Elderly})
        {
            VisitRuleEvaluation E(params DateTime[] visits) => VisitFollowUpCalculator.Evaluate(line,null,null,visits,false,today);
            Check(E().Missing==2 && E().Status==VisitFollowUpStatus.Recommended,"No visits is not compliance");
            Check(E(today.AddDays(-45)).Completed==1,"One completed day");
            foreach (var gap in new[]{0,29,30,31,60})
                Check(E(today.AddDays(-70),today.AddDays(-70+gap)).Completed==(gap>=30?2:1),"Minimum interval inclusive");
            Check(E(today.AddDays(-60),today.AddDays(-40),today.AddDays(-20)).Completed==2,"Greedy selection validates intermediate gaps");
            Check(E(today.AddYears(-1)).Completed==1,"Inclusive annual lower boundary");
            Check(E(today.AddYears(-1).AddDays(-1)).Completed==0,"Expired lower boundary");
            Check(E(today.AddDays(1)).Completed==0,"Future excluded");
            var march = new DateTime(2026,3,10); var april = new DateTime(2026,4,20);
            Check(E(march,april).Completed==2 && E(march,april).Deadline==new DateTime(2027,3,10),"No monthly reset and exact last compliant day");
            Check(VisitFollowUpCalculator.Evaluate(line,null,null,[march,april],false,new DateTime(2027,3,11)).Completed==1,"Expiry changes result without edits");
            Check(VisitFollowUpCalculator.Evaluate(line,null,null,[new DateTime(2025,12,1),new DateTime(2026,1,1)],false,today).Completed==2,"Year change");
            var leap=VisitFollowUpCalculator.Evaluate(line,null,null,[new DateTime(2024,2,29),new DateTime(2024,4,1)],false,new DateTime(2025,2,28));
            Check(leap.Deadline==new DateTime(2025,2,28),"Leap-day expiry uses inverse calendar boundary");
        }
        var birth = new DateTime(2026,1,1);
        VisitRuleEvaluation Child(DateTime reference, params DateTime[] visits) => VisitFollowUpCalculator.Evaluate(VisitCareLineType.Child,birth,null,visits,false,reference);
        Check(Child(birth).Missing==2,"Newborn pending");
        Check(Child(birth,birth).RecommendedDate==birth.AddDays(1),"Next visit must use a distinct day");
        Check(Child(birth.AddDays(30),birth.AddDays(30)).Completed==1,"30th day inclusive");
        Check(Child(birth.AddDays(31),birth.AddDays(31)).Completed==0,"Late visit cannot repair first milestone");
        Check(Child(birth.AddMonths(6),birth.AddDays(20),birth.AddMonths(6)).Completed==2,"Six-month boundary inclusive");
        Check(Child(birth.AddMonths(7),birth.AddDays(20),birth.AddMonths(6).AddDays(1)).Completed==1,"Late second visit excluded");
        Check(Child(birth.AddMonths(7),birth.AddDays(31),birth.AddDays(60)).Completed==1,"Second milestone tracked independently");
        Check(Child(birth.AddMonths(7),birth.AddDays(20),birth.AddDays(20)).Completed==1,"Duplicate dates excluded");
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Child,null,null,[],false,birth).Status==VisitFollowUpStatus.InsufficientData,"Missing birth");
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Child,birth.AddDays(1),null,[],false,birth).Status==VisitFollowUpStatus.InsufficientData,"Future birth");
        var pregnancy = new PatientPregnancy { LastMenstrualPeriod=today.AddDays(-100),Status=PregnancyStatus.Active };
        for(int count=0;count<=3;count++)
        {
            var dates=Enumerable.Range(1,count).Select(i=>today.AddDays(-i*10)).Append(today.AddDays(-110));
            var result=VisitFollowUpCalculator.Evaluate(VisitCareLineType.Pregnancy,null,pregnancy,dates,false,today);
            Check(result.Completed==count && result.Deadline==null,"Prenatal episode count and no normative DPP deadline");
        }
        pregnancy.LastMenstrualPeriod=null;
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Pregnancy,null,pregnancy,[],false,today).Status==VisitFollowUpStatus.InsufficientData,"Unknown pregnancy start");
        pregnancy.ExpectedBirthDate=today.AddDays(100);
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Pregnancy,null,pregnancy,[today],false,today).Completed==1,"DPP estimates start");
        pregnancy.Status=PregnancyStatus.Ended; pregnancy.EndType=PregnancyEndType.Birth; pregnancy.EndedAt=today.AddDays(-5);
        var postpartum=VisitFollowUpCalculator.Evaluate(VisitCareLineType.Postpartum,null,pregnancy,[today.AddDays(-10)],false,today);
        Check(postpartum.Completed==0 && postpartum.Deadline==today.AddDays(37),"Prenatal does not satisfy postpartum");
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Postpartum,null,pregnancy,[today],false,today).Completed==1,"Postpartum visit counts");
        pregnancy.EndedAt=null;
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Postpartum,null,pregnancy,[today],false,today).Status==VisitFollowUpStatus.InsufficientData,"No invented birth");
        pregnancy.Status=PregnancyStatus.Ended;
        pregnancy.LastMenstrualPeriod=today.AddDays(-100);
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Pregnancy,null,pregnancy,[today],false,today).Status==VisitFollowUpStatus.InsufficientData,"Ended pregnancy does not imply active prenatal follow-up");
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Child,DateTime.MinValue,null,[],false,today).Status==VisitFollowUpStatus.InsufficientData,"Invalid birth sentinel");
        Check(Child(birth.AddMonths(7),birth.AddDays(31),birth.AddDays(60)).Milestones.Count==2,"Structured separate childhood milestones");
        Check(Child(birth.AddYears(2)).Status==VisitFollowUpStatus.NoSpecificRecommendation,"Second birthday eligibility boundary");
        pregnancy.EndedAt=today.AddDays(-42);
        Check(PregnancyCalculator.IsPuerperal(pregnancy,today),"Postpartum day 42 inclusive");
        Check(!PregnancyCalculator.IsPuerperal(pregnancy,today.AddDays(1)),"Postpartum day 43 excluded");
        pregnancy.Status=PregnancyStatus.Active;
        Check(VisitFollowUpCalculator.Evaluate(VisitCareLineType.Postpartum,null,pregnancy,[],false,today).Status==VisitFollowUpStatus.InsufficientData,"Active pregnancy is not proof of delivery");
        Console.WriteLine($"{checks} visit follow-up domain checks passed.");
    }
}
