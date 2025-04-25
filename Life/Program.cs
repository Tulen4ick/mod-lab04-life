using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using System.Runtime.CompilerServices;

namespace cli_life
{

    public class World_Settings
    {
        public int width { get; set; }
        public int height { get; set; }
        public int cellSize { get; set; }
        public double liveDensity { get; set; }
    }
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public Cell[,] Cells;
        public readonly int CellSize;
        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1, bool Loaded = false)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            if (!Loaded) Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
        public void Save_Board()
        {
            List<string> state = new List<string>();
            for (int row = 0; row < Rows; row++)
            {
                StringBuilder line = new StringBuilder();
                for (int col = 0; col < Columns; col++)
                {
                    line.Append(Cells[col, row].IsAlive ? '1' : '0');
                }
                state.Add(line.ToString());
            }
            File.WriteAllLines(@"..\saved_state.txt", state);
        }
        public void Load_Board()
        {
            if (!File.Exists(@"..\saved_state.txt")) return;
            var state = File.ReadAllLines(@"..\saved_state.txt");
            for (int row = 0; row < Rows && row < state.Length; row++)
            {
                for (int col = 0; col < Columns && col < state[row].Length; col++)
                {
                    Cells[col, row].IsAlive = state[row][col] == '1';
                }
            }
        }

        public void Load_Figure(int figure_number, HashSet<string[]> basic_figures)
        {
            string[] figure = basic_figures.ElementAt(figure_number - 1);
            int startX = rand.Next(0, Columns - figure[0].Length + 1);
            int startY = rand.Next(0, Rows - figure.Length + 1);
            for (int y = 0; y < figure.Length; ++y)
            {
                for (int x = 0; x < figure[0].Length; ++x)
                {
                    Cells[x + startX, y + startY].IsAlive = figure[y][x] == '1';
                }
            }

        }
    }
    class Program
    {
        static Board board;
        static HashSet<string[]> basic_figures = new HashSet<string[]>();
        static void Load_figures()
        {
            basic_figures.Add(File.ReadAllLines(@"figures\blinker.txt"));
            basic_figures.Add(File.ReadAllLines(@"figures\block.txt"));
            basic_figures.Add(File.ReadAllLines(@"figures\boat.txt"));
            basic_figures.Add(File.ReadAllLines(@"figures\glider.txt"));
            basic_figures.Add(File.ReadAllLines(@"figures\hive.txt"));
            basic_figures.Add(File.ReadAllLines(@"figures\tub.txt"));
        }
        static private void Reset(bool Loaded)
        {
            string jsonString = File.ReadAllText(@"..\settings.json");
            World_Settings settings = JsonSerializer.Deserialize<World_Settings>(jsonString);
            board = new Board(
                settings.width,
                settings.height,
                settings.cellSize,
                settings.liveDensity,
                Loaded);
            if (Loaded) board.Load_Board();
            Load_figures();
        }
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        static void Main(string[] args)
        {
            Reset(true);
            while (true)
            {
                Console.Clear();
                Render();
                board.Advance();
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).KeyChar;
                    if (key == 'S') board.Save_Board();
                    if (key == 'L') board.Load_Board();
                    if (key == '1' || key == '2' || key == '3' || key == '4' || key == '5' || key == '6')
                    {
                        board.Load_Figure(int.Parse(key.ToString()), basic_figures);
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}