using System.Threading;
using System.Threading.Tasks;

namespace DeathRoll.Data;

public enum PlayerSymbol : byte
{
    None = 0,

    X = 1,
    O = 2
}

public enum WinType : byte
{
    None = 0,

    Tie = 1,
    Row = 2,
    Col = 3,
    Diag = 4,
    Anti = 5
}

public enum Difficulty : byte
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

public class Player
{
    public string Playername;
    public PlayerSymbol Symbol;
    public bool IsAI;
}

public class Winner
{
    public Player Player;
    public WinType Type;
    public int Row;
    public int Col;
}

public record Square(int Row, int Col);

public class GameField : ICloneable
{
    public readonly PlayerSymbol[,] Field;
    public readonly int FieldSize;

    private readonly int[] Range;

    public GameField(int fieldSize)
    {
        Field = new PlayerSymbol[fieldSize, fieldSize];

        FieldSize = fieldSize;
        Range = Enumerable.Range(0, fieldSize).ToArray();
    }

    private bool MarkedAllSquares(IEnumerable<Square> squares, PlayerSymbol p)
    {
        return squares.All(square => Field[square.Row, square.Col] == p);
    }

    public WinType IsWinningMove(int row, int col, PlayerSymbol p)
    {
        if (MarkedAllSquares(RowLine(row), p))
            return WinType.Row;
        if (MarkedAllSquares(ColLine(col), p))
            return WinType.Col;
        if (MarkedAllSquares(Diag(), p))
            return WinType.Diag;
        if (MarkedAllSquares(Anti(), p))
            return WinType.Anti;

        return WinType.None;
    }

    private bool IsWinnerAt(int row, int col, PlayerSymbol p)
    {
        return MarkedAllSquares(RowLine(row), p) || MarkedAllSquares(ColLine(col), p) || MarkedAllSquares(Diag(), p) ||
               MarkedAllSquares(Anti(), p);
    }

    public bool HasWinner(PlayerSymbol p)
    {
        for (var row = 0; row < FieldSize; row++)
        for (var col = 0; col < FieldSize; col++)
            if (IsWinnerAt(row, col, p))
                return true;

        return false;
    }

    public (int Row, int Col)[] OpenPositions()
    {
        var positions = new List<(int Row, int Col)>();
        for (var row = 0; row < FieldSize; row++)
            for (var col = 0; col < FieldSize; col++)
                if (Field[row, col] == PlayerSymbol.None)
                    positions.Add((row, col));

        return positions.ToArray();
    }

    public void MakeTestMove(int row, int col, PlayerSymbol p)
    {
        Field[row, col] = p;
    }

    private IEnumerable<Square> RowLine(int row) => Range.Select(i => new Square(row, i));
    private IEnumerable<Square> ColLine(int col) => Range.Select(i => new Square(i, col));
    private IEnumerable<Square> Diag() => Range.Select(i => new Square(i, i));
    private IEnumerable<Square> Anti() => Range.Select(i => new Square(i, FieldSize - 1 - i));

    #region ICloneable Members
    private GameField(PlayerSymbol[,] gameState, int fieldSize, int[] range)
    {
        Field = new PlayerSymbol[fieldSize, fieldSize];
        for (var i = 0; i < fieldSize; i++)
            for (var j = 0; j < fieldSize; j++)
                Field[i, j] = gameState[i, j];

        FieldSize = fieldSize;
        Range = range;
    }

    public object Clone()
    {
        return new GameField(Field, FieldSize, Range);
    }
    #endregion
}

public class Board
{
    public GameField GameField;
    public uint Turn;
    public bool BoardDone;

    private readonly List<Square> Diag = new();
    private readonly List<Square> Anti = new();

    public bool IsDiag(int row, int col) => Diag.Any(square => square.Row == row && square.Col == col);
    public bool IsAnti(int row, int col) => Anti.Any(square => square.Row == row && square.Col == col);

    public Board()
    {
        GameField = new GameField(3);
        FillDiagAnti();
    }

    public bool IsMoveAllowed(int row, int col)
    {
        return !BoardDone && GameField.Field[row, col] == PlayerSymbol.None;
    }

