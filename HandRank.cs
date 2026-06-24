namespace PokerGame;

/// <summary>
/// The hierarchy of poker hand types, ordered from weakest to strongest.
/// </summary>
public enum HandRankType
{
    HighCard,
    OnePair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
    RoyalFlush
}

/// <summary>
/// Represents the evaluated rank of a poker hand, including kickers for tie-breaking.
/// </summary>
public class HandRank : IComparable<HandRank>
{
    public HandRankType Type { get; }
    public List<int> Kickers { get; }
    public List<Card> BestCards { get; }

    public HandRank(HandRankType type, List<int> kickers, List<Card> bestCards)
    {
        Type = type;
        Kickers = kickers;
        BestCards = bestCards;
    }

    public int CompareTo(HandRank? other)
    {
        if (other is null) return 1;

        // Compare hand type first
        int typeCompare = Type.CompareTo(other.Type);
        if (typeCompare != 0) return typeCompare;

        // Compare kickers for tie-breaking
        for (int i = 0; i < Math.Min(Kickers.Count, other.Kickers.Count); i++)
        {
            int kickerCompare = Kickers[i].CompareTo(other.Kickers[i]);
            if (kickerCompare != 0) return kickerCompare;
        }

        return 0;
    }

    /// <summary>
    /// Returns a human-readable description of this hand.
    /// </summary>
    public string Description => Type switch
    {
        HandRankType.RoyalFlush     => "Royal Flush",
        HandRankType.StraightFlush  => "Straight Flush",
        HandRankType.FourOfAKind    => "Four of a Kind",
        HandRankType.FullHouse      => "Full House",
        HandRankType.Flush          => "Flush",
        HandRankType.Straight       => "Straight",
        HandRankType.ThreeOfAKind   => "Three of a Kind",
        HandRankType.TwoPair        => "Two Pair",
        HandRankType.OnePair        => "One Pair",
        HandRankType.HighCard       => "High Card",
        _ => "Unknown"
    };

    public override string ToString() => Description;
}
