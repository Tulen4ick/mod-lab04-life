using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life;
using System.Text.Json;
using System.Reflection;


namespace Life.Tests
{
    [TestClass]
    public class CellTests
    {
        [TestMethod]
        public void Cell_DetermineNextLiveState_AliveWith2Neighbors_StaysAlive()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 2));
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell._isAliveNext);
        }

        [TestMethod]
        public void Cell_DetermineNextLiveState_AliveWith4Neighbors_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 4));
            cell.DetermineNextLiveState();
            Assert.IsFalse(cell._isAliveNext);
        }

        [TestMethod]
        public void Cell_DetermineNextLiveState_DeadWith3Neighbors_BecomesAlive()
        {
            var cell = new Cell { IsAlive = false };
            cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 3));
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell._isAliveNext);
        }
    }

    [TestClass]
    public class BoardTests
    {
        private Board _board;

        [TestInitialize]
        public void Setup()
        {
            _board = new Board(10, 10, 1, 0.5);
        }

        [TestMethod]
        public void Board_Initialize_CellsCorrectlyConnected()
        {
            var cell = _board.Cells[0, 0];
            Assert.AreEqual(8, cell.neighbors.Count);
            Assert.IsTrue(cell.neighbors.Contains(_board.Cells[_board.Columns - 1, _board.Rows - 1]));
        }

        [TestMethod]
        public void Board_ConnectNeighbors_ToroidalTopology()
        {
            var board = new Board(30, 30, 1);
            var edgeCell = board.Cells[0, 0];
            Assert.IsTrue(edgeCell.neighbors.Contains(board.Cells[29, 29]));
        }

        [TestMethod]
        public void SaveLoad_RoundTrip_PreservesState()
        {
            var board = new Board(10, 10, 1);
            board.Cells[2, 3].IsAlive = true;
            var currentDir = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDir);

            while (directory != null && !directory.GetFiles("*.csproj").Any())
            {
                directory = directory.Parent;
            }
            string saved_txt = Path.Combine(directory.FullName, "saved_state.txt");
            board.Save_Board(saved_txt);
            var newBoard = new Board(10, 10, 1, 0, true);
            newBoard.Load_Board(saved_txt);
            Assert.IsTrue(newBoard.Cells[2, 3].IsAlive);
        }
    }

    [TestClass]
    public class BoardAnalyzerTests
    {
        private Board _board;
        private BoardAnalyzer _analyzer;

        [TestInitialize]
        public void Setup()
        {
            _board = new Board(30, 30, 1, 0);
            _analyzer = new BoardAnalyzer(_board, new HashSet<FigureTemplate>());
        }

        [TestMethod]
        public void BoardAnalyzer_BFS_VisitsAllConnectedCells()
        {
            _board.Cells[5, 5].IsAlive = true;
            _board.Cells[5, 6].IsAlive = true;
            var cells = _analyzer.BFS(5, 5);
            Assert.AreEqual(2, cells.Count);
        }

        [TestMethod]
        public void BoardAnalyzer_BoardAnalysis_CorrectCounts()
        {
            _board.Cells[1, 1].IsAlive = true;
            var (figures, alive) = _analyzer.BoardAnalysis();
            Assert.AreEqual(1, figures);
            Assert.AreEqual(1, alive);
        }
    }

    [TestClass]
    public class FigureTemplateTests
    {
        private Board _board;
        private HashSet<FigureTemplate> figureVariants;

        [TestInitialize]
        public void Setup()
        {
            _board = new Board(50, 20, 1, 0.5);
            figureVariants = new HashSet<FigureTemplate>();
            var currentDir = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDir);

            while (directory != null && !directory.GetFiles("*.csproj").Any())
            {
                directory = directory.Parent;
            }
            //if (!File.Exists("TestState.txt")) return;
            var state = File.ReadAllLines(Path.Combine(directory.FullName, "TestState.txt"));
            for (int row = 0; row < _board.Rows && row < state.Length; row++)
            {
                for (int col = 0; col < _board.Columns && col < state[row].Length; col++)
                {
                    _board.Cells[col, row].IsAlive = state[row][col] == '1';
                }
            }
            string figuresPath = Path.Combine(directory.FullName, "figures");
            string figure1Path = Path.Combine(figuresPath, "blinker.txt");
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(figure1Path), "blinker"));
            string figure2Path = Path.Combine(figuresPath, "block.txt");
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(figure2Path), "block"));
            string figure3Path = Path.Combine(figuresPath, "boat.txt");
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(figure3Path), "boat"));
            string figure4Path = Path.Combine(figuresPath, "glider.txt");
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(figure4Path), "glider"));
            string figure5Path = Path.Combine(figuresPath, "hive.txt");
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(figure5Path), "hive"));
            string figure6Path = Path.Combine(figuresPath, "tub.txt");
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(figure6Path), "tub"));
        }

        [TestMethod]
        public void FigureTemplateTests_Recognize_Blinker()
        {
            var analyzer = new BoardAnalyzer(_board, figureVariants);
            var classification = analyzer.ClassificationOfShapes();
            Assert.IsTrue(classification.ContainsKey("blinker"));
            Assert.AreEqual(2, classification["blinker"]);
        }

        [TestMethod]
        public void FigureTemplateTests_Recognize_Block()
        {
            var analyzer = new BoardAnalyzer(_board, figureVariants);
            var classification = analyzer.ClassificationOfShapes();
            Assert.IsTrue(classification.ContainsKey("block"));
            Assert.AreEqual(2, classification["block"]);
        }

        [TestMethod]
        public void FigureTemplateTests_Recognize_Boat()
        {
            var analyzer = new BoardAnalyzer(_board, figureVariants);
            var classification = analyzer.ClassificationOfShapes();
            Assert.IsTrue(classification.ContainsKey("boat"));
            Assert.AreEqual(1, classification["boat"]);
        }

        [TestMethod]
        public void FigureTemplateTests_Recognize_Glider()
        {
            var analyzer = new BoardAnalyzer(_board, figureVariants);
            var classification = analyzer.ClassificationOfShapes();
            Assert.IsTrue(classification.ContainsKey("glider"));
            Assert.AreEqual(1, classification["glider"]);
        }

        [TestMethod]
        public void FigureTemplateTests_Recognize_Hive()
        {
            var analyzer = new BoardAnalyzer(_board, figureVariants);
            var classification = analyzer.ClassificationOfShapes();
            Assert.IsTrue(classification.ContainsKey("hive"));
            Assert.AreEqual(1, classification["hive"]);
        }

        [TestMethod]
        public void FigureTemplateTests_Recognize_Tub()
        {
            var analyzer = new BoardAnalyzer(_board, figureVariants);
            var classification = analyzer.ClassificationOfShapes();
            Assert.IsTrue(classification.ContainsKey("tub"));
            Assert.AreEqual(2, classification["tub"]);
        }
    }

    [TestClass]
    public class SettingsTests
    {
        [TestMethod]
        public void World_Settings_Deserialization_FromJson()
        {
            var json = "{ \"width\": 100, \"height\": 80, \"cellSize\": 5, \"liveDensity\": 0.3 }";
            var settings = JsonSerializer.Deserialize<World_Settings>(json);
            Assert.AreEqual(100, settings.width);
        }
    }
}