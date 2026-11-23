using UnityEngine;

// Values for how the 3D board looks and behaves, kept apart from the Difficulty Manager (which holds the game rules).
// Edit the asset in Resources, or from the Test Panel window (Window > Cold War > Test Panel), also while playing.
[CreateAssetMenu(fileName = "GameTuning", menuName = "Cold War/Game Tuning")]
public class GameTuning : ScriptableObject
{
    const string ResourcePath = "GameTuning";

    // Test Panel switch: nuclear war never ends the game (the meter can sit at 100%). Never saved, off at every start.
    public static bool NoLose;
    static GameTuning current;

    // The asset in Resources, or default values if it is missing.
    public static GameTuning Current
    {
        get
        {
            if (current == null)
                current = Resources.Load<GameTuning>(ResourcePath);
            if (current == null)
                current = CreateInstance<GameTuning>();
            return current;
        }
    }

    [Header("Hidden levels (never shown to players; they change how units look)")]
    [Tooltip("How many zeros of power make one level. With 6: below 1M is level 1, 1M to 1T is level 2, 1T to 1Qi is level 3, and so on.")]
    [Range(1, 12)] public int zerosPerLevel = 6;
    [Tooltip("The highest level. Units stop changing above it.")]
    [Range(1, 10)] public int maxLevel = 5;
    [Tooltip("Factories and tank columns grow this much bigger with each level above 1 (0.1 = 10%).")]
    [Range(0f, 0.5f)] public float growthPerLevel = 0.1f;
    [Tooltip("The most factories and tank columns can grow (1.5 = 50% bigger).")]
    [Range(1f, 3f)] public float maxGrowth = 1.5f;

    [Header("Units shown on the board (0 = every one is shown)")]
    [Range(0, 200)] public int maxFactoriesShown = 0;
    [Range(0, 200)] public int maxWarsShown = 0;
    [Range(0, 200)] public int maxSpiesShown = 0;
    [Range(0, 200)] public int maxSatellitesShown = 0;

    [Header("Proxy wars")]
    [Tooltip("Chance that a proxy war sends warships instead of tanks, when there is enemy coast to reach.")]
    [Range(0f, 1f)] public float fleetChance = 0.4f;
    [Tooltip("Warships in each fleet.")]
    [Range(1, 4)] public int shipsPerFleet = 2;
    [Tooltip("Tanks in each column.")]
    [Range(1, 5)] public int tanksPerColumn = 3;
    [Tooltip("How far each pair of tanks drives behind the one in front (they form an arrowhead).")]
    [Range(0.3f, 2f)] public float tankGap = 0.9f;
    [Tooltip("How far each pair of tanks drives out to the side of the leader.")]
    [Range(0.2f, 2f)] public float tankSpread = 0.65f;
    [Tooltip("Gap between warships sailing in line.")]
    [Range(0.5f, 5f)] public float shipGap = 2.6f;
    [Tooltip("Sea kept clear around the nuke meter, beyond its rim, so ships sail round it.")]
    [Range(0f, 4f)] public float nukeMeterClearance = 1.2f;

    [Header("Unit sizes on the board (length of each model)")]
    [Tooltip("Applies to new units.")]
    [Range(0.2f, 2f)] public float tankSize = 0.6f;
    [Tooltip("Applies to new units.")]
    [Range(0.2f, 3f)] public float shipSize = 1.3f;
    [Tooltip("Applies to new units.")]
    [Range(0.1f, 2f)] public float satelliteSize = 0.4f;

    [Tooltip("Height of the research rocket on its pad.")]
    [Range(0.5f, 5f)] public float rocketHeight = 2.2f;

    [Header("Satellites")]
    [Tooltip("Height of the orbit up the capital, as a share of its height (1 = level with the top).")]
    [Range(0.3f, 1.2f)] public float satelliteHeight = 0.78f;
    [Tooltip("Space kept between a satellite and the capital (and its flags).")]
    [Range(0f, 3f)] public float satelliteClearance = 0.45f;
    [Tooltip("Each new satellite's circle is this much wider than the one before.")]
    [Range(0f, 2f)] public float satelliteStepOut = 0.35f;
    [Tooltip("Each new satellite flies this much higher than the one before (0 = all at one height).")]
    [Range(-1f, 2f)] public float satelliteStepUp = 0.4f;
    [Tooltip("How many different heights satellites use before the next one starts again at the first height (its circle is still wider).")]
    [Range(1, 10)] public int satelliteHeightLayers = 4;
    [Tooltip("Orbit speed (radians per second).")]
    [Range(0f, 3f)] public float satelliteSpeed = 0.7f;
    [Tooltip("Size of the satellites in orbit (live).")]
    [Range(0.2f, 3f)] public float satelliteScale = 1f;

    [Header("Drones")]
    [Tooltip("Drones fly this far above the tallest capital.")]
    [Range(0f, 5f)] public float droneHeightAboveCapital = 0.9f;

    // The hidden level for an amount of power: 1 up to the first step, then one more for every step of zeros.
    public int LevelFor(double power)
    {
        if (power < 1.0)
            return 1;
        int level = 1 + (int)System.Math.Floor(System.Math.Log10(power) / Mathf.Max(1, zerosPerLevel));
        return Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
    }

    // The least power that reaches a level.
    public double PowerForLevel(int level) => level <= 1 ? 1.0 : System.Math.Pow(10.0, (double)(level - 1) * Mathf.Max(1, zerosPerLevel));
}
