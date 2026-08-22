public sealed class ExplorationClearRewardResult
{
    public string EncounterName { get; }
    public int CharacterExperience { get; }
    public int Gold { get; }
    public int Screw { get; }
    public int IronPlate { get; }
    public int Wire { get; }
    public int PreviousCharacterLevel { get; }
    public int CurrentCharacterLevel { get; }

    public bool LeveledUp =>
        CurrentCharacterLevel > PreviousCharacterLevel;

    public ExplorationClearRewardResult(
        string encounterName,
        int characterExperience,
        int gold,
        int screw,
        int ironPlate,
        int wire,
        int previousCharacterLevel,
        int currentCharacterLevel)
    {
        EncounterName = encounterName;
        CharacterExperience = characterExperience;
        Gold = gold;
        Screw = screw;
        IronPlate = ironPlate;
        Wire = wire;
        PreviousCharacterLevel = previousCharacterLevel;
        CurrentCharacterLevel = currentCharacterLevel;
    }
}
