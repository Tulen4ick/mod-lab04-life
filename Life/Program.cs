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
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data;
using System.Reflection.Metadata;

namespace cli_life
{

    public class World_Settings
    {
        public int width { get; set; }
        public int height { get; set; }
        public int cellSize { get; set; }
        public double liveDensity { get; set; }
    }
    public class BoardAnalyzer
    {
        private bool[,] visited;
        private Board board;
        private Dictionary<Cell, (int X, int Y)> cellsCoordinates;
        private HashSet<FigureTemplate> templates;
        public BoardAnalyzer(Board board, HashSet<FigureTemplate> templates)
        {
            this.board = board;
            this.templates = templates;
            visited = new bool[board.Columns, board.Rows];
            cellsCoordinates = new Dictionary<Cell, (int X, int Y)>();
            for (int x = 0; x < board.Columns; ++x)
            {
                for (int y = 0; y < board.Rows; ++y)
                {
                    cellsCoordinates[board.Cells[x, y]] = (x, y);
                }
            }
        }

        private List<(int X, int Y)> BFS(int startX, int startY)
        {
            var cells = new List<(int X, int Y)>();
            Queue<(int X, int Y)> queue = new Queue<(int X, int Y)>();
            queue.Enqueue((startX, startY));
            cells.Add((startX, startY));
            visited[startX, startY] = true;
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                foreach (Cell neighbor in board.Cells[x, y].neighbors)
                {
                    if (cellsCoordinates.TryGetValue(neighbor, out var coordinates))
                    {
                        if (!visited[coordinates.X, coordinates.Y] && neighbor.IsAlive)
                        {
                            visited[coordinates.X, coordinates.Y] = true;
                            queue.Enqueue((coordinates.X, coordinates.Y));
                            cells.Add((coordinates.X, coordinates.Y));
                        }
                    }
                }
            }
            return cells;
        }

        public (int CountOfFigures, int CountOfAlive) BoardAnalysis()
        {
            visited = new bool[board.Columns, board.Rows];
            int CountOfAlive = 0;
            int CountOfFigures = 0;
            for (int x = 0; x < board.Columns; ++x)
            {
                for (int y = 0; y < board.Rows; ++y)
                {
                    if (board.Cells[x, y].IsAlive)
                    {
                        CountOfAlive++;
                        if (!visited[x, y])
                        {
                            BFS(x, y);
                            CountOfFigures++;
                        }
                    }
                }
            }
            return (CountOfFigures, CountOfAlive);
        }

        public Dictionary<string, int> ClassificationOfShapes()
        {
            var result = new Dictionary<string, int>();
            var combos = AllCombos();
            foreach (var combo in combos)
            {
                string type = "unknown";
                foreach (var template in templates)
                {
                    if (template.ComboInPatterns(combo))
                    {
                        type = template.Name;
                        break;
                    }
                }
                if (result.ContainsKey(type))
                {
                    result[type]++;
                }
                else
                    result[type] = 1;
            }
            return result;
        }

        private List<string[]> AllCombos()
        {
            var result = new List<string[]>();
            visited = new bool[board.Columns, board.Rows];
            for (int x = 0; x < board.Columns; ++x)
            {
                for (int y = 0; y < board.Rows; ++y)
                {
                    if (!visited[x, y] && board.Cells[x, y].IsAlive)
                    {
                        var cells = BFS(x, y);
                        result.Add(CreateMatrix(cells));
                    }
                }
            }
            return result;
        }

