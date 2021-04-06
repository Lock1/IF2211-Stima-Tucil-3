using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Msagl.Drawing;

namespace Tubes2_App
{
    public partial class MainWindow : Window
    {
        // Attributes
        string[] lines;
        HashSet<string> uniqueAccounts = new HashSet<string>();
        HashSet<string> visitedRoute = new HashSet<string>();
        Bitmap graphBitmap;
        string currentAccount;
        string currentTargetFriend;
        int lastIndexCurrentAccount;
        int lastIndexCurrentTargetFriend;
        List<string> exploreRoute;
        List<string> coloredRoute;
        TextBlock friendsTextBlock;
        TextBlock exploreTextBlock;
        bool DFSSolution;
        string selectedRadio;
        string currentFilename;
        int fileOpenCount;
        int searchingCount;
        float TotalPathWeight;

        Dictionary<string, List<string>> adjacencyList;
        float[,] adjacencyMatrix;
        string[] nameList;

        // Constructor
        public MainWindow()
        {
            InitializeComponent();
            exploreRoute = new List<string>();
            friendsTextBlock = new TextBlock();
            exploreTextBlock = new TextBlock();
            currentFilename = "";
            fileOpenCount = 0;
            searchingCount = 0;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            bool isExplorable;

            // Membersihkan canvas dan textBlock
            resultGraphCanvas.Children.Clear();
            exploreTextBlock.Inlines.Clear();
            exploreTextBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            exploreTextBlock.VerticalAlignment = VerticalAlignment.Top;
            exploreTextBlock.TextAlignment = TextAlignment.Center;

            isExplorable = PathfindAStar(); // DEBUG
            if(isExplorable)
            {
                // Menggambar Explore Graph
                searchingCount++;
                MakeGraph(currentFilename + "-" + searchingCount.ToString(), true);
                System.Windows.Controls.Image myImage3 = new System.Windows.Controls.Image();
                myImage3.Source = null;

                // Setting Directory
                string path = Environment.CurrentDirectory;
                BitmapImage bi3 = new BitmapImage();
                bi3.BeginInit();
                bi3.UriSource = new Uri(@path + ("/explore-" + currentFilename + "-" + searchingCount.ToString() + ".png"), UriKind.Absolute); // TODO : Fix
                bi3.EndInit();

                // Setting Image Attributes
                myImage3.Stretch = Stretch.None;
                myImage3.Source = bi3;
                myImage3.Width = 200;
                myImage3.Height = 500;
                myImage3.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                myImage3.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                resultGraphCanvas.Children.Add(myImage3);
            }
        }

        private void Browse_File_Button(object sender, RoutedEventArgs e)
        {
            // Mempersiapkan pembacaan input .txt
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.DefaultExt = ".txt";
            fileDialog.Filter = "Text documents (.txt)|*.txt";
            fileDialog.Multiselect = false;

            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Clearing and Refreshing variables
                resultGraphCanvas.Children.Clear();
                graphCanvas.Children.Clear();
                uniqueAccounts.Clear();
                selectedRadio = "";
                currentAccount = null;
                currentTargetFriend = null;
                lastIndexCurrentAccount = -1;
                lastIndexCurrentTargetFriend = -1;
                selectedRadio = "";


                // Get directory file .txt yang dipilih
                string sFileName = fileDialog.FileName;
                currentFilename = sFileName.Substring(sFileName.LastIndexOf('\\') + 1).Replace(".txt", "");
                fileOpenCount++;
                currentFilename = currentFilename + "-" + fileOpenCount.ToString();

                // Baca line per line kemudian dimasukkan ke array of string lines
                lines = System.IO.File.ReadAllLines(@sFileName);

                System.Windows.Controls.Image myImage3 = new System.Windows.Controls.Image();
                myImage3.Source = null;

                // Membuat Adjancency List berdasarkan input .txt
                generateAdjacencyList();

                // Membuat Graph yang disimpan sebagai .png
                MakeGraph(currentFilename, false);

                string path = Environment.CurrentDirectory;
                BitmapImage bi3 = new BitmapImage();
                bi3.BeginInit();
                bi3.UriSource = new Uri(@path + ("/graph-" + currentFilename + ".png"), UriKind.Absolute);
                bi3.EndInit();
                myImage3.Stretch = Stretch.None;
                myImage3.Source = bi3;
                myImage3.Width = 600;
                myImage3.Height = 400;
                myImage3.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                myImage3.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                graphCanvas.Children.Add(myImage3);

                legendCanvas.Children.Clear();
                TextBlock legendTextBlock = new TextBlock();
                legendTextBlock.TextAlignment = TextAlignment.Center;
                Run text;
                text = new Run("\nLegend");
                var bc = new BrushConverter();
                text.Foreground = (System.Windows.Media.Brush)bc.ConvertFrom("#FFFFE5B4");
                text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                text.FontSize = 18;
                legendTextBlock.Inlines.Add(text);

                int i = 0;
                foreach (string account in uniqueAccounts)
                {
                    string alpha = char.ConvertFromUtf32(65 + i).ToString();
                    Run text2 = new Run("\n" + alpha + " : " + account);
                    text2.Foreground = (System.Windows.Media.Brush)bc.ConvertFrom("#F8B195");
                    text2.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                    text2.FontSize = 14;
                    legendTextBlock.Inlines.Add(text2);
                    i++;
                }

                legendCanvas.Children.Add(legendTextBlock);

                // handler untuk event ChangeComboBox
                handleUpdateComboBox();
            }
        }

