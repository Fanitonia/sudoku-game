﻿using System.Diagnostics;

namespace SudokuLibrary
{
    public class Sudoku
    {
        private Cell[,] field = new Cell[9, 9];
        private Cell[,] solvedField = new Cell[9, 9];
        private const int EMPTY_CELL = 0;
        private Stopwatch timer = new Stopwatch();
        private int step = 0;
        private string test;

        public long SolvedTime { get { return timer.ElapsedMilliseconds; } }
        public int SolvedStep { get { return step; } }

        public bool SetCellValue(int cordX, int cordY, int value)
        {
            if (value > 9 || value < 1)
                throw new Exception("Value is invalid (it must be between 1-9)");

            if (!IsCordValid(cordX, cordY))
                throw new Exception("Coordinates are invalid");

            if (field[cordY, cordX].canChange)
            {
                field[cordY, cordX].value = value;
                return true;
            }
            else
                return false;
        }

        public int GetCellValue(int cordX, int cordY, bool getFromSolvedVersion)
        {
            if (!IsCordValid(cordX, cordY))
                throw new Exception("Coordinates are invalid");

            if (getFromSolvedVersion)
            {
                if (solvedField[0,0] == null)
                    throw new Exception("There is no solved version of the puzzle");
                return solvedField[cordY, cordX].value;
            }
                
            else
                return field[cordY, cordX].value;
        }