        private string[] CreateMatrix(List<(int X, int Y)> cells)
        {
            int minX = cells[0].X;
            int minY = cells[0].Y;
            int maxX = cells[0].X;
            int maxY = cells[0].Y;
            foreach (var _cell in cells)
            {
                if (minX > _cell.X)
                    minX = _cell.X;
                if (minY > _cell.Y)
                    minY = _cell.Y;
                if (maxX < _cell.X)
                    maxX = _cell.X;
                if (maxY < _cell.Y)
                    maxY = _cell.Y;
            }
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            var result = new string[height];
            for (int y = 0; y < height; ++y)
            {
                result[y] = "";
                for (int x = 0; x < width; ++x)
                {
                    if (cells.Contains((x + minX, y + minY)))
                    {
                        result[y] = result[y] + '1';
                    }
                    else
                        result[y] = result[y] + '0';
                }
            }
            return result;
        }
    }
    public class FigureTemplate
    {
        public string Name { get; set; }
        public HashSet<string[]> Patterns { get; set; }

        public FigureTemplate(string[] basic_pattern, string name)
        {
            Name = name;
            Patterns = MatrixVariants.GetAllSymmetries(basic_pattern);
        }

        public bool ComboInPatterns(string[] combo)
        {
            foreach (var pattern in Patterns)
            {
                if (ComboEqualPattern(pattern, combo))
                    return true;
            }
            return false;
        }

        private bool ComboEqualPattern(string[] combo, string[] pattern)
        {
            if (combo.Length != pattern.Length)
                return false;
            for (int i = 0; i < combo.Length; i++)
            {
                if (combo[i].Length != pattern[i].Length)
                    return false;
                for (int j = 0; j < combo[i].Length; j++)
                {
                    if (combo[i][j] != pattern[i][j])
                        return false;
                }
            }
            return true;
        }
    }
    public class MatrixVariants
    {
        public static HashSet<string[]> GetAllSymmetries(string[] basic_matrix)
        {
            var allMatrices = new HashSet<string[]>();
            string[] current = basic_matrix;
            for (int i = 0; i < 4; ++i)
            {
                allMatrices.Add(current);
                current = Rotate90(current);
            }
            string[] reflected = HorizontalReflection(basic_matrix);
            current = reflected;
            for (int i = 0; i < 4; ++i)
            {
                allMatrices.Add(current);
                current = Rotate90(current);
            }
            return allMatrices;
        }

        private static string[] Rotate90(string[] matrix)
        {
            if (matrix == null || matrix.Length == 0)
                return Array.Empty<string>();
            int rows = matrix.Length;
            int cols = matrix[0].Length;
            char[][] rotated = new char[cols][];
            for (int i = 0; i < cols; i++)
            {
                rotated[i] = new char[rows];
                for (int j = 0; j < rows; j++)
                {
                    rotated[i][j] = matrix[rows - j - 1][i];
                }
            }
            string[] result = new string[cols];
            for (int i = 0; i < cols; i++)
            {
                result[i] = new string(rotated[i]);
            }
            return result;
        }

        private static string[] HorizontalReflection(string[] matrix)
        {
            int size = matrix.Length;
            string[] reflected = new string[size];
            for (int i = 0; i < size; ++i)
            {
                reflected[i] = new string(matrix[i].Reverse().ToArray());
            }
            return reflected;
        }
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

        public void Load_Figure(int figure_number, HashSet<FigureTemplate> figureVariants)
        {
            string[] figure = figureVariants.ElementAt(figure_number - 1).Patterns.ElementAt(0);
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
        static HashSet<FigureTemplate> figureVariants = new HashSet<FigureTemplate>();
        static void Load_figures()
        {
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\blinker.txt"), "blinker"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\block.txt"), "block"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\boat.txt"), "boat"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\glider.txt"), "glider"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\hive.txt"), "hive"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\tub.txt"), "tub"));
        }
        static private void Reset(bool Loaded, double additionalDensity)
        {
            string jsonString = File.ReadAllText(@"..\settings.json");
            World_Settings settings = JsonSerializer.Deserialize<World_Settings>(jsonString);
            if (additionalDensity < 0)
                additionalDensity = settings.liveDensity;
            board = new Board(
                settings.width,
                settings.height,
                settings.cellSize,
                additionalDensity,
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

        static int StartTheGame(double density, int attempt)
        {
            Reset(false, density);
            int generation = 0;
            int stablePhaseTime = 10;
            int possibleStability = 0;
            int countOfAlivePoints = 0;
            while (true)
            {
                Console.Clear();
                Render();
                board.Advance();
                generation++;
                var analyzer = new BoardAnalyzer(board, figureVariants);
                var classification = analyzer.ClassificationOfShapes();
                Console.WriteLine("Shape statistics: ");
                foreach (var shape in classification)
                {
                    Console.WriteLine($"{shape.Key}: {shape.Value}");
                }
                var (x, y) = analyzer.BoardAnalysis();
                Console.WriteLine("Count of figures: " + x);
                Console.WriteLine("Count of points: " + y);
                if (countOfAlivePoints == y)
                {
                    possibleStability++;
                    if (possibleStability == stablePhaseTime)
                    {
                        if (attempt != 0)
                        {
                            string filename = @"average_number_of_generations\LiveDensity" + density.ToString() + ".txt";
                            string text = attempt.ToString() + " запуск: количество поколений: " + generation.ToString() + "\n";
                            File.AppendAllText(filename, text);
                        }
                        return generation;
                    }
                }
                else
                {
                    possibleStability = 0;
                }
                countOfAlivePoints = y;
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).KeyChar;
                    if (key == 'S') board.Save_Board();
                    if (key == 'L') board.Load_Board();
                    if (key == '1' || key == '2' || key == '3' || key == '4' || key == '5' || key == '6')
                    {
                        board.Load_Figure(int.Parse(key.ToString()), figureVariants);
                    }
                }
            }
        }

        static void CreatingMultipleLaunchData()
        {
            for (double density = 0.1; density < 1; density += 0.1)
            {
                int countOfGenerations = 0;
                for (int i = 1; i <= 10; ++i)
                {
                    countOfGenerations += StartTheGame(density, i);
                }
                string filename = @"average_number_of_generations\LiveDensity" + density.ToString() + ".txt";
                string text = "Среднее количество поколений: " + ((double)countOfGenerations / (double)10).ToString() + "\n";
                File.AppendAllText(filename, text);
            }
        }

        static void CreatingDataForGraph()
        {
            for (double density = 0.63; density <= 1; density += 0.02)
            {
                int countOfGenerations = StartTheGame(density, 0);
                string filename = "DataForGraph.txt";
                string text = $"{density} {countOfGenerations}\n";
                File.AppendAllText(filename, text);
            }
        }
        static void Main(string[] args)
        {
            CreatingDataForGraph();
        }
    }
}