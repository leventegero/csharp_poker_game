namespace PokerGame;

/// <summary>
/// Represents a standard 52-card deck with shuffle and deal operations.
/// </summary>
public class Deck
{
    private readonly List<Card> _cards = new();
    private int _dealIndex;
    private readonly Random _rng = new();

    public Deck()
    {
        Reset();
    }

    /// <summary>
    /// Rebuilds the deck with all 52 cards and shuffles it.
    /// </summary>
    public void Reset()
    {
        _cards.Clear();
        _dealIndex = 0;

        foreach (Suit suit in Enum.GetValues<Suit>())
        {
            foreach (Rank rank in Enum.GetValues<Rank>())
            {
                _cards.Add(new Card(suit, rank));
            }
        }

        Shuffle();
    }

    /// <summary>
    /// Fisher-Yates shuffle.
    /// </summary>
    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    /// <summary>
    /// Deals the next card from the deck.
    /// </summary>
    public Card Deal()
    {
        if (_dealIndex >= _cards.Count)
            throw new InvalidOperationException("No more cards in the deck.");

        return _cards[_dealIndex++];
    }

    /// <summary>
    /// Deals multiple cards at once.
    /// </summary>
    public List<Card> Deal(int count)
    {
        var cards = new List<Card>(count);
        for (int i = 0; i < count; i++)
            cards.Add(Deal());
        return cards;
    }

    public int CardsRemaining => _cards.Count - _dealIndex;
}
