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


    public async Task JoinGame()
    {
        if (!players.ContainsValue(Context.ConnectionId)) // Ensure same connection isn't rejoining
        {
            if (!players.ContainsKey("X"))
            {
                players["X"] = Context.ConnectionId;
                await Clients.Client(Context.ConnectionId).SendAsync("SetPlayer", "X");
            }
            else if (!players.ContainsKey("O"))
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


    public async Task MakeMove(int index)
    {
        if (board[index] == '\0' && players.ContainsKey(currentPlayer.ToString()))
        {
            string currentPlayerId = players[currentPlayer.ToString()];
            if (Context.ConnectionId != currentPlayerId)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("GameStatus", "Not your turn!");
                return;
            }

            board[index] = currentPlayer;
            await Clients.All.SendAsync("UpdateBoard", index, currentPlayer);

            if (CheckWin())
            {
                scores[currentPlayer]++; // Increment the winner's score
                await Clients.All.SendAsync("GameOver", currentPlayer);
                await UpdateScoreboard();
                ResetGame();
            }
            else if (CheckDraw())  // 🔥 Check if the board is full and no winner
            {
                await Clients.All.SendAsync("GameDraw");  // 🔥 Notify clients about a draw
                ResetGame();
            }
            else
            {
                currentPlayer = (currentPlayer == 'X') ? 'O' : 'X';
                await Clients.All.SendAsync("TurnChanged", currentPlayer);
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
