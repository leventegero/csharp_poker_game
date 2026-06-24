namespace PokerGame;

/// <summary>
/// Possible actions a player can take during a betting round.
/// </summary>
public enum PlayerAction
{
    Fold,
    Check,
    Call,
    Raise
}

/// <summary>
/// Represents a decision made by a player.
/// </summary>
public class PlayerDecision
{
    public PlayerAction Action { get; }
    public int RaiseAmount { get; }

    public PlayerDecision(PlayerAction action, int raiseAmount = 0)
    {
        Action = action;
        RaiseAmount = raiseAmount;
    }
}

/// <summary>
/// AI decision engine for bot players. Uses hand strength evaluation
/// combined with personality-based thresholds to decide fold/call/raise.
/// </summary>
public static class BotAI
{
    private static readonly Random _rng = new();

    /// <summary>
    /// Makes a decision for the given bot player based on the current game state.
    /// </summary>
    public static PlayerDecision MakeDecision(Player bot, List<Card> communityCards,
        int currentBet, int pot, int minRaise)
    {
        double handStrength = EvaluateStrength(bot, communityCards);
        int toCall = currentBet - bot.CurrentBet;

        // Add some randomness
        double randomFactor = (_rng.NextDouble() - 0.5) * 0.15;
        double adjustedStrength = handStrength + randomFactor;

        // Get personality thresholds
        var (foldThreshold, raiseThreshold, bluffChance, raiseMultiplier) = GetPersonalityParams(bot.Personality);

        // Bluff chance - occasionally act strong with a weak hand
        if (adjustedStrength < foldThreshold && _rng.NextDouble() < bluffChance)
        {
            adjustedStrength = raiseThreshold + 0.1; // Pretend we have a strong hand
        }

        // Can we check (no cost to stay in)?
        bool canCheck = toCall == 0;

        // Decision logic
        if (adjustedStrength < foldThreshold)
        {
            // Weak hand
            if (canCheck)
                return new PlayerDecision(PlayerAction.Check);
            else
                return new PlayerDecision(PlayerAction.Fold);
        }
        else if (adjustedStrength >= raiseThreshold)
        {
            // Strong hand - raise
            int raiseAmount = CalculateRaiseAmount(bot, pot, minRaise, adjustedStrength, raiseMultiplier);
            if (raiseAmount > 0 && bot.Credits > toCall)
                return new PlayerDecision(PlayerAction.Raise, raiseAmount);
            else if (canCheck)
                return new PlayerDecision(PlayerAction.Check);
            else
                return new PlayerDecision(PlayerAction.Call);
        }
        else
        {
            // Medium hand - call or check
            if (canCheck)
            {
                // Sometimes raise with medium hands (semi-bluff)
                if (_rng.NextDouble() < 0.15 * (double)GetPersonalityParams(bot.Personality).raiseMultiplier)
                {
                    int raiseAmount = CalculateRaiseAmount(bot, pot, minRaise, adjustedStrength, 0.5);
                    if (raiseAmount > 0)
                        return new PlayerDecision(PlayerAction.Raise, raiseAmount);
                }
                return new PlayerDecision(PlayerAction.Check);
            }

            // Check pot odds - is the call worth it?
            double potOdds = (double)toCall / (pot + toCall);
            if (adjustedStrength > potOdds + 0.1)
                return new PlayerDecision(PlayerAction.Call);
            else if (canCheck)
                return new PlayerDecision(PlayerAction.Check);
            else
                return new PlayerDecision(PlayerAction.Fold);
        }
    }

    /// <summary>
    /// Returns the hand strength score (0.0 to 1.0) based on current game state.
    /// </summary>
    private static double EvaluateStrength(Player bot, List<Card> communityCards)
    {
        if (communityCards.Count == 0)
        {
            // Pre-flop: evaluate hole cards
            return HandEvaluator.GetPreFlopStrength(bot.HoleCards);
        }
        else
        {
            // Post-flop: evaluate full hand
            var rank = HandEvaluator.Evaluate(bot.HoleCards, communityCards);
            return HandEvaluator.GetHandStrength(rank);
        }
    }

    /// <summary>
    /// Returns personality-specific thresholds and parameters.
    /// </summary>
    private static (double foldThreshold, double raiseThreshold, double bluffChance, double raiseMultiplier)
        GetPersonalityParams(BotPersonality personality)
    {
        return personality switch
        {
            BotPersonality.Cautious   => (0.35, 0.65, 0.05, 0.6),
            BotPersonality.Balanced   => (0.25, 0.55, 0.12, 1.0),
            BotPersonality.Aggressive => (0.15, 0.40, 0.25, 1.5),
            _ => (0.25, 0.55, 0.10, 1.0)
        };
    }

    /// <summary>
    /// Calculates a raise amount based on hand strength and personality.
    /// </summary>
    private static int CalculateRaiseAmount(Player bot, int pot, int minRaise,
        double strength, double multiplier)
    {
        // Base raise is proportional to pot and hand strength
        int baseRaise = (int)(pot * strength * multiplier * 0.5);
        baseRaise = Math.Max(baseRaise, minRaise);

        // Don't raise more than we have
        int maxRaise = bot.Credits;
        baseRaise = Math.Min(baseRaise, maxRaise);

        // Add some variance
        int variance = (int)(baseRaise * 0.3 * (_rng.NextDouble() - 0.5));
        baseRaise = Math.Max(minRaise, baseRaise + variance);
        baseRaise = Math.Min(baseRaise, maxRaise);

        return baseRaise;
    }
}