        /// <summary>
        /// Deletes the value of the specified cell.
        /// </summary>
        /// <returns>False if specified cell cannot be changed.</returns>
        public bool DeleteCellValue(int cordX, int cordY)
        {
            if (!IsCordValid(cordX, cordY))
                throw new Exception("Coordinates are invalid");

            if (field[cordY, cordX].canChange)
            {
                field[cordY, cordX].value = 0;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Checks if the cell is part of the puzzle and cannot be changed by the user.
        /// </summary>
        public bool CanCellChange(int cordX, int cordY)
        {
            if (!IsCordValid(cordX, cordY))
                throw new Exception("Coordinates are invalid");

            return field[cordY, cordX].canChange;
        }

        public int GetNumberOfEmptyCells()
        {
            return GetNumberOfEmptyCells(field);
        }

        /// <summary>
        /// Creates an empty Sudoku board (cells are considered empty if their value is 0).
        /// </summary>
        public void CreateEmptyField()
        {
            for (int cordY = 0; cordY < 9; cordY++)
            {
                for (int cordX = 0; cordX < 9; cordX++)
                {
                    field[cordY, cordX] = new Cell();
                }
            }
        }

        /// <summary>
        /// Generates a Sudoku puzzle with the specified number of clues.
        /// </summary>
        public void GeneratePuzzle(int clues)
        {
            Random random = new Random();
            int cordX, cordY;
            do
            {
                CreateEmptyField();
                // this is for creating more randomness
                for (int i = 0; i < 10; i++)
                {
                    do
                    {
                        cordY = random.Next(9);
                        cordX = random.Next(9);
                    } while (field[cordY, cordX].value != EMPTY_CELL);

                    try
                    {
                        field[cordY, cordX].value = field[cordY, cordX].pNumbers[random.Next(field[cordY, cordX].pNumbers.Count)];
                        field[cordY, cordX].canChange = false;
                    }
                    catch (Exception)
                    {
                        break;
                    }

                    UpdateAllPotentials(field);
                }
            } while (!Solve());

            CreateEmptyField();
            for(int i = 0; i < clues; i++)
            {
                do
                {
                    cordY = random.Next(9);
                    cordX = random.Next(9);
                } while (field[cordY, cordX].value != EMPTY_CELL);

                field[cordY, cordX].value = solvedField[cordY, cordX].value;
                field[cordY, cordX].canChange = false;
            }
        }

        /// <summary>
        /// Checks if a value can be placed in the specified position of the Sudoku puzzle. 
        /// </summary>
        /// <returns>Returns false if the value cannot be placed at the specified position, otherwise true.</returns>
        public bool IsPositionSuitable(int cordX, int cordY, int value)
        {
            return IsPositionSuitable(cordX, cordY, value, field);
        }

        /// <summary>
        /// Checks if Sudoku solved correctly.
        /// </summary>
        public bool IsSudokuSolved()
        {
            return IsSudokuSolved(field);
        }

        /// <summary>
        /// Checks if the current state of the puzzle is valid.
        /// </summary>
        public bool IsSudokuValid()
        {
            int tmpHolder;
            for (int cordY = 0; cordY < 9; cordY++)
            {
                for (int cordX = 0; cordX < 9; cordX++)
                {
                    tmpHolder = field[cordY, cordX].value;
                    field[cordY, cordX].value = EMPTY_CELL;
                    if (tmpHolder == EMPTY_CELL || IsPositionSuitable(cordX, cordY, tmpHolder))
                        field[cordY, cordX].value = tmpHolder;
                    else
                    {
                        field[cordY, cordX].value = tmpHolder;
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Attempts to solve the puzzle for specified number of attempts.
        /// </summary>
        public bool TrySolve(int attempts)
        {
            if (attempts < 1)
                throw new Exception("Attempts cannot be smaller than 1");

            if (!IsSudokuValid())
                return false;

            for (int i = 0; i < attempts; i++)
            {
                if (Solve())
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to solve the puzzle once.
        /// </summary>
        public bool TrySolve()
        {
            return TrySolve(1);
        }

        // Copies the field to solvedField and tries to solve it.
        private bool Solve()
        {
            Random random = new Random();
            CopyAndSetBoard(field, solvedField);

            step = 0;
            timer.Restart();
            timer.Start();
            UpdateAllPotentials(solvedField);

            do
            {
                foreach (Cell cell in solvedField)
                {
                    if (cell.pNumbers.Count == 0 && cell.value == EMPTY_CELL)
                        return false;

                    step++;
                }

                foreach (Cell cell in solvedField)
                {
                    if (cell.pNumbers.Count == FindSmallestPotential(solvedField) && cell.value == EMPTY_CELL)
                    {
                        cell.value = cell.pNumbers[random.Next(cell.pNumbers.Count)];
                        UpdateAllPotentials(solvedField);
                        break;
                    }

                    step++;
                }
            } while (!IsSudokuSolved(solvedField));

            timer.Stop();
            return true;
        }

        private void CopyAndSetBoard(Cell[,] original, Cell[,] copy)
        {
            bool canChange;
            for (int cordY = 0; cordY < 9; cordY++)
            {
                for (int cordX = 0; cordX < 9; cordX++)
                {
                    if (original[cordY, cordX].value != EMPTY_CELL)
                    {
                        canChange = false;
                        original[cordY, cordX].canChange = false;
                    }
                    else
                        canChange = true;

                    copy[cordY, cordX] = new Cell(original[cordY, cordX].value, original[cordY, cordX].pNumbers, canChange);
                }
            }
        }

        // updates potential list of a specific cell
        private void UpdateCellPotentials(int cordX, int cordY, Cell[,] board)
        {
            List<int> tmpNumbers = new List<int>();
            for (int testValue = 1; testValue <= 9; testValue++)
            {
                if (IsPositionSuitable(cordX, cordY, testValue, board))
                    tmpNumbers.Add(testValue);
                step++;
            }
            board[cordY, cordX].pNumbers.Clear();
            board[cordY, cordX].pNumbers.AddRange(tmpNumbers);
        }

        // updates the potential list of alll cells in the field
        private void UpdateAllPotentials(Cell[,] board)
        {
            for (int cordY = 0; cordY < 9; cordY++)
            {
                for (int cordX = 0; cordX < 9; cordX++)
                    UpdateCellPotentials(cordX, cordY, board);
            }
        }

        private int FindSmallestPotential(Cell[,] board)
        {
            int smallest = 9;
            foreach (Cell cell in board)
            {
                if (cell.pNumbers.Count < smallest && cell.value == EMPTY_CELL)
                    smallest = cell.pNumbers.Count;
                step++;
            }
            return smallest;
        }

        private bool IsPositionSuitable(int cordX, int cordY, int value, Cell[,] board)
        {
            if (!IsCordValid(cordX, cordY))
                throw new Exception("Coordinates are invalid");
            else if (value > 9 || value < 0)
                throw new Exception("Value is invalid (it must be between 1-9)");

            // checks the column and the row of the position
            for (int i = 0; i < 9; i++)
            {
                if (board[cordY, i].value == value)
                    return false;

                else if (board[i, cordX].value == value)
                    return false;
                step++;
            }

            // finding top left position of the 3x3 are position's part of
            int rowStart = cordY - (cordY % 3);
            int columnStart = cordX - (cordX % 3);

            // checks the 3x3 area position's part of
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[rowStart + i, columnStart + j].value == value)
                        return false;
                    step++;
                }
            }

            return true;
        }

        private bool IsSudokuSolved(Cell[,] board)
        {
            int tmpHolder;
            for (int cordY = 0; cordY < 9; cordY++)
            {
                for (int cordX = 0; cordX < 9; cordX++)
                {
                    tmpHolder = board[cordY, cordX].value;
                    board[cordY, cordX].value = EMPTY_CELL;
                    if (IsPositionSuitable(cordX, cordY, tmpHolder, board) && tmpHolder != EMPTY_CELL)
                        board[cordY, cordX].value = tmpHolder;
                    else
                    {
                        test = $"{cordX},{cordY} = {tmpHolder}";
                        board[cordY, cordX].value = tmpHolder;
                        return false;
                    }
                }
            }
            return true;
        }

        private bool IsCordValid(int cordX, int cordY)
        {
            if (cordX > 8 || cordX < 0)
                return false;
            else if (cordY > 8 || cordY < 0)
                return false;

            return true;
        }

        private int GetNumberOfEmptyCells(Cell[,] board)
        {
            int emptyCell = 0;

            for (int cordY = 0; cordY < 9; cordY++)
            {
                for (int cordX = 0; cordX < 9; cordX++)
                {
                    if (board[cordY, cordX].value == 0)
                    {
                        emptyCell++;
                    }
                }
            }

            return emptyCell;
        }
    }
}