    public bool IsFieldFull()
    {
        return GameField.Field.Length == Turn;
    }

    private void FillDiagAnti()
    {
        Diag.Clear();
        Anti.Clear();

        for (var i = 0; i < GameField.FieldSize; i++)
        {
            Diag.Add(new Square(i, i));
            Anti.Add(new Square(i, GameField.FieldSize - 1 - i));
        }
    }

    public void Empty(int fieldSize)
    {
        GameField = new GameField(fieldSize);
        BoardDone = false;
        Turn = 0;

        FillDiagAnti();
    }

    public void MakeMove(int row, int col, PlayerSymbol p)
    {
        if (!IsMoveAllowed(row, col))
            return;

        Turn++;
        GameField.Field[row, col] = p;
    }
}

public class RoundInfo
{
    public CancellationTokenSource CancellationToken = new();
    private static readonly Random Rng = new();

    public Winner? Winner;
    public Board Board = new();
    public bool CalculatingAIMove;
    public DateTime ComputeStart = DateTime.Now;

    private Difficulty Difficulty;
    private Player[] Players;
    private int CurrentPlayerIndex;

    public Player CurrentPlayer => Players[CurrentPlayerIndex];
    private static PlayerSymbol GetOpposite(PlayerSymbol p) => p == PlayerSymbol.X ? PlayerSymbol.O : PlayerSymbol.X;

    public RoundInfo(Configuration config)
    {
        Difficulty = config.Difficulty;
        Board.Empty(config.FieldSize);
        Players = new[] { new Player { Playername = config.Username, Symbol = PlayerSymbol.X }, new Player { Playername = Difficulty.String(), Symbol = PlayerSymbol.O, IsAI = true } };
        CurrentPlayerIndex = Rng.Next(0, 2);

        CheckIfAIMove();
    }

    private void NextPlayer()
    {
        CurrentPlayerIndex = CurrentPlayerIndex == 0 ? 1 : 0;
    }

    private Winner? IsGameDone(int row, int col)
    {
        var winType = Board.GameField.IsWinningMove(row, col, CurrentPlayer.Symbol);
        if (winType != WinType.None)
            return new Winner {Player = CurrentPlayer, Type = winType, Row = row, Col = col};

        if (Board.IsFieldFull())
            return new Winner { Player = new Player { Symbol = PlayerSymbol.None }, Type = WinType.Tie };

        return null;
    }

    private void MakeMove(Square move) => MakeMove(move.Row, move.Col);
    public void MakeMove(int row, int col)
    {
        if (!Board.IsMoveAllowed(row, col))
            return;

        Board.MakeMove(row, col, CurrentPlayer.Symbol);

        Winner = IsGameDone(row, col);
        if (Winner != null)
            Board.BoardDone = true;
        else
            NextPlayer();

        CheckIfAIMove();
    }

