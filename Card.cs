namespace PokerGame;

/// <summary>
/// Represents the four suits of a standard deck.
/// </summary>
public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

/// <summary>
/// Represents card ranks from Two (lowest) to Ace (highest).
/// </summary>
public enum Rank
{
    Two = 2,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace
}

/// <summary>
/// Represents a single playing card with a suit and rank.
/// </summary>
public class Card : IComparable<Card>
{
    public Suit Suit { get; }
    public Rank Rank { get; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    /// <summary>
    /// Returns the Unicode symbol for this card's suit.
    /// </summary>
    public string SuitSymbol => Suit switch
    {
        Suit.Hearts   => "♥",
        Suit.Diamonds => "♦",
        Suit.Clubs    => "♣",
        Suit.Spades   => "♠",
        _ => "?"
    };

    /// <summary>
    /// Returns the short rank label (e.g. "A", "K", "10", "2").
    /// </summary>
    public string RankLabel => Rank switch
    {
        Rank.Ace   => "A",
        Rank.King  => "K",
        Rank.Queen => "Q",
        Rank.Jack  => "J",
        Rank.Ten   => "10",
        _ => ((int)Rank).ToString()
    };

    /// <summary>
    /// Returns the console color used for this card's suit.
    /// </summary>
    public ConsoleColor SuitColor => Suit switch
    {
        Suit.Hearts   => ConsoleColor.Red,
        Suit.Diamonds => ConsoleColor.Red,
        Suit.Clubs    => ConsoleColor.White,
        Suit.Spades   => ConsoleColor.White,
        _ => ConsoleColor.Gray
    };

    public int CompareTo(Card? other)
    {
        if (other is null) return 1;
        return Rank.CompareTo(other.Rank);
    }

    public override string ToString() => $"{RankLabel}{SuitSymbol}";

    public override bool Equals(object? obj) =>
        obj is Card other && Suit == other.Suit && Rank == other.Rank;

    public override int GetHashCode() => HashCode.Combine(Suit, Rank);
}
