using Microsoft.VisualStudio.TestTools.UnitTesting;
using cli_life;
using System.Text.Json;


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
            var board = new Board(30, 30, 10);
            var edgeCell = board.Cells[0, 0];
            Assert.IsTrue(edgeCell.neighbors.Contains(board.Cells[29, 29]));
        }

        [TestMethod]
        public void SaveLoad_RoundTrip_PreservesState()
        {
            var board = new Board(10, 10, 1);
            board.Cells[2, 3].IsAlive = true;
            board.Save_Board();
            var newBoard = new Board(10, 10, 1, 0, true);
            newBoard.Load_Board();
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
            _board = new Board(30, 30, 1);
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
            if (!File.Exists("TestState.txt")) return;
            var state = File.ReadAllLines("TestState.txt");
            for (int row = 0; row < _board.Rows && row < state.Length; row++)
            {
                for (int col = 0; col < _board.Columns && col < state[row].Length; col++)
                {
                    _board.Cells[col, row].IsAlive = state[row][col] == '1';
                }
            }
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\blinker.txt"), "blinker"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\block.txt"), "block"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\boat.txt"), "boat"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\glider.txt"), "glider"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\hive.txt"), "hive"));
            figureVariants.Add(new FigureTemplate(File.ReadAllLines(@"figures\tub.txt"), "tub"));
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