        private void generateAdjacencyList()
        {
            int countLines = lines.Length;
            adjacencyList = new Dictionary<string, List<string>>();
            adjacencyMatrix = new float[countLines,countLines];
            nameList = new string[countLines];

            // Membaca dan mempersiapkan adjancency list dengan membaca input
            // secara baris per baris.

            for (int i=0;i<countLines;i++)
            {
                string[] splitLine = lines[i].ToString().Split(' ');
                string locationName = splitLine[0];
                nameList[i] = locationName;
                adjacencyList[locationName] = new List<string>();
            }

            for (int i=0;i<countLines;i++)
            {
                string[] splitLine = lines[i].ToString().Split(' ');
                string source = nameList[i];
                for (int j=0;j<countLines;j++)
                {
                    float edgeWeight = float.Parse(splitLine[j+1]);
                    adjacencyMatrix[i,j] = edgeWeight;
                    if (edgeWeight >= 0) {
                        string dest = nameList[j];
                        adjacencyList[source].Add(dest);
                        adjacencyList[dest].Add(source);
                    }
                }
            }
        }

        private void MakeGraph(string filename, bool isExploreGraph)
        {
            // Membuat Objek Graph
            Graph graph = new Graph("graph");

            if (!isExploreGraph)
            {
                // Create Graph Content
                for (int i = 0; i < lines.Length; i++)
                {
                    // bool isLoneNode = true;
                    for (int j = 0; j < lines.Length; j++)
                    {
                        int currentLength;
                        string source = nameList[i];


                        //string alphaSource = (uniqueAccounts.ToList().IndexOf(source) + 1).ToString();
                        currentLength = uniqueAccounts.Count;
                        uniqueAccounts.Add(source);
                        string alphaSource = char.ConvertFromUtf32(65 + uniqueAccounts.ToList().IndexOf(source)).ToString();
                        if (uniqueAccounts.Count > currentLength)
                        {
                            graph.AddNode(alphaSource);
                            Node src = graph.FindNode(alphaSource);
                            src.Attr.Shape = Shape.Circle;
                            src.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                            src.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                        }

                        if (i > j && adjacencyMatrix[i, j] >= 0)
                        {

                            string dest = nameList[j];
                            // Menambah akun unik ke uniqueAccounts
                            uniqueAccounts.Add(dest);

                            string alphaDest = char.ConvertFromUtf32(65 + uniqueAccounts.ToList().IndexOf(dest)).ToString();
                            //string alphaDest = (uniqueAccounts.ToList().IndexOf(dest) + 1).ToString();
                            // Styling Graph
                            var edge = graph.AddEdge(alphaSource, " " + adjacencyMatrix[i, j].ToString() + " ", alphaDest);
                            edge.Attr.ArrowheadAtSource = ArrowStyle.None;
                            edge.Attr.ArrowheadAtTarget = ArrowStyle.None;
                            edge.Label.FontColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                            edge.Label.FontSize = 5;
                            edge.Label.Size = new Microsoft.Msagl.Core.DataStructures.Size(60, 60);
                            edge.Attr.Color = Microsoft.Msagl.Drawing.Color.GhostWhite;
                            //edge.Label = new Microsoft.Msagl.Drawing.Label(adjacencyMatrix[i, j].ToString());
                            //edge.LabelText = "Halo";
                            // TODO : Edge length ?
                            edge.Attr.Weight = (int)adjacencyMatrix[i, j];
                            // edge.Attr.Weight = 1;

                            Node target = graph.FindNode(alphaDest);
                            target.Attr.Shape = Shape.Circle;
                            target.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                            target.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                            target.Attr.Padding = 20;



                        }
                    }
                }
                // System.Windows.Forms.MessageBox.Show(splitLine[j+1]); // DEBUG
                // System.Windows.Forms.MessageBox.Show("pout"); // DEBUG
                // System.Windows.Forms.MessageBox.Show("omasd"); // DEBUG


                // Create Graph Image
                Microsoft.Msagl.GraphViewerGdi.GraphRenderer renderer = new Microsoft.Msagl.GraphViewerGdi.GraphRenderer(graph);
                renderer.CalculateLayout();
                graph.Attr.BackgroundColor = Microsoft.Msagl.Drawing.Color.Transparent;
                int height = 360;
                if (graph.Width > graph.Height && graph.Width > 400)
                {
                    height = 160;
                }
                else if (graph.Width / graph.Height > 1.3)
                {
                    height = 150;
                }
                else if (graph.Width / graph.Height > 1.5)
                {
                    height = 120;
                }
                else if (graph.Width * (height / graph.Height) > 500)
                {
                    height = 250;
                }

                graphBitmap = new Bitmap((int)(graph.Width *
                (height / graph.Height)), height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                renderer.Render(graphBitmap);

                Bitmap cloneBitmap = (Bitmap)graphBitmap.Clone();

                string outputFileName = "graph-" + filename + ".png";
                // string outputFileName = "graph.png";

                cloneBitmap.Save(outputFileName);
                cloneBitmap.Dispose();
            }
            else
            {
                visitedRoute.Clear();
                foreach (string rut in exploreRoute)
                {
                    System.Windows.Forms.MessageBox.Show(rut, "Route end to start");
                }
                coloredRoute = new List<string>();
                // Create Graph Content
                for (int i = 0; i < lines.Length; i++)
                {
                    string source = nameList[i];

                    //string alphaSource = (uniqueAccounts.ToList().IndexOf(source) + 1).ToString();
                    uniqueAccounts.Add(source);
                    string alphaSource = char.ConvertFromUtf32(65 + uniqueAccounts.ToList().IndexOf(source)).ToString();
                    graph.AddNode(alphaSource);
                    Node src = graph.FindNode(alphaSource);
                    src.Attr.Shape = Shape.Circle;
                    src.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                    src.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;

                    if (exploreRoute.Contains(source))
                    {
                        src.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightPink;
                        src.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                        coloredRoute.Add(source);
                    }
                    else if (!coloredRoute.Contains(source))
                    {
                        src.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                        src.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                    }
                    // bool isLoneNode = true;
                    for (int j = 0; j < lines.Length; j++)
                    {


                        if (i > j && adjacencyMatrix[i, j] >= 0)
                        {

                            string dest = nameList[j];
                            // Menambah akun unik ke uniqueAccounts
                            uniqueAccounts.Add(dest);

                            string alphaDest = char.ConvertFromUtf32(65 + uniqueAccounts.ToList().IndexOf(dest)).ToString();
                            //string alphaDest = (uniqueAccounts.ToList().IndexOf(dest) + 1).ToString();
                            // Styling Graph
                            var edge = graph.AddEdge(alphaSource, " " + adjacencyMatrix[i, j].ToString() + " ", alphaDest);
                            edge.Attr.ArrowheadAtSource = ArrowStyle.None;
                            edge.Attr.ArrowheadAtTarget = ArrowStyle.None;
                            edge.Label.FontColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                            edge.Label.FontSize = 5;
                            edge.Label.Size = new Microsoft.Msagl.Core.DataStructures.Size(60, 60);
                            edge.Attr.Color = Microsoft.Msagl.Drawing.Color.GhostWhite;
                            //edge.Label = new Microsoft.Msagl.Drawing.Label(adjacencyMatrix[i, j].ToString());
                            //edge.LabelText = "Halo";
                            // TODO : Edge length ?
                            edge.Attr.Weight = (int)adjacencyMatrix[i, j];
                            // edge.Attr.Weight = 1;

                            Node target = graph.FindNode(alphaDest);
                            target.Attr.Shape = Shape.Circle;
                            target.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                            target.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                            target.Attr.Padding = 20;


                            if (exploreRoute.Contains(dest))
                            {
                                target.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightPink;
                                target.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                                coloredRoute.Add(dest);
                            }
                            else if (!coloredRoute.Contains(dest))
                            {
                                target.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                                target.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                            }

                            bool routeSibling = (exploreRoute.IndexOf(dest) - exploreRoute.IndexOf(source) == 1 || exploreRoute.IndexOf(dest) - exploreRoute.IndexOf(source) == -1);
                            if (exploreRoute.Contains(source) && exploreRoute.Contains(dest) && routeSibling)
                            {
                                edge.Label.FontColor = Microsoft.Msagl.Drawing.Color.MediumVioletRed;
                                edge.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
                                visitedRoute.Add(source);
                            }
                            else
                            {
                                edge.Attr.Color = Microsoft.Msagl.Drawing.Color.LightPink;
                            }

                        }
                    }
                }
                // System.Windows.Forms.MessageBox.Show(splitLine[j+1]); // DEBUG
                // System.Windows.Forms.MessageBox.Show("pout"); // DEBUG
                // System.Windows.Forms.MessageBox.Show("omasd"); // DEBUG


                // Create Graph Image
                Microsoft.Msagl.GraphViewerGdi.GraphRenderer renderer = new Microsoft.Msagl.GraphViewerGdi.GraphRenderer(graph);
                renderer.CalculateLayout();
                graph.Attr.BackgroundColor = Microsoft.Msagl.Drawing.Color.Transparent;
                int height = 360;
                if (graph.Width > graph.Height && graph.Width > 400)
                {
                    height = 160;
                }
                else if (graph.Width / graph.Height > 1.3)
                {
                    height = 150;
                }
                else if (graph.Width / graph.Height > 1.5)
                {
                    height = 120;
                }
                else if (graph.Width * (height / graph.Height) > 500)
                {
                    height = 250;
                }

                graphBitmap = new Bitmap((int)(graph.Width *
                (height / graph.Height)), height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                renderer.Render(graphBitmap);

                Bitmap cloneBitmap = (Bitmap)graphBitmap.Clone();

                string outputFileName = "explore-" + filename + ".png";
                // string outputFileName = "graph.png";

                cloneBitmap.Save(outputFileName);
                cloneBitmap.Dispose();
            }
        }

        private void handleUpdateComboBox()
        {
            // Refreshing Comboboxes
            Choose_Account_ComboBox.Items.Clear();
            Explore_ComboBox.Items.Clear();

            foreach (string account in uniqueAccounts)
            {
                Choose_Account_ComboBox.Items.Add(account);
                Explore_ComboBox.Items.Add(account);
            }
        }

        private void Choose_Account_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(Choose_Account_ComboBox.SelectedItem != null) {
                // Logic untuk handleSelectionChange (event) Choose_Account
                if(lastIndexCurrentTargetFriend >= 0)
                {
                    Explore_ComboBox.Items.Insert(lastIndexCurrentTargetFriend, currentAccount);
                }
                currentAccount = Choose_Account_ComboBox.SelectedItem.ToString();
                lastIndexCurrentTargetFriend = Explore_ComboBox.Items.IndexOf(currentAccount);
                Explore_ComboBox.Items.Remove(currentAccount);
            }
        }

        private void Explore_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(Explore_ComboBox.SelectedItem != null) {
                // Logic untuk handleSelectionChange (event) Explore
                if (lastIndexCurrentAccount >= 0)
                {
                    Choose_Account_ComboBox.Items.Insert(lastIndexCurrentAccount, currentTargetFriend);
                }
                currentTargetFriend = Explore_ComboBox.SelectedItem.ToString();
                lastIndexCurrentAccount = Choose_Account_ComboBox.Items.IndexOf(currentTargetFriend);
                Choose_Account_ComboBox.Items.Remove(currentTargetFriend);
            }
        }

