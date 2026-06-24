namespace PokerGame;

/// <summary>
/// Main game controller. Manages the poker table, dealing, betting rounds,
/// showdown, and game flow.
/// </summary>
public class Game
{
    private readonly List<Player> _players;
    private readonly Deck _deck;
    private List<Card> _communityCards;
    private int _pot;
    private int _dealerIndex;
    private int _handNumber;
    private int _currentBet;
    private int _minRaise;
    private const int SmallBlind = 1;
    private const int BigBlind = 2;

    public Game()
    {
        _deck = new Deck();
        _communityCards = new List<Card>();
        _pot = 0;
        _dealerIndex = 0;
        _handNumber = 0;

        _players = new List<Player>
        {
            new Player("You", 100, isBot: false),
            new Player("Alex", 100, isBot: true, BotPersonality.Cautious),
            new Player("Sam", 100, isBot: true, BotPersonality.Balanced),
            new Player("Max", 100, isBot: true, BotPersonality.Aggressive),
        };
    }

    /// <summary>
    /// Runs the main game loop until only one player remains.
    /// </summary>
    public void Run()
    {
        Display.Banner();
        Display.WriteLineColor("  Welcome to the table!", ConsoleColor.White);
        Display.WriteLineColor("  You start with $100. Beat all 3 opponents to win!", ConsoleColor.DarkGray);
        Console.WriteLine();
        Display.WriteColor("  Opponents: ", ConsoleColor.White);
        Display.WriteColor("Alex", ConsoleColor.Green);
        Display.WriteColor(" (cautious)  ", ConsoleColor.DarkGray);
        Display.WriteColor("Sam", ConsoleColor.Yellow);
        Display.WriteColor(" (balanced)  ", ConsoleColor.DarkGray);
        Display.WriteColor("Max", ConsoleColor.Magenta);
        Display.WriteLineColor(" (aggressive)", ConsoleColor.DarkGray);

        if (Display.PressAnyKey())
            return;

        while (GetActivePlayers().Count > 1)
        {
            PlayHand();

            // Remove eliminated players
            var eliminated = _players.Where(p => p.Credits <= 0 && !p.IsAllIn).ToList();
            foreach (var p in eliminated)
            {
                Display.PlayerEliminated(p);
            }
            _players.RemoveAll(p => p.Credits <= 0);

            if (GetActivePlayers().Count <= 1)
                break;

            // Move dealer button
            _dealerIndex = (_dealerIndex) % _players.Count;

            if (Display.PressAnyKey())
                return;
        }

        // Game over
        var champion = _players.FirstOrDefault(p => p.Credits > 0) ?? _players[0];
        Display.GameOver(champion);

        if (!champion.IsBot)
            Display.WriteLineColor("  Congratulations, you won the game! 🎉", ConsoleColor.Cyan);
        else
            Display.WriteLineColor("  Better luck next time! 💪", ConsoleColor.DarkGray);

        Display.PressAnyKey("Press any key to exit...");
    }

    /// <summary>
    /// Plays a single hand of poker.
    /// </summary>
    private void PlayHand()
    {
        _handNumber++;
        _pot = 0;
        _currentBet = 0;
        _minRaise = BigBlind;
        _communityCards = new List<Card>();
        _deck.Reset();

        // Reset all players for new hand
        foreach (var p in _players)
            p.ResetForNewHand();

        Display.Banner();
        Display.HandHeader(_handNumber);

        // Post blinds
        PostBlinds();

        // Deal hole cards
        DealHoleCards();

        // Show initial state
        RefreshDisplay("PRE-FLOP");

        // ── Pre-flop betting ──
        if (!RunBettingRound(startAfterBigBlind: true))
        {
            AwardPot();
            return;
        }

        // ── Flop ──
        Display.PhaseLabel("FLOP");
        _communityCards.AddRange(_deck.Deal(3));
        ResetBetsForNewRound();
        RefreshDisplay("FLOP");

        if (!RunBettingRound())
        {
            AwardPot();
            return;
        }

        // ── Turn ──
        Display.PhaseLabel("TURN");
        _communityCards.Add(_deck.Deal());
        ResetBetsForNewRound();
        RefreshDisplay("TURN");

        if (!RunBettingRound())
        {
            AwardPot();
            return;
        }

        // ── River ──
        Display.PhaseLabel("RIVER");
        _communityCards.Add(_deck.Deal());
        ResetBetsForNewRound();
        RefreshDisplay("RIVER");

        if (!RunBettingRound())
        {
            AwardPot();
            return;
        }

        // ── Showdown ──
        Showdown();
    }

