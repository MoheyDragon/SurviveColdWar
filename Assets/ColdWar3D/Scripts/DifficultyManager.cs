using UnityEngine;

// Every number that shapes the game's difficulty, in one asset you can tune from the Inspector.
// Select Assets/ColdWar3D/Resources/DifficultyManager. Changes made during play mode are kept.
// The tutorial is scripted around its own fixed numbers and ignores most of these.
[CreateAssetMenu(fileName = "DifficultyManager", menuName = "Cold War/Difficulty Manager")]
public class DifficultyManager : ScriptableObject
{
    const string ResourcePath = "DifficultyManager";
    static DifficultyManager current;

    // The asset in Resources, or default values if it is missing.
    public static DifficultyManager Current
    {
        get
        {
            if (current == null)
                current = Resources.Load<DifficultyManager>(ResourcePath);
            if (current == null)
                current = CreateInstance<DifficultyManager>();
            return current;
        }
    }

    public const int FirstYear = 1947;
    public const int LastYear = 1991;

    [Header("War limit: how many times stronger one side can be before nuclear war")]
    [Tooltip("Across: the year (1947 to 1991). Up: how many times stronger one side may be before nuclear war.\n" +
             "Drag the points to reshape it, right-click to add or delete points. After the last point the limit stays at its final value (endless mode).")]
    public AnimationCurve warLimit = DefaultWarLimit();

    static AnimationCurve DefaultWarLimit()
    {
        // 25x in 1947, about 11x by 1957, 6x by 1967, 3.6x by 1977, 2.5x by 1985 and 2x by 1991.
        var keys = new[]
        {
            new Keyframe(1947f, 25f), new Keyframe(1957f, 11f), new Keyframe(1967f, 6f),
            new Keyframe(1977f, 3.6f), new Keyframe(1985f, 2.5f), new Keyframe(1991f, 2f),
        };

        // Smooth slopes through each point, so the line flows from one point to the next.
        for (int i = 0; i < keys.Length; i++)
        {
            Keyframe before = keys[Mathf.Max(0, i - 1)];
            Keyframe after = keys[Mathf.Min(keys.Length - 1, i + 1)];
            float slope = (after.value - before.value) / Mathf.Max(0.001f, after.time - before.time);
            keys[i].inTangent = slope;
            keys[i].outTangent = slope;
        }

        return new AnimationCurve(keys);
    }

    // The limit for a moment in the war, given as years since January 1947.
    public float WarLimitAt(float yearsElapsed)
    {
        if (warLimit == null || warLimit.length == 0)
            return 3f;
        return Mathf.Max(1.01f, warLimit.Evaluate(FirstYear + yearsElapsed));
    }

    [Header("Missile crisis")]
    [Tooltip("How long a missile crisis lasts, in months.")]
    public int crisisMonths = 6;
    [Tooltip("The war limit is multiplied by this during a crisis (0.7 makes it 30% tighter).")]
    [Range(0.1f, 1f)] public float crisisRatioFactor = 0.7f;
    [Tooltip("The war limit never goes below this, even during a crisis.")]
    public float minimumRatio = 1.5f;

    [Header("Power drift: each side's power wanders up or down every month")]
    [Tooltip("Largest random monthly change in 1947 (0.005 = up to 0.5%).")]
    [Range(0f, 0.2f)] public float driftStart = 0.005f;
    [Tooltip("Largest random monthly change by 1991.")]
    [Range(0f, 0.2f)] public float driftEnd = 0.04f;

