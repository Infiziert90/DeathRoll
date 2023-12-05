using System.Diagnostics;
using DeathRoll.Windows;

namespace DeathRoll.Data;

public class Minesweeper
{
    private static readonly Random Rng = new();

    public enum SquareState : byte
    {
        Hidden = 0,
        Open = 1,
        Flagged = 2
    }

    public struct Square
    {
        public byte Value;
        public byte AdjacentMines;
        public SquareState State;
        public bool Exploded;

        public bool IsMine => Value == 1;
        public bool NoAdjacentMines => AdjacentMines == 0;
        public string Symbol => State == SquareState.Flagged
            ? FontAwesomeIcon.Flag.ToIconString() : IsMine
                ? FontAwesomeIcon.LandMineOn.ToIconString() : AdjacentMines.ToString();
        public bool UsesIconFont => State == SquareState.Flagged || IsMine;

        public Vector4 NumberColor()
        {
            if (IsMine || State == SquareState.Flagged)
                return Helper.Black;

            return AdjacentMines switch
            {
                1 => Helper.DarkBlue,
                2 => Helper.DarkGreen,
                3 => Helper.DarkRed,
                4 => Helper.DarkViolet,
                5 => Helper.DarkBrown,
                6 => Helper.DarkCyan,
                7 => Helper.DarkGrey,
                8 => Helper.Black,
                _ => Helper.LighterGrey,
            };
        }
    }

    public readonly Square[,] Board;
    public bool FirstClick;

    public bool GameOver;
    public bool PlayerWon;

    public readonly int Rows;
    public readonly int Cols;

    private int FlagsPlaced;
    private readonly int NumberOfMines;
    private readonly Stopwatch Timer = new();

    public int MinesLeft => NumberOfMines - FlagsPlaced;
    public long Time => Timer.ElapsedMilliseconds / 1000;
    private Square GetSquare((int Row, int Col) point) => Board[point.Row, point.Col];

    public Minesweeper((int Rows, int Cols, int Mines) size)
    {
        NumberOfMines = size.Mines;
        Board = new Square[size.Rows, size.Cols];
        Rows = Board.GetLength(0);
        Cols = Board.GetLength(1);

        Timer.Reset();

        FlagsPlaced = 0;
        FirstClick = true;
    }

    private void PopulateBoard(int fRow, int fCol)
    {
        FirstClick = false;

        var mines = 0;
        while (mines < NumberOfMines)
        {
            var row = Rng.Next(Rows);
            var col = Rng.Next(Cols);

            if (row == fRow && col == fCol)
                continue;

            if (Board[row, col].Value != 0)
                continue;

            mines++;
            Board[row, col].Value = 1;
        }


        foreach (var (row, col) in ForGenerator())
            Board[row, col].AdjacentMines = (byte) AdjacentGenerator(row, col).Count(tuple => GetSquare(tuple).IsMine);
    }

    private bool CheckWinCondition()
    {
        return ForGenerator().Select(GetSquare).Where(s => !s.IsMine).Any(s => s.State == SquareState.Hidden);
    }

    private readonly List<(int, int)> CheckedMiddles = new();
    private void OpenAdjacentZeros(int row, int col)
    {
        CheckedMiddles.Add((row, col));
        foreach (var (aRow, aCol) in AdjacentGenerator(row, col).Where(tuple => !CheckedMiddles.Contains(tuple)))
        {
            Board[aRow, aCol].State = SquareState.Open;
            if (Board[aRow, aCol].NoAdjacentMines)
                OpenAdjacentZeros(aRow, aCol);
        }
    }

    public IEnumerable<(int Row, int Col)> ForGenerator()
    {
        for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Cols; col++)
                yield return (row, col);
    }

    private IEnumerable<(int aRow, int aCol)> AdjacentGenerator(int row, int col)
    {
        var rowMin = row > 0 ? -1 : 0;
        var rowMax = row + 1 < Rows ? 1 : 0;

        var colMin = col > 0 ? -1 : 0;
        var colMax = col + 1 < Cols ? 1 : 0;

        for (var i = rowMin; i <= rowMax; i++)
            for (var j = colMin; j <= colMax; j++)
                if (i != 0 || j != 0)
                    yield return (row + i, col + j);
    }

    #region Events
    public void ProcessLeftClick(int row, int col)
    {
        if (FirstClick)
        {
            Timer.Restart();
            PopulateBoard(row, col);
        }

        try
        {
            ref var square = ref Board[row, col];

            square.State = SquareState.Open;
            if (square.IsMine)
            {
                Timer.Stop();
                GameOver = true;
                PlayerWon = false;
                square.Exploded = true;

                foreach (var (i, j) in ForGenerator().Where(tuple => GetSquare(tuple).IsMine))
                    Board[i, j].State = SquareState.Open;

                return;
            }

            if (square.NoAdjacentMines)
            {
                CheckedMiddles.Clear();
                OpenAdjacentZeros(row, col);
            }

            if (!CheckWinCondition())
            {
                Timer.Stop();
                GameOver = true;
                PlayerWon = true;
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Things went horrible wrong.");
        }
    }

    public void ProcessRightClick(int row, int col)
    {
        try
        {
            switch (Board[row, col].State)
            {
                case SquareState.Open:
                    return;
                case SquareState.Flagged:
                    FlagsPlaced--;
                    Board[row, col].State = SquareState.Hidden;
                    break;
                default:
                    FlagsPlaced++;
                    Board[row, col].State = SquareState.Flagged;
                    break;
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Things went horrible wrong.");
        }
    }
    #endregion
}