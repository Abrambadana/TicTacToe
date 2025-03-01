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
                    await Clients.All.SendAsync("StartGame");
                }
            }
            else if (!players.ContainsKey("O") && !singlePlayer)
            {
                players["O"] = Context.ConnectionId;
                await Clients.Client(Context.ConnectionId).SendAsync("SetPlayer", "O");
                await Clients.All.SendAsync("StartGame");
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("GameFull");
            }
        }
    }

    private async Task UpdateScoreboard()
    {
        await Clients.All.SendAsync("UpdateScoreboard", scores['X'], scores['O']);
    }

    private async Task AIMakeMove()
    {
        List<int> availableMoves = new List<int>();

        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == '\0')
                availableMoves.Add(i);
        }

        if (availableMoves.Count > 0)
        {
            Random rnd = new Random();
            int aiMove = availableMoves[rnd.Next(availableMoves.Count)]; // Choose a random move
            await MakeMove(aiMove);
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
                await Clients.All.SendAsync("GameOver", currentPlayer);
                await UpdateScoreboard();
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


