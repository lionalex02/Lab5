using GraphEditor.Algorithms;
using GraphEditor.Constants;
using GraphEditor.ViewModels;
using GraphEditor.Visualization;
using GraphEditor.YunPart;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GraphEditor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Graph _graph;
        private GraphVM _graphVM;
        public GraphVM GraphVM
        {
            get
            {
                return _graphVM;
            }
            set
            {
                _graphVM = value;
                OnPropertyChanged();
            }
        }

        private DispatcherTimer _timer = new DispatcherTimer();
        public event PropertyChangedEventHandler? PropertyChanged;

        VisualizationManager visualizationManager;

        private bool _isRunning = false;

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsStepableForward));
                OnPropertyChanged(nameof(IsStepableBackward));
            }
        }

        private NodeVM _startNode;
        private NodeVM _endNode;

        public NodeVM StartNode
        {
            get => _startNode;
            set
            {
                _startNode = value;
                OnPropertyChanged();
            }
        }

        public NodeVM EndNode
        {
            get => _endNode;
            set
            {
                _endNode = value;
                OnPropertyChanged();
            }
        }

        private void InitGraph()
        {
            _graph = new Graph();

            var Nodes = _graph.Nodes;
            var Edges = _graph.Edges;

            Nodes.Add(new Node(1, "1"));
            Nodes.Add(new Node(2, "23"));
            Nodes.Add(new Node(3, "3"));

            Edges.Add(new Edge(Nodes[0], Nodes[1]));
            Edges.Add(new Edge(Nodes[0], Nodes[2]));
            Edges.Add(new Edge(Nodes[1], Nodes[2]));

            GraphVM = new GraphVM(_graph, NodeClickCommand, CanvasClickCommand);

            var NodesVM = new ObservableCollection<NodeVM>();
            var EdgesVM = new ObservableCollection<EdgeVM>();

            NodesVM.Add(new NodeVM(Nodes[0], 100, 100));
            NodesVM.Add(new NodeVM(Nodes[1], 200, 100));
            NodesVM.Add(new NodeVM(Nodes[2], 300, 200));

            EdgesVM.Add(new EdgeVM(NodesVM[0], NodesVM[1]));
            EdgesVM.Add(new EdgeVM(NodesVM[0], NodesVM[2]));
            EdgesVM.Add(new EdgeVM(NodesVM[1], NodesVM[2]));

            GraphVM.NodesVM = NodesVM;
            GraphVM.EdgesVM = EdgesVM;
        }

        private Dictionary<string, IAlgorithm> _algorithms = new Dictionary<string, IAlgorithm>()
        {
            { "test", new TestAlgorithm() }
        };

        public Dictionary<string, IAlgorithm> Algorithms { get => _algorithms; }

        public IAlgorithm SelectedAlgorithm { get; set; }

        public RelayCommand<IAlgorithm> RunStopAlgorithm => new(RunStop, CanRun);
        public RelayCommand<IAlgorithm> StepForwardCommand => new(StepForward, CanStepForward);
        public RelayCommand<IAlgorithm> StepBackwardCommand => new(StepBackward, CanStepBackward);
        public RelayCommand<object> FindShortestPathCommand => new(FindShortestPath, CanFindShortestPath);

        public bool CanRun(object _) => (SelectedAlgorithm != null && !IsRunning) || (IsRunning && SelectedAlgorithm != null);
        public bool CanStepForward(object? _ = null) => visualizationManager != null && GraphVM.NodesVM.Count > 0 && !IsRunning && visualizationManager.CanStepForward();
        public bool CanStepBackward(object? _ = null) => visualizationManager != null && GraphVM.NodesVM.Count > 0 && !IsRunning && visualizationManager.CanStepBackward();
        public bool CanFindShortestPath(object _) => !IsRunning;

        public bool IsStepableForward => CanStepForward();
        public bool IsStepableBackward => CanStepBackward();
        public RelayCommand<NodeVM> NodeClickCommand => new(NodeClick, CanNodeClick);
        public RelayCommand<NodeVM> NodeDoubleClickCommand => new(NodeDoubleClick, CanNodeClick);
        public RelayCommand<Canvas> CanvasClickCommand => new(CanvasClick, CanCanvasClick);
        public RelayCommand<Canvas> CanvasDoubleClickCommand => new(CanvasDoubleClick, CanCanvasClick);
        public bool CanNodeClick(NodeVM nodeVM) => !IsRunning;
        public bool CanCanvasClick(Canvas canvas) => !IsRunning;

        public async void RunStop(IAlgorithm obj)
        {
            if (IsRunning)
            {
                IsRunning = false;
                _timer.Stop();
                return;
            }
            _timer.Stop();
            Console.WriteLine("Run");
            IsRunning = true;
            visualizationManager = await VisualizationManager.WarmUp(GraphVM, SelectedAlgorithm);
            InitializeTimer();
        }

        public async void StepForward(object? obj = null)
        {
            if (CanStepForward())
                GraphVM = visualizationManager.StepForward();
        }

        public async void StepBackward(object? obj = null)
        {
            if (CanStepBackward())
                GraphVM = visualizationManager.StepBackward();
        }

        public async void TimerTick(object? sender, EventArgs e)
        {
            if (!IsRunning || !visualizationManager.CanStepForward()) return;
            GraphVM = visualizationManager.StepForward();
            if (!visualizationManager.CanStepForward())
            {
                IsRunning = false;
            }
            Console.WriteLine("Tick");
        }

        private GraphClickStateManager _clickStateManager = new GraphClickStateManager();

        private void NodeClick(NodeVM nodeVM)
        {
            var coords = Mouse.GetPosition(GraphCanvas);
            if (_clickStateManager.ProcessClick((int)coords.X, (int)coords.Y, nodeVM) is NewEdgeClick newEdgeClick)
            {
                GraphVM.AddEdge(newEdgeClick.NodeFrom, newEdgeClick.NodeTo);
            }
        }

        private void NodeDoubleClick(NodeVM nodeVM)
        {
            if (StartNode == null)
            {
                StartNode = nodeVM;
                nodeVM.Color = Colors.Green; // ���� ��� ��������� �����
            }
            else if (EndNode == null)
            {
                EndNode = nodeVM;
                nodeVM.Color = Colors.Blue; // ���� ��� �������� �����
            }
            else
            {
                // ����� ������
                StartNode.Color = StartNode.OriginalColor;
                EndNode.Color = EndNode.OriginalColor;
                StartNode = nodeVM;
                EndNode = null;
                nodeVM.Color = Colors.Green; // ���� ��� ��������� �����
            }
        }


        private void CanvasClick(Canvas canvas)
        {
            Console.WriteLine($"Canvas {canvas != null}");
        }

        private void CanvasDoubleClick(Canvas canvas)
        {
            var coords = Mouse.GetPosition(canvas);
            GraphVM.AddNode(new Point(coords.X - NodeVM.DefaultWidth / 2, coords.Y - NodeVM.DefaultHeight / 2));
            Console.WriteLine($"CanvasDoubleClick: {coords} Canvas Coords: {canvas.ActualWidth} {canvas.ActualHeight}");
        }

        public MainWindow()
        {
            AllocConsole();
            InitGraph();
            InitializeComponent();
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += TimerTick;
            _timer.Start();
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void FindShortestPath(object _)
        {
            if (StartNode == null || EndNode == null)
            {
                MessageBox.Show("Please select start and end nodes.");
                return;
            }

            var shortestPathLogic = new ShortestPathLogic();
            var shortestPath = shortestPathLogic.FindShortestPath(StartNode, EndNode, GraphVM);

            // ���������� ������������
            foreach (var node in GraphVM.NodesVM)
            {
                node.Color = node.OriginalColor; // �������������� ��������� ����� ���� ������
            }

            foreach (var node in shortestPath)
            {
                node.Color = Colors.Red; // ��������� ������ ����������� ����
            }

            OnPropertyChanged(nameof(GraphVM));
        }

    }
}