    [Header("Chaos events")]
    [Tooltip("Chance each month that a random event strikes, in 1947.")]
    [Range(0f, 1f)] public float chaosChanceStart = 0.03f;
    [Tooltip("Chance each month that a random event strikes, by 1991.")]
    [Range(0f, 1f)] public float chaosChanceEnd = 0.2f;
    [Tooltip("Economic crash: share of money lost, in percent.")]
    public Vector2Int crashLoss = new Vector2Int(50, 80);
    [Tooltip("Arms race: the stronger side gains, and the weaker side loses, a random amount in this range, in percent. Other events always strike the weaker side.")]
    public Vector2Int armsRaceSwing = new Vector2Int(10, 35);
    [Tooltip("Spy network caught: share of spies lost, in percent.")]
    public Vector2Int spiesLost = new Vector2Int(30, 80);
    [Tooltip("Factory disaster: share of factories lost, in percent.")]
    public Vector2Int factoriesLost = new Vector2Int(20, 70);
    [Tooltip("Proxy war lost: share of wars lost, in percent.")]
    public Vector2Int warsLost = new Vector2Int(10, 40);

    [Header("Presidents")]
    [Tooltip("A candidate's monthly power, as a percentage of the party's power at the election.")]
    public Vector2 presidentPowerPercent = new Vector2(0.5f, 5f);
    [Tooltip("A candidate's monthly money, as a percentage of the party's money at the election.")]
    public Vector2 presidentMoneyPercent = new Vector2(0.2f, 3f);
    [Tooltip("A candidate's monthly happiness change.")]
    public Vector2Int presidentHappiness = new Vector2Int(-20, 20);
    [Tooltip("Chance that a candidate takes power away instead of adding it.")]
    [Range(0f, 1f)] public float negativePresidentChance = 0.01f;
    [Tooltip("Below this happiness, the people may overthrow the president.")]
    public int unpopularBelow = 20;
    [Tooltip("Happiness must stay below the line for more than this many months in a row before uprisings can start.")]
    public int unhappyMonthsBeforeUprising = 12;
    [Tooltip("Monthly chance of an uprising, once happiness has stayed below the line long enough.")]
    [Range(0f, 1f)] public float uprisingChance = 0.08f;
    [Tooltip("How long the presidency stays empty after a coup or an uprising, in months.")]
    public Vector2Int vacancyMonths = new Vector2Int(6, 24);
    [Tooltip("Happiness rises to at least this when a new president takes power after an empty seat.")]
    public int newLeaderHappiness = 40;

    [Header("Action power gains, as a percentage of the side's own power")]
    [Tooltip("Each proxy war adds this percentage every month.")]
    public float warGain = 0.5f;
    [Tooltip("Each spy adds this percentage every month.")]
    public float spyGain = 1f;
    [Tooltip("Each factory adds this percentage every month.")]
    public float factoryGain = 0.3f;
    [Tooltip("Finished research adds this percentage once.")]
    public float researchGain = 25f;
    [Tooltip("A president's research is worth this share of a normal one.")]
    [Range(0f, 1f)] public float agendaResearchShare = 0.5f;

    [Header("Action power costs, as a percentage of the side's own power")]
    public float warCost = 3f;
    public float spyCost = 5f;
    public float factoryCost = 2f;
    public float researchCost = 10f;
    public float assassinCost = 0f;
    [Tooltip("Each earlier use raises the power cost by this share of the base cost (0.1 = +10% per use).")]
    public float warCostPerUse = 0.1f;
    public float spyCostPerUse = 0.5f;
    public float factoryCostPerUse = 0.1f;
    public float researchCostPerUse = 0.2f;

    [Header("Money costs rising over the decades")]
    public bool risingMoneyCosts = true;
    [Tooltip("Money costs are multiplied by this by 1991, growing steadily from 1x in 1947.")]
    public float moneyCostBy1991 = 4f;

    [Header("Opportunities: only some actions are available each month")]
    public bool useOpportunities = true;
    [Tooltip("Fewest and most actions each side can use in a month, out of 6.")]
    public Vector2Int actionsPerMonth = new Vector2Int(3, 4);

    // 0 in 1947, 1 once the war limit reaches its end value.
    public float Era => GameManager.Singleton != null ? Mathf.Clamp01(GameManager.Singleton.YearsElapsed / (LastYear - FirstYear)) : 0f;
}