        private int GetIndexFromNameList(string name) {
            for (int i = 0;i < nameList.Length; i++) {
                if (name == nameList[i])
                    return i;
            }
            return -1;
        }

        private bool PathfindAStar()
        {
            // Inisiasi variabel
            exploreRoute = new List<string>();
            Dictionary<string, List<string>> Route = new Dictionary<string, List<string>>(); // TODO : Use ?

            Queue<string> MoveQueue = new Queue<string>();
            Stack<Queue<Tuple<string,float>>> ChoiceStack = new Stack<Queue<Tuple<string,float>>>();
            Stack<Tuple<string,float>> CurrentTraversedRoute = new Stack<Tuple<string,float>>();
            Dictionary<string, bool> visited = new Dictionary<string, bool>();
            int currentLocationIndex = GetIndexFromNameList(currentAccount);
            string currentLocationName = currentAccount;
            bool isBacktracking = false;
            string targetLocationName = currentTargetFriend;
            bool isSolutionFound = true;

            foreach (string node in uniqueAccounts)
            {
                visited[node] = false;
            }

            visited[currentLocationName] = true;
            CurrentTraversedRoute.Push(new Tuple<string,float>(currentLocationName,0));
            TotalPathWeight = 0;

            // Pathfinding
            while (currentLocationName != currentTargetFriend) {
                // System.Windows.Forms.MessageBox.Show(currentLocationName, "Current Location"); // DEBUG

                // Creating branch only if not backtracking
                // Get sorted distance and put to choice stack
                if (!isBacktracking) {
                    // List carrying tuple of target location name and distance
                    List<Tuple<string,float>> distanceList = new List<Tuple<string,float>>();
                    for (int i = 0; i < nameList.Length; i++) {
                        if (currentLocationIndex != i && !visited[nameList[i]] && adjacencyMatrix[currentLocationIndex,i] >= 0)
                            distanceList.Add(new Tuple<string, float> (nameList[i], adjacencyMatrix[currentLocationIndex,i]));
                    }

                    // Sorting list with Linq, ascending order
                    List<Tuple<string,float>> sortedDistance = distanceList.OrderBy(obj=>obj.Item2).ToList();

                    // Creating available path queue from sorted list
                    Queue<Tuple<string,float>> AvailableBranch = new Queue<Tuple<string,float>>();
                    foreach (var entry in sortedDistance) {
                        AvailableBranch.Enqueue(entry);
                    }

                    // Push available path queue to choice stack
                    ChoiceStack.Push(AvailableBranch);
                }

                // Move taking
                if (ChoiceStack.Count != 0) {
                    // If choice stack is not exhausted
                    Queue<Tuple<string,float>> TopMostBranch = ChoiceStack.Peek();
                    if (TopMostBranch.Count != 0) {
                        // If choice queue in choice stack is not empty,
                        // Move to that location
                        currentLocationName = TopMostBranch.Peek().Item1;
                        currentLocationIndex = GetIndexFromNameList(currentLocationName);
                        float currentPathWeight = TopMostBranch.Peek().Item2;
                        TotalPathWeight += currentPathWeight;
                        TopMostBranch.Dequeue();

                        isBacktracking = false;
                        // | Trying new path, so algorithm is stopped backtracking
                        CurrentTraversedRoute.Push(new Tuple<string,float>(currentLocationName, currentPathWeight));
                        // | Put selected path to route stack
                        visited[currentLocationName] = true;
                        // | Flagging location as visited
                    }
                    else {
                        // If choice queue is empty, pop choice stack and backtrack
                        ChoiceStack.Pop();
                        isBacktracking = true;
                        // | Set mode to backtracking
                        visited[currentLocationName] = false;
                        // | Backtracking, removing old path visited flags
                        // string LastLocation = CurrentTraversedRoute.Peek(); // DEBUG
                        TotalPathWeight -= CurrentTraversedRoute.Peek().Item2;
                        CurrentTraversedRoute.Pop();
                        // | Remove last path from route stack
                    }
                }
                else {
                    // If choice stack is exhausted, then no path found
                    System.Windows.Forms.MessageBox.Show("path not found"); // DEBUG
                    // TODO : Do something if no path found
                    isSolutionFound = false;
                    break;
                }

            }

            // DEBUG
            // Reversed path printing
            string TargetToCurrent = "";
            while (CurrentTraversedRoute.Count != 0) {
                // System.Windows.Forms.MessageBox.Show(CurrentTraversedRoute.Peek(), count.ToString()); // DEBUG
                exploreRoute.Add(CurrentTraversedRoute.Peek().Item1);
                TargetToCurrent = TargetToCurrent + " " + CurrentTraversedRoute.Peek().Item1;
                CurrentTraversedRoute.Pop();
            }
            System.Windows.Forms.MessageBox.Show(TargetToCurrent, "Route end to start"); // DEBUG
            System.Windows.Forms.MessageBox.Show(TotalPathWeight.ToString(), "Route end to start - Weight"); // DEBUG
            exploreRoute.Reverse();
            return isSolutionFound;
        }
    }
}