    /// <summary>
    /// Posts the small and big blinds.
    /// </summary>
    private void PostBlinds()
    {
        int sbIndex = (_dealerIndex + 1) % _players.Count;
        int bbIndex = (_dealerIndex + 2) % _players.Count;

        var sbPlayer = _players[sbIndex];
        var bbPlayer = _players[bbIndex];

        int sbActual = sbPlayer.PlaceBet(SmallBlind);
        int bbActual = bbPlayer.PlaceBet(BigBlind);
        _pot += sbActual + bbActual;
        _currentBet = BigBlind;

        Display.WriteColor($"  {sbPlayer.Name}", sbPlayer.DisplayColor);
        Display.WriteLineColor($" posts small blind (${sbActual})", ConsoleColor.DarkGray);
        Display.WriteColor($"  {bbPlayer.Name}", bbPlayer.DisplayColor);
        Display.WriteLineColor($" posts big blind (${bbActual})", ConsoleColor.DarkGray);
    }

    /// <summary>
    /// Deals 2 hole cards to each player.
    /// </summary>
    private void DealHoleCards()
    {
        foreach (var p in _players)
        {
            p.HoleCards = _deck.Deal(2);
        }
    }

    /// <summary>
    /// Runs a single betting round. Returns false if only one player remains (others folded).
    /// </summary>
    private bool RunBettingRound(bool startAfterBigBlind = false)
    {
        var activePlayers = GetPlayersInHand();
        if (activePlayers.Count <= 1) return false;

        // Determine starting position
        int startIndex;
        if (startAfterBigBlind)
        {
            // Pre-flop: start after big blind
            int bbIndex = (_dealerIndex + 2) % _players.Count;
            startIndex = (bbIndex + 1) % _players.Count;
        }
        else
        {
            // Post-flop: start after dealer
            startIndex = (_dealerIndex + 1) % _players.Count;
        }

        // Track who has acted and when everyone has matched the current bet
        var hasActed = new HashSet<string>();
        string? lastRaiser = null;
        int playerIndex = startIndex;

        while (true)
        {
            var player = _players[playerIndex];

            // Skip folded, all-in, or eliminated players
            if (player.HasFolded || player.IsAllIn || player.Credits <= 0)
            {
                playerIndex = (playerIndex + 1) % _players.Count;
                // Check if round is complete
                if (IsRoundComplete(hasActed, lastRaiser))
                    break;
                continue;
            }

            // Check if only one active player remains
            if (GetPlayersInHand().Count <= 1) return false;

            // If this player was the last raiser and everyone else has acted, round is done
            if (lastRaiser == player.Name && hasActed.Contains(player.Name))
                break;

            int toCall = _currentBet - player.CurrentBet;

            PlayerDecision decision;

            if (player.IsBot)
            {
                // Bot decision
                Thread.Sleep(600); // Thinking delay
                decision = BotAI.MakeDecision(player, _communityCards, _currentBet, _pot, _minRaise);
                Display.BotAction(player, decision);
            }
            else
            {
                // Human player decision
                decision = GetHumanDecision(player, toCall);
            }

            // Apply decision
            ApplyDecision(player, decision, toCall);
            hasActed.Add(player.Name);

            if (decision.Action == PlayerAction.Raise)
            {
                lastRaiser = player.Name;
                // Reset acted status for everyone except the raiser
                hasActed.Clear();
                hasActed.Add(player.Name);
            }

            // Check if only one player left
            if (GetPlayersInHand().Count <= 1) return false;

            playerIndex = (playerIndex + 1) % _players.Count;

            // Safety: if everyone has acted and matched, end round
            if (IsRoundComplete(hasActed, lastRaiser))
                break;
        }

        return GetPlayersInHand().Count > 1;
    }

