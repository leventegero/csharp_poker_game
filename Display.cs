namespace PokerGame;

/// <summary>
/// Console rendering utilities for the poker game.
/// Handles colorful output, card display, and table layout.
/// </summary>
public static class Display
{
    private const int TableWidth = 60;

    /// <summary>
    /// Writes text in a specific color, then resets.
    /// </summary>
    public static void WriteColor(string text, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = prev;
    }

    /// <summary>
    /// Writes a line of text in a specific color, then resets.
    /// </summary>
    public static void WriteLineColor(string text, ConsoleColor color)
    {
        WriteColor(text + "\n", color);
    }

    /// <summary>
    /// Writes a card with its suit color.
    /// </summary>
    public static void WriteCard(Card card)
    {
        WriteColor("[", ConsoleColor.DarkGray);
        WriteColor(card.RankLabel, ConsoleColor.White);
        WriteColor(card.SuitSymbol, card.SuitColor);
        WriteColor("]", ConsoleColor.DarkGray);
    }

    /// <summary>
    /// Writes a face-down card placeholder.
    /// </summary>
    public static void WriteHiddenCard()
    {
        WriteColor("[", ConsoleColor.DarkGray);
        WriteColor("??", ConsoleColor.DarkGray);
        WriteColor("]", ConsoleColor.DarkGray);
    }

    /// <summary>
    /// Displays a horizontal divider line.
    /// </summary>
    public static void Divider(char ch = '═')
    {
        WriteLineColor(new string(ch, TableWidth), ConsoleColor.DarkGray);
    }

    /// <summary>
    /// Displays the game title banner.
    /// </summary>
    public static void Banner()
    {
        Divider('╔');
        WriteLineColor("  ♠ ♥ ♦ ♣   T E X A S   H O L D ' E M   ♣ ♦ ♥ ♠", ConsoleColor.Yellow);
        Divider('╚');
        Console.WriteLine();
    }

    /// <summary>
    /// Displays the current hand number header.
    /// </summary>
    public static void HandHeader(int handNumber)
    {
        Console.WriteLine();
        WriteColor("  ── ", ConsoleColor.DarkGray);
        WriteColor($"HAND #{handNumber}", ConsoleColor.White);
        WriteLineColor(" ──", ConsoleColor.DarkGray);
        Console.WriteLine();
    }

