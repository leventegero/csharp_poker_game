namespace PokerGame;

/// <summary>
/// Personality types for bot players.
/// </summary>
public enum BotPersonality
{
    None,       // Human player
    Cautious,   // Tight, folds often
    Balanced,   // Reasonable play
    Aggressive  // Loose, raises often
}

/// <summary>
/// Represents a player at the poker table (human or bot).
/// </summary>
public class Player
{
    public string Name { get; }
    public int Credits { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public bool IsBot { get; }
    public BotPersonality Personality { get; }
    public bool HasFolded { get; set; }
    public int CurrentBet { get; set; }
    public bool IsAllIn { get; set; }
    public bool IsEliminated => Credits <= 0 && !IsAllIn;

    public Player(string name, int credits, bool isBot, BotPersonality personality = BotPersonality.None)
    {
        Name = name;
        Credits = credits;
        IsBot = isBot;
        Personality = personality;
    }

    /// <summary>
    /// Places a bet, deducting from credits. Returns the actual amount bet (may be less if all-in).
    /// </summary>
    public int PlaceBet(int amount)
    {
        int actualBet = Math.Min(amount, Credits);
        Credits -= actualBet;
        CurrentBet += actualBet;

        if (Credits == 0)
            IsAllIn = true;

        return actualBet;
    }

    /// <summary>
    /// Folds the hand.
    /// </summary>
    public void Fold()
    {
        HasFolded = true;
    }

    /// <summary>
    /// Resets the player's state for a new hand.
    /// </summary>
    public void ResetForNewHand()
    {
        HoleCards.Clear();
        HasFolded = false;
        CurrentBet = 0;
        IsAllIn = false;
    }

    /// <summary>
    /// Returns the display color for this player.
    /// </summary>
    public ConsoleColor DisplayColor => Personality switch
    {
        BotPersonality.None       => ConsoleColor.Cyan,
        BotPersonality.Cautious   => ConsoleColor.Green,
        BotPersonality.Balanced   => ConsoleColor.Yellow,
        BotPersonality.Aggressive => ConsoleColor.Magenta,
        _ => ConsoleColor.White
    };

    public override string ToString() => $"{Name} (${Credits})";
}
