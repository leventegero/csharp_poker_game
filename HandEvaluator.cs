namespace PokerGame;

/// <summary>
/// Evaluates poker hands from 7 cards (2 hole + 5 community) and determines the best 5-card hand.
/// </summary>
public static class HandEvaluator
{
    /// <summary>
    /// Evaluates the best possible 5-card hand from the given cards.
    /// </summary>
    public static HandRank Evaluate(List<Card> holeCards, List<Card> communityCards)
    {
        var allCards = new List<Card>(holeCards);
        allCards.AddRange(communityCards);

        HandRank? best = null;

        // Generate all combinations of 5 from available cards
        var combinations = GetCombinations(allCards, 5);

        foreach (var combo in combinations)
        {
            var rank = EvaluateFiveCards(combo);
            if (best is null || rank.CompareTo(best) > 0)
                best = rank;
        }

        return best ?? new HandRank(HandRankType.HighCard, new List<int>(), new List<Card>());
    }

    /// <summary>
    /// Evaluates exactly 5 cards and returns the hand rank.
    /// </summary>
    private static HandRank EvaluateFiveCards(List<Card> cards)
    {
        var sorted = cards.OrderByDescending(c => (int)c.Rank).ToList();
        var ranks = sorted.Select(c => (int)c.Rank).ToList();
        var suits = sorted.Select(c => c.Suit).ToList();

        bool isFlush = suits.Distinct().Count() == 1;
        bool isStraight = IsStraight(ranks, out int straightHigh);

        // Check for Ace-low straight (A-2-3-4-5)
        bool isLowStraight = false;
        if (!isStraight && ranks.Contains((int)Rank.Ace) &&
            ranks.Contains(2) && ranks.Contains(3) &&
            ranks.Contains(4) && ranks.Contains(5))
        {
            isStraight = true;
            isLowStraight = true;
            straightHigh = 5;
        }

        var groups = ranks.GroupBy(r => r)
                         .OrderByDescending(g => g.Count())
                         .ThenByDescending(g => g.Key)
                         .ToList();

        // Royal Flush
        if (isFlush && isStraight && !isLowStraight && ranks.Contains((int)Rank.Ace) && ranks.Contains((int)Rank.King))
            return new HandRank(HandRankType.RoyalFlush, new List<int> { (int)Rank.Ace }, sorted);

        // Straight Flush
        if (isFlush && isStraight)
            return new HandRank(HandRankType.StraightFlush, new List<int> { straightHigh }, sorted);

        // Four of a Kind
        if (groups[0].Count() == 4)
        {
            int quadRank = groups[0].Key;
            int kicker = groups[1].Key;
            return new HandRank(HandRankType.FourOfAKind, new List<int> { quadRank, kicker }, sorted);
        }

        // Full House
        if (groups[0].Count() == 3 && groups[1].Count() == 2)
        {
            return new HandRank(HandRankType.FullHouse, new List<int> { groups[0].Key, groups[1].Key }, sorted);
        }

        // Flush
        if (isFlush)
            return new HandRank(HandRankType.Flush, ranks, sorted);

        // Straight
        if (isStraight)
            return new HandRank(HandRankType.Straight, new List<int> { straightHigh }, sorted);

        // Three of a Kind
        if (groups[0].Count() == 3)
        {
            var kickers = groups.Skip(1).Select(g => g.Key).ToList();
            return new HandRank(HandRankType.ThreeOfAKind,
                new List<int> { groups[0].Key }.Concat(kickers).ToList(), sorted);
        }

        // Two Pair
        if (groups[0].Count() == 2 && groups[1].Count() == 2)
        {
            int highPair = Math.Max(groups[0].Key, groups[1].Key);
            int lowPair = Math.Min(groups[0].Key, groups[1].Key);
            int kicker = groups[2].Key;
            return new HandRank(HandRankType.TwoPair, new List<int> { highPair, lowPair, kicker }, sorted);
        }

        // One Pair
        if (groups[0].Count() == 2)
        {
            var kickers = groups.Skip(1).Select(g => g.Key).ToList();
            return new HandRank(HandRankType.OnePair,
                new List<int> { groups[0].Key }.Concat(kickers).ToList(), sorted);
        }

        // High Card
        return new HandRank(HandRankType.HighCard, ranks, sorted);
    }

    /// <summary>
    /// Checks if the given ranks form a straight. Returns the highest card of the straight.
    /// </summary>
    private static bool IsStraight(List<int> ranks, out int highCard)
    {
        var distinct = ranks.Distinct().OrderByDescending(r => r).ToList();
        highCard = distinct[0];

        if (distinct.Count < 5) return false;

        // Check if consecutive
        for (int i = 0; i < distinct.Count - 1; i++)
        {
            if (distinct[i] - distinct[i + 1] != 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Generates all combinations of a given size from the source list.
    /// </summary>
    private static List<List<Card>> GetCombinations(List<Card> source, int size)
    {
        var result = new List<List<Card>>();
        GenerateCombinations(source, size, 0, new List<Card>(), result);
        return result;
    }

    private static void GenerateCombinations(List<Card> source, int size, int start,
        List<Card> current, List<List<Card>> result)
    {
        if (current.Count == size)
        {
            result.Add(new List<Card>(current));
            return;
        }

        for (int i = start; i < source.Count; i++)
        {
            current.Add(source[i]);
            GenerateCombinations(source, size, i + 1, current, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    /// <summary>
    /// Returns a rough numeric strength score (0.0 to 1.0) for use by bot AI.
    /// </summary>
    public static double GetHandStrength(HandRank rank)
    {
        return rank.Type switch
        {
            HandRankType.RoyalFlush     => 1.0,
            HandRankType.StraightFlush  => 0.95,
            HandRankType.FourOfAKind    => 0.90,
            HandRankType.FullHouse      => 0.82,
            HandRankType.Flush          => 0.75,
            HandRankType.Straight       => 0.68,
            HandRankType.ThreeOfAKind   => 0.55,
            HandRankType.TwoPair        => 0.42,
            HandRankType.OnePair        => 0.28,
            HandRankType.HighCard       => 0.10 + ((double)rank.Kickers[0] / 14.0) * 0.12,
            _ => 0.0
        };
    }

    /// <summary>
    /// Evaluates the pre-flop strength of hole cards (0.0 to 1.0).
    /// </summary>
    public static double GetPreFlopStrength(List<Card> holeCards)
    {
        if (holeCards.Count < 2) return 0.0;

        var c1 = holeCards[0];
        var c2 = holeCards[1];

        int high = Math.Max((int)c1.Rank, (int)c2.Rank);
        int low = Math.Min((int)c1.Rank, (int)c2.Rank);
        bool suited = c1.Suit == c2.Suit;
        bool paired = c1.Rank == c2.Rank;

        double strength = 0.0;

        if (paired)
        {
            // Pocket pairs: AA=1.0, KK=0.95, ..., 22=0.5
            strength = 0.5 + ((high - 2) / 12.0) * 0.5;
        }
        else
        {
            // Non-paired: base on high card + gap
            strength = (high - 2) / 12.0 * 0.45;
            strength += (low - 2) / 12.0 * 0.15;

            // Suited bonus
            if (suited) strength += 0.06;

            // Connectedness bonus (cards close in rank)
            int gap = high - low;
            if (gap == 1) strength += 0.05;
            else if (gap == 2) strength += 0.03;
        }

        return Math.Clamp(strength, 0.0, 1.0);
    }
}
