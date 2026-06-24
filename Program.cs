namespace PokerGame;

/// <summary>
/// Entry point for the Poker Game application.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Set console encoding to support Unicode symbols (♠ ♥ ♦ ♣)
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Set console colors for a dark theme
        try
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
        }
        catch
        {
            // Ignore if console handles are not available (e.g. redirected output)
        }

        // Set a nice window title
        try
        {
            Console.Title = "♠ Texas Hold'em Poker ♥";
        }
        catch
        {
            // Title setting may fail on some terminals - ignore
        }

        var game = new Game();
        game.Run();
    }
}
