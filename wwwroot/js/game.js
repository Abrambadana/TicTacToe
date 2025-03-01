const connection = new signalR.HubConnectionBuilder()
    .withUrl("/tictactoehub")
    .build();

let playerSymbol = "";
let gameActive = false;

connection.on("SetPlayer", (symbol) => {
    playerSymbol = symbol;
    document.getElementById("gameStatus").innerText = `You are Player ${playerSymbol}`;
});

connection.on("StartGame", () => {
    document.getElementById("gameStatus").innerText = "Game started! Your turn.";
    gameActive = true;
});

connection.on("GameFull", () => {
    document.getElementById("gameStatus").innerText = "Game is full. Try again later.";
});

connection.on("UpdateBoard", (index, symbol) => {
    document.getElementById(`cell-${index}`).innerText = symbol;
});

connection.on("GameOver", (winner, scoreX, scoreO) => {
    document.getElementById("gameStatus").innerText = `Player ${winner} wins!`;
    document.getElementById("scoreX").innerText = scoreX;
    document.getElementById("scoreO").innerText = scoreO;
    gameActive = false;
});

connection.on("GameDraw", () => {
    document.getElementById("gameStatus").innerText = "It's a draw!";
    gameActive = false;
});

async function makeMove(index) {
    if (gameActive && document.getElementById(`cell-${index}`).innerText === "") {
        await connection.invoke("MakeMove", index);
    }
}

document.getElementById("restartBtn").addEventListener("click", async () => {
    await connection.invoke("RestartGame");
});

connection.on("RestartGame", () => {
    gameActive = true;
    document.getElementById("gameStatus").innerText = "Game restarted! Player X starts.";

    for (let i = 0; i < 9; i++) {
        document.getElementById(`cell-${i}`).innerText = "";
    }
});

connection.start().then(() => {
    connection.invoke("JoinGame");
});
connection.on("TurnChanged", (symbol) => {
    document.getElementById("gameStatus").innerText = `Player ${symbol}'s turn`;
});

connection.on("UpdateScoreboard", (scoreX, scoreO) => {
    document.getElementById("scoreX").innerText = scoreX;
    document.getElementById("scoreO").innerText = scoreO;
});