    public void NewRound(Configuration config)
    {
        try
        {
            CancellationToken.Cancel();
            CancellationToken = new CancellationTokenSource();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        Difficulty = config.Difficulty;
        Board.Empty(config.FieldSize);
        Players = new[] { new Player { Playername = config.Username, Symbol = PlayerSymbol.X }, new Player { Playername = Difficulty.String(), Symbol = PlayerSymbol.O, IsAI = true } };
        CurrentPlayerIndex = Rng.Next(0, 2);

        Winner = null;

        CheckIfAIMove();
    }

    private void CheckIfAIMove()
    {
        if (Board.BoardDone || !CurrentPlayer.IsAI)
            return;

        ComputeStart = DateTime.Now;
        Task.Run(() =>
        {
            try
            {
                AIMove();
            }
            catch
            {
                // ignored
            }
        }, CancellationToken.Token);
    }

    public void Dispose()
    {
        CancellationToken.Cancel();
        CancellationToken.Dispose();
    }

    #region AI
    // From: http://www.wisamyacteen.com/2012/11/an-artificial-intelligence-example-tic-tac-toe-using-c/
    public class Evaluator
    {
        public double Evaluate(GameField b, PlayerSymbol p)
        {
            if (b.HasWinner(p))
                return double.PositiveInfinity;
            if (b.HasWinner(GetOpposite(p)))
                return double.NegativeInfinity;

            var maxValue = EvaluatePiece(b, p);
            var minValue = EvaluatePiece(b, GetOpposite(p));

            return maxValue - minValue;
        }

        private double EvaluatePiece(GameField b, PlayerSymbol p)
        {
            return EvaluateRows(b, p) + EvaluateColumns(b, p) + EvaluateDiagonals(b, p);
        }

        private double EvaluateRows(GameField b, PlayerSymbol p)
        {
            var score = 0.0;
            for (var i = 0; i < b.FieldSize; i++)
            {
                var count = 0;
                var rowClean = true;
                for (var j = 0; j < b.FieldSize; j++)
                {
                    var boardPiece = b.Field[i, j];
                    if (boardPiece == p)
                    {
                        count++;
                    }
                    else if (boardPiece == GetOpposite(p))
                    {
                        rowClean = false;
                        break;
                    }
                }

                // if we get here then the row is clean (an open row)
                if (rowClean && count != 0)
                    score += count;
            }

            return score;
        }

        private double EvaluateColumns(GameField b, PlayerSymbol p)
        {
            var score = 0.0;
            for (var j = 0; j < b.FieldSize; j++)
            {
                var count = 0;
                var rowClean = true;
                for (var i = 0; i < b.FieldSize; i++)
                {
                    var boardPiece = b.Field[i, j];
                    if (boardPiece == p)
                    {
                        count++;
                    }
                    else if (boardPiece == GetOpposite(p))
                    {
                        rowClean = false;
                        break;
                    }
                }

                // if we get here then the row is clean (an open row)
                if (rowClean && count != 0)
                    score += count; //Math.Pow(count, count);

            }

            return score;
        }

        private double EvaluateDiagonals(GameField b, PlayerSymbol p)
        {
            // go down and to the right diagonal first
            var count = 0;
            var diagonalClean = true;

            var score = 0.0;
            for (var i = 0; i < b.FieldSize; i++)
            {
                var boardPiece = b.Field[i, i];
                if (boardPiece == p)
                    count++;

                if (boardPiece == GetOpposite(p))
                {
                    diagonalClean = false;
                    break;
                }
            }

            if (diagonalClean && count > 0)
                score += count;// Math.Pow(count, count);

            // now try the other way
            var row = 0;
            var col = 2;
            count = 0;
            diagonalClean = true;

            while (row < b.FieldSize && col >= 0)
            {
                var boardPiece = b.Field[row, col];
                if (boardPiece == p)
                    count++;

                if (boardPiece == GetOpposite(p))
                {
                    diagonalClean = false;
                    break;
                }

                row++;
                col--;
            }

            if (count > 0 && diagonalClean)
                score += count;

            return score;
        }
    }

    public abstract class Node
    {
        protected readonly List<Node> Children;
        protected readonly Evaluator Evaluator = new();
        protected readonly GameField GameField;

        public double Value;
        public PlayerSymbol MyPiece;

        private Square Move;
        private Node BestMoveNode;

        public Square BestMove => BestMoveNode.Move;

        protected Node(GameField b, Node parent, Square move)
        {
            GameField = b;
            Move = move;

            if (parent != null)
                MyPiece = GetOpposite(parent.MyPiece);

            Children = new List<Node>();
        }

        private void SelectBestMove()
        {
            // if no children there is no best move for the node
            if (Children.Count == 0)
            {
                BestMoveNode = null;
                return;
            }

            // sort the children so that the first element contains the 'best' node
            BestMoveNode = SortChildren(Children)[0];
            Value = BestMoveNode.Value;
        }

        public void FindBestMove(int depth, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            if (depth <= 0)
                return;

            // expand this node -- subclasses provide their own implementation of this
            GenerateChildren();

            // evaluate each child
            // if there is a winner there is no need to go further down
            // the tree
            // sends the Evaluate() message to each child node, which is implemented
            // by subclasses
            EvaluateChildren();

            if (Children.Exists(c => c.IsGameEndingNode()))
            {
                // the best move depends on the subclass
                SelectBestMove();
            }
            else
            {
                Parallel.ForEach(Children, c => c.FindBestMove(depth - 1, token));
                SelectBestMove();
            }
        }

        private bool IsGameEndingNode()
        {
            return double.IsInfinity(Value);
        }

        private void EvaluateChildren()
        {
            foreach (var child in Children)
                child.Evaluate();
        }

        protected abstract void Evaluate();
        protected abstract void GenerateChildren();
        protected abstract List<Node> SortChildren(IEnumerable<Node> unsortedChildren);
    }

    private class MaxNode : Node
    {
        public MaxNode(GameField b, Node parent, Square m) : base(b, parent, m) { }

        protected override void GenerateChildren()
        {
            foreach (var (row, col) in GameField.OpenPositions())
            {
                var b = (GameField) GameField.Clone();
                var m = new Square(row, col);

                b.MakeTestMove(row, col, MyPiece);
                Children.Add(new MinNode(b, this, m));
            }
        }

        protected override void Evaluate()
        {
            Value = Evaluator.Evaluate(GameField, MyPiece);
        }

        protected override List<Node> SortChildren(IEnumerable<Node> unsortedChildren)
        {
            return unsortedChildren.OrderByDescending(n=> n.Value).ToList();
        }
    }

    private class MinNode : Node
    {
        public MinNode(GameField b, Node parent, Square m) : base(b, parent, m) { }

        protected override void GenerateChildren()
        {
            foreach (var (row, col) in GameField.OpenPositions())
            {
                var b = (GameField) GameField.Clone();
                var m = new Square(row, col);

                b.MakeTestMove(row, col, MyPiece);
                Children.Add(new MaxNode(b, this, m));
            }
        }

        protected override void Evaluate()
        {
            Value = Evaluator.Evaluate(GameField, GetOpposite(MyPiece));
        }

        protected override List<Node> SortChildren(IEnumerable<Node> unsortedChildren)
        {
            return unsortedChildren.OrderBy(n => n.Value).ToList();
        }
    }

    private async void AIMove()
    {
        CalculatingAIMove = true;

        try
        {
            // Delay for UI effect
            await Task.Delay(1000);

            // We can either move randomly at first, or choose middle spot
            // Middle Spot is only picked from Hard AI
            if (Board.Turn == 0)
            {
                MakeMove(Difficulty != Difficulty.Hard ? GetRandomMove() : GetMidSquare());
                return;
            }

            Node root = new MaxNode(Board.GameField, null, null);
            root.MyPiece = CurrentPlayer.Symbol;
            root.FindBestMove(Board.GameField.FieldSize - 2 + (int) Difficulty, CancellationToken.Token);

            MakeMove(root.BestMove.Row, root.BestMove.Col);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "AI calculation weren't successful");
        }
        finally
        {
            CalculatingAIMove = false;
        }
    }

    private Square GetRandomMove()
    {
        var openPositions = Board.GameField.OpenPositions();
        var square = openPositions[Rng.Next(openPositions.Length)];
        return new Square(square.Row, square.Col);
    }

    private Square GetMidSquare()
    {
        if (Board.GameField.FieldSize % 2 != 1)
            return GetRandomMove();

        var middle = Board.GameField.FieldSize / 2;
        return new Square(middle, middle);
    }
    #endregion
}

public static class PlayerTypeExtensions
{
    public static string String(this PlayerSymbol symbol)
    {
        return symbol switch
        {
            PlayerSymbol.None => "None",
            PlayerSymbol.X => "X",
            PlayerSymbol.O => "O",
            _ => "U"
        };
    }

    public static string String(this Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "Easy AI",
            Difficulty.Medium => "Medium AI",
            Difficulty.Hard => "Hard AI",
            _ => "Unknown"
        };
    }

    public static string Name(this Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "Easy",
            Difficulty.Medium => "Medium",
            Difficulty.Hard => "Hard",
            _ => "Unknown"
        };
    }
}