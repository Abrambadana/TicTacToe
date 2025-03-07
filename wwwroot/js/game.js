const connection = new signalR.HubConnectionBuilder()
    .withUrl("/tictactoehub")
    .build();

let playerSymbol = "";
let gameActive = false;

// Event listeners for mode selection
document.getElementById("singlePlayerBtn").addEventListener("click", async () => {
    await switchToSinglePlayer();
});

document.getElementById("multiPlayerBtn").addEventListener("click", async () => {
    await switchToMultiplayer();
});

// Function to switch to single-player mode
async function switchToSinglePlayer() {
    await connection.invoke("ResetGame");
    await connection.invoke("JoinGame", true); // Join as single-player
}

// Function to switch to multiplayer mode
async function switchToMultiplayer() {
    await connection.invoke("ResetGame");
    await connection.invoke("JoinGame", false); // Join as multiplayer
}

// SignalR event handlers
connection.on("SetSinglePlayerMode", () => {
    document.getElementById("gameStatus").innerText = "You are Player X. AI is Player O.";
});

connection.on("SetPlayer", (symbol) => {
    playerSymbol = symbol;
    document.getElementById("gameStatus").innerText = `You are Player ${playerSymbol}`;
});


connection.on("StartGame", (message) => {
    document.getElementById("gameStatus").innerText = message;
    gameActive = true;
});

connection.on("GameFull", () => {
    document.getElementById("gameStatus").innerText = "Game is full. Try again later.";
});

connection.on("UpdateBoard", (index, symbol) => {
    const cell = document.getElementById(`cell-${index}`);
    cell.innerText = symbol;
    animateCell(cell);
});

connection.on("GameOver", (winner, scoreX, scoreO) => {
    document.getElementById("gameStatus").innerText = `Player ${winner} wins!`;
    document.getElementById("scoreX").innerText = scoreX;
    document.getElementById("scoreO").innerText = scoreO;
    gameActive = false;

    // Highlight winning cells
    highlightWinningCells(winner);
});

connection.on("GameDraw", () => {
    document.getElementById("gameStatus").innerText = "It's a draw!";
    gameActive = false;
});

connection.on("RestartGame", () => {
    gameActive = true;
    document.getElementById("gameStatus").innerText = "Game restarted! Player X starts.";

    // Clear the board
    for (let i = 0; i < 9; i++) {
        const cell = document.getElementById(`cell-${i}`);
        cell.innerText = "";
        cell.classList.remove("winning-cell");
    }
});

connection.on("TurnChanged", (symbol) => {
    if (symbol === playerSymbol) {
        document.getElementById("gameStatus").innerText = `Your turn (Player ${symbol})!`;
    } else {
        document.getElementById("gameStatus").innerText = `Waiting for Player ${symbol} to move.`;
    }
});

// Function to handle cell clicks
async function makeMove(index) {
    if (gameActive && document.getElementById(`cell-${index}`).innerText === "") {
        await connection.invoke("MakeMove", index);
    }
}

// Function to animate cell clicks
function animateCell(cell) {
    cell.style.transform = "scale(1.1)";
    setTimeout(() => {
        cell.style.transform = "scale(1)";
    }, 200);
}

// Function to highlight winning cells
function highlightWinningCells(winner) {
    const winningPatterns = [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // Rows
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // Columns
        [0, 4, 8], [2, 4, 6]             // Diagonals
    ];

    for (const pattern of winningPatterns) {
        const [a, b, c] = pattern;
        if (
            document.getElementById(`cell-${a}`).innerText === winner &&
            document.getElementById(`cell-${b}`).innerText === winner &&
            document.getElementById(`cell-${c}`).innerText === winner
        ) {
            document.getElementById(`cell-${a}`).classList.add("winning-cell");
            document.getElementById(`cell-${b}`).classList.add("winning-cell");
            document.getElementById(`cell-${c}`).classList.add("winning-cell");
        }
    }
}

// Start SignalR connection
connection.start().then(() => {
    connection.invoke("JoinGame");
});