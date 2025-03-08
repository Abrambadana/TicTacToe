using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TicTacToeHub : Hub
{
    private static Dictionary<string, string> players = new Dictionary<string, string>();
    private static char[] board = new char[9];
    private static char currentPlayer = 'X';
    private static Dictionary<char, int> scores = new Dictionary<char, int>
    {
        { 'X', 0 },
        { 'O', 0 }
    };

    public async Task JoinGame(bool singlePlayer = false)
    {
        if (!players.ContainsValue(Context.ConnectionId)) // Ensure same connection isn't rejoining
        {
            if (!players.ContainsKey("X"))
            {
                players["X"] = Context.ConnectionId;
                await Clients.Client(Context.ConnectionId).SendAsync("SetPlayer", "X");

                if (singlePlayer)
                {
                    players["O"] = "AI"; // Assign AI as Player O
                    await Clients.Client(Context.ConnectionId).SendAsync("SetSinglePlayerMode");
                    await Clients.Client(Context.ConnectionId).SendAsync("StartGame", "Your turn (Player X).");
                }
            }
            else if (!players.ContainsKey("O") && !singlePlayer)
            {
                players["O"] = Context.ConnectionId;
                await Clients.Client(Context.ConnectionId).SendAsync("SetPlayer", "O");

                // Notify Player X
                await Clients.Client(players["X"]).SendAsync("StartGame", "Your turn (Player X).");

                // Notify Player O
                await Clients.Client(players["O"]).SendAsync("StartGame", "Game started! Player X's turn.");
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("GameFull");
            }
        }
    }
    public async Task MakeMove(int index)
    {
        if (board[index] == '\0' && players.ContainsKey(currentPlayer.ToString()))
        {
            string currentPlayerId = players[currentPlayer.ToString()];
            if (Context.ConnectionId != currentPlayerId && players[currentPlayer.ToString()] != "AI")
            {
                await Clients.Client(Context.ConnectionId).SendAsync("GameStatus", "Not your turn!");
                return;
            }

            board[index] = currentPlayer;
            await Clients.All.SendAsync("UpdateBoard", index, currentPlayer);

            if (CheckWin())
            {
                scores[currentPlayer]++;
                await Clients.All.SendAsync("GameOver", currentPlayer, scores['X'], scores['O']);
                ResetGame();
                return;
            }
            else if (CheckDraw())
            {
                await Clients.All.SendAsync("GameDraw");
                ResetGame();
                return;
            }

            // Switch turn
            currentPlayer = (currentPlayer == 'X') ? 'O' : 'X';
            await Clients.All.SendAsync("TurnChanged", currentPlayer);

            // If it's AI's turn, make an AI move
            if (players[currentPlayer.ToString()] == "AI")
            {
                await Task.Delay(500); // Add slight delay for realism
                await AIMakeMove();
            }
        }
    }
    
    private async Task AIMakeMove()
    {
        int bestMove = -1;
        int bestScore = int.MinValue;

        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == '\0') // Check empty spots
            {
                board[i] = 'O'; // Try the move
                int moveScore = Minimax(board, 0, false);
                board[i] = '\0'; // Undo the move

                if (moveScore > bestScore)
                {
                    bestScore = moveScore;
                    bestMove = i;
                }
            }
        }

        if (bestMove != -1)
        {
            await MakeMove(bestMove);
        }
    }

    // Minimax Algorithm for AI decision-making
    private int Minimax(char[] boardState, int depth, bool isMaximizing)
    {
        if (CheckWinFor('O')) return 10 - depth; // AI wins
        if (CheckWinFor('X')) return depth - 10; // Player wins
        if (CheckDraw()) return 0; // Draw

        if (isMaximizing)
        {
            int bestScore = int.MinValue;
            for (int i = 0; i < boardState.Length; i++)
            {
                if (boardState[i] == '\0')
                {
                    boardState[i] = 'O';
                    int score = Minimax(boardState, depth + 1, false);
                    boardState[i] = '\0'; // Undo move
                    bestScore = Math.Max(score, bestScore);
                }
            }
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            for (int i = 0; i < boardState.Length; i++)
            {
                if (boardState[i] == '\0')
                {
                    boardState[i] = 'X';
                    int score = Minimax(boardState, depth + 1, true);
                    boardState[i] = '\0'; // Undo move
                    bestScore = Math.Min(score, bestScore);
                }
            }
            return bestScore;
        }
    }

    // Helper method to check for a win (used in Minimax)
    private bool CheckWinFor(char player)
    {
        int[][] winningPatterns = new int[][]
        {
        new[] {0, 1, 2}, new[] {3, 4, 5}, new[] {6, 7, 8},
        new[] {0, 3, 6}, new[] {1, 4, 7}, new[] {2, 5, 8},
        new[] {0, 4, 8}, new[] {2, 4, 6}
        };

        foreach (var pattern in winningPatterns)
        {
            if (board[pattern[0]] == player && board[pattern[1]] == player && board[pattern[2]] == player)
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckWin()
    {
        int[][] winningPatterns = new int[][]
        {
            new[] {0, 1, 2}, new[] {3, 4, 5}, new[] {6, 7, 8},
            new[] {0, 3, 6}, new[] {1, 4, 7}, new[] {2, 5, 8},
            new[] {0, 4, 8}, new[] {2, 4, 6}
        };

        foreach (var pattern in winningPatterns)
        {
            if (board[pattern[0]] != '\0' && board[pattern[0]] == board[pattern[1]] && board[pattern[1]] == board[pattern[2]])
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckDraw()
    {
        foreach (var cell in board)
        {
            if (cell == '\0') return false;
        }
        return true;
    }

    private void ResetGame()
    {
        board = new char[9];
        currentPlayer = 'X';
    }

    public async Task RestartGame()
    {
        ResetGame();
        await Clients.All.SendAsync("RestartGame");
    }
}