    /// <summary>
    /// Displays the community cards with a phase label.
    /// </summary>
    public static void CommunityCards(string phase, List<Card> cards)
    {
        Console.WriteLine();
        WriteColor($"  {phase,-10} ", ConsoleColor.DarkCyan);

        if (cards.Count == 0)
        {
            WriteLineColor("  (no cards yet)", ConsoleColor.DarkGray);
        }
        else
        {
            foreach (var card in cards)
            {
                WriteCard(card);
                Console.Write(" ");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Displays the pot amount.
    /// </summary>
    public static void Pot(int amount)
    {
        Console.Write("  ");
        WriteColor("POT: ", ConsoleColor.DarkYellow);
        WriteLineColor($"${amount}", ConsoleColor.Yellow);
    }

    /// <summary>
    /// Displays all players and their status.
    /// </summary>
    public static void PlayersInfo(List<Player> players, int dealerIndex)
    {
        Console.WriteLine();
        WriteLineColor("  PLAYERS", ConsoleColor.DarkGray);
        Divider('─');

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            string dealerMark = i == dealerIndex ? " [D]" : "    ";
            string status = p.HasFolded ? " FOLDED" : p.IsAllIn ? " ALL-IN" : "";

            Console.Write("  ");

            // Dealer marker
            WriteColor(dealerMark, ConsoleColor.Yellow);
            Console.Write(" ");

            // Player name
            WriteColor($"{p.Name,-10}", p.DisplayColor);

            // Credits
            WriteColor($"  ${p.Credits,-6}", ConsoleColor.White);

            // Current bet
            if (p.CurrentBet > 0)
                WriteColor($"  Bet: ${p.CurrentBet}", ConsoleColor.DarkYellow);

            // Status
            if (p.HasFolded)
                WriteColor(status, ConsoleColor.DarkRed);
            else if (p.IsAllIn)
                WriteColor(status, ConsoleColor.Magenta);

            // Show hole cards for human player (or all at showdown)
            if (!p.IsBot && !p.HasFolded && p.HoleCards.Count > 0)
            {
                Console.Write("  ");
                foreach (var card in p.HoleCards)
                {
                    WriteCard(card);
                    Console.Write(" ");
                }
            }

            Console.WriteLine();
        }

        Divider('─');
    }

    /// <summary>
    /// Displays the player's action prompt and available choices.
    /// </summary>
    public static void ActionPrompt(int toCall, int minRaise, int credits)
    {
        Console.WriteLine();
        WriteLineColor("  YOUR TURN", ConsoleColor.Cyan);

        if (toCall == 0)
        {
            WriteColor("  [C]heck", ConsoleColor.Green);
            Console.Write("  ");
            WriteColor("[R]aise", ConsoleColor.Yellow);
            Console.Write("  ");
            WriteLineColor("[F]old", ConsoleColor.Red);
        }
        else
        {
            WriteColor($"  [C]all (${toCall})", ConsoleColor.Green);
            Console.Write("  ");
            WriteColor($"[R]aise (min ${minRaise})", ConsoleColor.Yellow);
            Console.Write("  ");
            WriteLineColor("[F]old", ConsoleColor.Red);
        }

        Console.Write("  > ");
    }

    /// <summary>
    /// Displays a bot's action.
    /// </summary>
    public static void BotAction(Player bot, PlayerDecision decision)
    {
        Console.Write("  ");
        WriteColor($"{bot.Name,-10}", bot.DisplayColor);
        WriteColor(" → ", ConsoleColor.DarkGray);

        switch (decision.Action)
        {
            case PlayerAction.Fold:
                WriteLineColor("folds", ConsoleColor.DarkRed);
                break;
            case PlayerAction.Check:
                WriteLineColor("checks", ConsoleColor.DarkGreen);
                break;
            case PlayerAction.Call:
                WriteLineColor("calls", ConsoleColor.Green);
                break;
            case PlayerAction.Raise:
                WriteLineColor($"raises ${decision.RaiseAmount}", ConsoleColor.Yellow);
                break;
        }
    }

    /// <summary>
    /// Displays showdown results for all active players.
    /// </summary>
    public static void Showdown(List<(Player player, HandRank rank)> results)
    {
        Console.WriteLine();
        WriteLineColor("  ═══ SHOWDOWN ═══", ConsoleColor.Yellow);
        Console.WriteLine();

        foreach (var (player, rank) in results)
        {
            Console.Write("  ");
            WriteColor($"{player.Name,-10}", player.DisplayColor);
            Console.Write("  ");

            // Show cards
            foreach (var card in rank.BestCards)
            {
                WriteCard(card);
                Console.Write(" ");
            }

            Console.Write("  ");
            WriteLineColor($"── {rank.Description}", ConsoleColor.White);
        }
    }

    /// <summary>
    /// Displays the winner of the hand.
    /// </summary>
    public static void Winner(Player winner, int potAmount, HandRank? rank = null)
    {
        Console.WriteLine();
        WriteColor("  🏆 ", ConsoleColor.Yellow);
        WriteColor(winner.Name, winner.DisplayColor);
        WriteColor($" won ${potAmount}", ConsoleColor.Yellow);
        if (rank != null)
            WriteColor($" with {rank.Description}", ConsoleColor.White);
        WriteLineColor("!", ConsoleColor.Yellow);
        Console.WriteLine();
    }

    /// <summary>
    /// Displays a split pot result.
    /// </summary>
    public static void SplitPot(List<Player> winners, int shareEach)
    {
        Console.WriteLine();
        WriteColor("  🤝 Split pot! ", ConsoleColor.Yellow);
        var names = string.Join(", ", winners.Select(w => w.Name));
        WriteColor(names, ConsoleColor.Cyan);
        WriteLineColor($" each receive ${shareEach}", ConsoleColor.Yellow);
        Console.WriteLine();
    }

    /// <summary>
    /// Displays a player elimination message.
    /// </summary>
    public static void PlayerEliminated(Player player)
    {
        WriteColor("  ✘ ", ConsoleColor.Red);
        WriteColor(player.Name, player.DisplayColor);
        WriteLineColor(" has been eliminated!", ConsoleColor.Red);
    }

    /// <summary>
    /// Displays the game-over screen.
    /// </summary>
    public static void GameOver(Player winner)
    {
        Console.WriteLine();
        Divider('═');
        WriteLineColor("  ★ ★ ★  G A M E   O V E R  ★ ★ ★", ConsoleColor.Yellow);
        Divider('═');
        Console.WriteLine();
        WriteColor("  Champion: ", ConsoleColor.White);
        WriteColor(winner.Name, winner.DisplayColor);
        WriteLineColor($"  (${winner.Credits})", ConsoleColor.Yellow);
        Console.WriteLine();
    }

    /// <summary>
    /// Pauses and waits for the user to press a key.
    /// </summary>
    public static void PressAnyKey(string message = "Press any key to continue...")
    {
        Console.WriteLine();
        WriteColor($"  {message}", ConsoleColor.DarkGray);
        Console.ReadKey(true);
        Console.WriteLine();
    }

    /// <summary>
    /// Displays the phase label (FLOP, TURN, RIVER).
    /// </summary>
    public static void PhaseLabel(string phase)
    {
        Console.WriteLine();
        WriteColor("  ── ", ConsoleColor.DarkCyan);
        WriteColor(phase, ConsoleColor.Cyan);
        WriteLineColor(" ──", ConsoleColor.DarkCyan);
    }
}