    /// <summary>
    /// Checks if the betting round is complete (all active players have acted and matched).
    /// </summary>
    private bool IsRoundComplete(HashSet<string> hasActed, string? lastRaiser)
    {
        var active = GetPlayersInHand().Where(p => !p.IsAllIn).ToList();
        if (active.Count == 0) return true;

        // All active players must have acted
        foreach (var p in active)
        {
            if (!hasActed.Contains(p.Name))
                return false;
        }

        // All active players must have matched the current bet
        foreach (var p in active)
        {
            if (p.CurrentBet < _currentBet && !p.IsAllIn)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the human player's decision via console input.
    /// </summary>
    private PlayerDecision GetHumanDecision(Player player, int toCall)
    {
        while (true)
        {
            Display.ActionPrompt(toCall, _minRaise, player.Credits);
            string? input = Console.ReadLine()?.Trim().ToUpper();

            switch (input)
            {
                case "F":
                    return new PlayerDecision(PlayerAction.Fold);

                case "C":
                    if (toCall == 0)
                        return new PlayerDecision(PlayerAction.Check);
                    else
                        return new PlayerDecision(PlayerAction.Call);

                case "R":
                    return GetRaiseAmount(player, toCall);

                default:
                    Display.WriteLineColor("  Invalid input. Enter F, C, or R.", ConsoleColor.Red);
                    break;
            }
        }
    }

    /// <summary>
    /// Prompts the human player for a raise amount.
    /// </summary>
    private PlayerDecision GetRaiseAmount(Player player, int toCall)
    {
        int maxRaise = player.Credits - toCall;
        if (maxRaise <= 0)
        {
            Display.WriteLineColor("  Not enough credits to raise. Going all-in!", ConsoleColor.Yellow);
            return new PlayerDecision(PlayerAction.Raise, player.Credits);
        }

        while (true)
        {
            Console.Write($"  Raise amount (min ${_minRaise}, max ${maxRaise}, or 'all'): ");
            string? input = Console.ReadLine()?.Trim().ToLower();

            if (input == "all")
                return new PlayerDecision(PlayerAction.Raise, player.Credits);

            if (int.TryParse(input, out int amount))
            {
                if (amount < _minRaise)
                {
                    Display.WriteLineColor($"  Minimum raise is ${_minRaise}.", ConsoleColor.Red);
                    continue;
                }
                if (amount > maxRaise)
                {
                    Display.WriteLineColor($"  Maximum raise is ${maxRaise}.", ConsoleColor.Red);
                    continue;
                }
                return new PlayerDecision(PlayerAction.Raise, amount);
            }

            Display.WriteLineColor("  Enter a valid number or 'all'.", ConsoleColor.Red);
        }
    }

    /// <summary>
    /// Applies a player's decision to the game state.
    /// </summary>
    private void ApplyDecision(Player player, PlayerDecision decision, int toCall)
    {
        switch (decision.Action)
        {
            case PlayerAction.Fold:
                player.Fold();
                break;

            case PlayerAction.Check:
                // No action needed
                break;

            case PlayerAction.Call:
                int callAmount = player.PlaceBet(toCall);
                _pot += callAmount;
                break;

            case PlayerAction.Raise:
                // First call the current bet, then raise on top
                int totalBet = toCall + decision.RaiseAmount;
                int actualBet = player.PlaceBet(totalBet);
                _pot += actualBet;
                _currentBet = player.CurrentBet;
                _minRaise = decision.RaiseAmount;
                break;
        }
    }

    /// <summary>
    /// Resets per-round betting state for a new betting round.
    /// </summary>
    private void ResetBetsForNewRound()
    {
        foreach (var p in _players)
            p.CurrentBet = 0;
        _currentBet = 0;
        _minRaise = BigBlind;
    }

    /// <summary>
    /// Performs the showdown - evaluates and compares all remaining hands.
    /// </summary>
    private void Showdown()
    {
        var contenders = GetPlayersInHand();

        var results = contenders
            .Select(p => (player: p, rank: HandEvaluator.Evaluate(p.HoleCards, _communityCards)))
            .OrderByDescending(r => r.rank)
            .ToList();

        Display.Showdown(_communityCards, results);

        // Find winner(s)
        var bestRank = results[0].rank;
        var winners = results.Where(r => r.rank.CompareTo(bestRank) == 0).Select(r => r.player).ToList();

        if (winners.Count == 1)
        {
            var winner = winners[0];
            winner.Credits += _pot;
            Display.Winner(winner, _pot, bestRank);
        }
        else
        {
            // Split pot
            int share = _pot / winners.Count;
            int remainder = _pot % winners.Count;
            foreach (var w in winners)
            {
                w.Credits += share;
            }
            // Give remainder to first winner (closest to dealer)
            if (remainder > 0)
                winners[0].Credits += remainder;

            Display.SplitPot(winners, share);
        }

        _pot = 0;
    }

    /// <summary>
    /// Awards the pot when all but one player has folded.
    /// </summary>
    private void AwardPot()
    {
        var winner = GetPlayersInHand().FirstOrDefault();
        if (winner != null)
        {
            winner.Credits += _pot;
            Display.Winner(winner, _pot);
        }
        _pot = 0;
    }

    /// <summary>
    /// Returns players who haven't folded and are still in the hand.
    /// </summary>
    private List<Player> GetPlayersInHand()
    {
        return _players.Where(p => !p.HasFolded).ToList();
    }

    /// <summary>
    /// Returns all players with credits remaining.
    /// </summary>
    private List<Player> GetActivePlayers()
    {
        return _players.Where(p => p.Credits > 0).ToList();
    }

    /// <summary>
    /// Refreshes the full display with current game state.
    /// </summary>
    private void RefreshDisplay(string phase)
    {
        Display.CommunityCards(phase, _communityCards);
        Display.Pot(_pot);
        Display.PlayersInfo(_players, _dealerIndex);
    }
}
