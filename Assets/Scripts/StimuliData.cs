using System.Collections.Generic;

public class Stimulus
{
    public string Operation;
    public int Result;
    public bool IsTarget;

    public Stimulus(string operation, int result, bool isTarget)
    {
        Operation = operation;
        Result = result;
        IsTarget = isTarget;
    }
}

public static class StimuliData
{
    // ?? 0-back (target = 50) ??????????????????????????????????????????????????

    public static List<Stimulus> List0Back = new List<Stimulus>
    {
        new Stimulus("30 + 20", 50,  true),
        new Stimulus("75 - 10", 65,  false),
        new Stimulus("45 + 5",  50,  true),
        new Stimulus("60 + 25", 85,  false),
        new Stimulus("95 - 45", 50,  true),
        new Stimulus("20 + 35", 55,  false),
        new Stimulus("80 - 15", 65,  false),
        new Stimulus("70 - 20", 50,  true),
        new Stimulus("15 + 60", 75,  false),
        new Stimulus("40 + 30", 70,  false),
        new Stimulus("55 + 20", 75,  false),
        new Stimulus("25 + 45", 70,  false),
    };

    // ?? 1-back variante A (target = 35) ???????????????????????????????????????

    public static List<Stimulus> List1BackA = new List<Stimulus>
    {
        new Stimulus("60 - 15", 45,  false),
        new Stimulus("55 + 30", 85,  false),
        new Stimulus("40 + 25", 65,  false),
        new Stimulus("80 - 25", 55,  false),
        new Stimulus("20 + 15", 35,  true),
        new Stimulus("70 - 35", 35,  true),
        new Stimulus("50 + 30", 80,  false),
        new Stimulus("25 + 45", 70,  false),
        new Stimulus("60 - 25", 35,  true),
        new Stimulus("15 + 20", 35,  true),
        new Stimulus("45 + 30", 75,  false),
        new Stimulus("35 + 30", 65,  false),
    };

    // ?? 1-back variante B (target = 35) ???????????????????????????????????????

    public static List<Stimulus> List1BackB = new List<Stimulus>
    {
        new Stimulus("75 - 30", 45,  false),
        new Stimulus("10 + 25", 35,  true),
        new Stimulus("50 - 15", 35,  true),
        new Stimulus("65 + 20", 85,  false),
        new Stimulus("30 + 40", 70,  false),
        new Stimulus("85 - 30", 55,  false),
        new Stimulus("40 + 35", 75,  false),
        new Stimulus("25 + 10", 35,  true),
        new Stimulus("70 - 35", 35,  true),
        new Stimulus("55 + 25", 80,  false),
        new Stimulus("20 + 45", 65,  false),
        new Stimulus("60 + 15", 75,  false),
    };

    // ?? 1-back variante C (target = 35) ???????????????????????????????????????

    public static List<Stimulus> List1BackC = new List<Stimulus>
    {
        new Stimulus("85 - 25", 60,  false),
        new Stimulus("30 + 45", 75,  false),
        new Stimulus("15 + 20", 35,  true),
        new Stimulus("60 - 25", 35,  true),
        new Stimulus("50 + 30", 80,  false),
        new Stimulus("75 - 20", 55,  false),
        new Stimulus("25 + 40", 65,  false),
        new Stimulus("90 - 40", 50,  false),
        new Stimulus("70 + 15", 85,  false),
        new Stimulus("20 + 15", 35,  true),
        new Stimulus("55 - 20", 35,  true),
        new Stimulus("40 + 30", 70,  false),
    };

    // ?? 2-back variante A (target = 40) ???????????????????????????????????????

    public static List<Stimulus> List2BackA = new List<Stimulus>
    {
        new Stimulus("70 - 25", 45,  false),
        new Stimulus("55 + 30", 85,  false),
        new Stimulus("25 + 15", 40,  true),
        new Stimulus("80 - 15", 65,  false),
        new Stimulus("95 - 55", 40,  true),
        new Stimulus("30 + 45", 75,  false),
        new Stimulus("60 + 15", 75,  false),
        new Stimulus("65 - 25", 40,  true),
        new Stimulus("50 + 25", 75,  false),
        new Stimulus("20 + 20", 40,  true),
        new Stimulus("45 + 30", 75,  false),
        new Stimulus("35 + 30", 65,  false),
    };

    // ?? 2-back variante B (target = 40) ???????????????????????????????????????

    public static List<Stimulus> List2BackB = new List<Stimulus>
    {
        new Stimulus("55 + 20", 75,  false),
        new Stimulus("15 + 25", 40,  true),
        new Stimulus("60 + 25", 85,  false),
        new Stimulus("30 + 10", 40,  true),
        new Stimulus("70 - 15", 55,  false),
        new Stimulus("85 - 20", 65,  false),
        new Stimulus("95 - 55", 40,  true),
        new Stimulus("25 + 50", 75,  false),
        new Stimulus("20 + 20", 40,  true),
        new Stimulus("45 + 30", 75,  false),
        new Stimulus("60 - 15", 45,  false),
        new Stimulus("35 + 45", 80,  false),
    };

    // ?? 2-back variante C (target = 40) ???????????????????????????????????????

    public static List<Stimulus> List2BackC = new List<Stimulus>
    {
        new Stimulus("30 + 35", 65,  false),
        new Stimulus("75 - 30", 45,  false),
        new Stimulus("55 + 30", 85,  false),
        new Stimulus("25 + 15", 40,  true),
        new Stimulus("60 + 15", 75,  false),
        new Stimulus("80 - 40", 40,  true),
        new Stimulus("70 - 25", 45,  false),
        new Stimulus("35 + 30", 65,  false),
        new Stimulus("20 + 20", 40,  true),
        new Stimulus("55 + 20", 75,  false),
        new Stimulus("95 - 55", 40,  true),
        new Stimulus("45 + 30", 75,  false),
    };

    // ?? Groupes par condition ??????????????????????????????????????????????????

    public static List<List<Stimulus>> Variants0Back = new List<List<Stimulus>>
    {
        List0Back
    };

    public static List<List<Stimulus>> Variants1Back = new List<List<Stimulus>>
    {
        List1BackA, List1BackB, List1BackC
    };

    public static List<List<Stimulus>> Variants2Back = new List<List<Stimulus>>
    {
        List2BackA, List2BackB, List2BackC
    };
}