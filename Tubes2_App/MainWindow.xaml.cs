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
        SortedSet<string> uniqueAccounts = new SortedSet<string>();
        Bitmap graphBitmap;
        string currentAccount;
        string currentTargetFriend;
        int lastIndexCurrentAccount;
        int lastIndexCurrentTargetFriend;
        Dictionary<string, List<string>> adjacencyList;
        List<string> exploreRoute;
        TextBlock descriptionTextBlock;
        TextBlock friendsTextBlock;
        TextBlock exploreTextBlock;
        bool DFSSolution;
        string selectedRadio;
        string currentFilename;
        int fileOpenCount;
        int searchingCount;

        // Constructor
        public MainWindow()
        {
            InitializeComponent();
            exploreRoute = new List<string>();
            descriptionTextBlock = new TextBlock();
            friendsTextBlock = new TextBlock();
            exploreTextBlock = new TextBlock();
            descriptionTextBlock.TextAlignment = TextAlignment.Center;
            currentFilename = "";
            fileOpenCount = 0;
            searchingCount = 0;
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            bool isExplorable;

            // Membersihkan canvas dan textBlock
            exploreCanvas.Children.Clear();
            exploreGraph.Children.Clear();
            exploreTextBlock.Inlines.Clear();
            exploreTextBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            exploreTextBlock.VerticalAlignment = VerticalAlignment.Top;
            exploreTextBlock.TextAlignment = TextAlignment.Center;

            if (currentTargetFriend == null)
            {
                System.Windows.Forms.MessageBox.Show("Harap memilih akun target yang hendak dieksplorasi terlebih dahulu"
                    , "Error Title", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if (selectedRadio == "")
            {
                System.Windows.Forms.MessageBox.Show("Harap memilih DFS atau BFS terlebih dahulu"
                    , "Error Title", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else {
                if(selectedRadio == "DFS")
                {
                    isExplorable = DFS_Explore();
                }
                else
                {
                    isExplorable = BFS_Explore();
                }

                Run text;
                var bc = new BrushConverter();

                text = new Run("\nExplore Friends With " + currentTargetFriend);
                text.Foreground = (System.Windows.Media.Brush)bc.ConvertFrom("#FF522E92");
                text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                exploreTextBlock.Inlines.Add(text);
                if (isExplorable)
                {

                    text = new Run("\n\nNama akun : " + currentAccount + " dan " + currentTargetFriend);
                    text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                    exploreTextBlock.Inlines.Add(text);
                    text = new Run("\n\n" + (exploreRoute.Count - 2).ToString() + " degree connection");
                    text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                    exploreTextBlock.Inlines.Add(text);
                    for (int i=0;i<exploreRoute.Count;i++)
                    {
                        if (i == 0)
                        {
                            text = new Run("\n" + exploreRoute[i]);
                            text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                            exploreTextBlock.Inlines.Add(text);
                        }
                        else
                        {
                            text = new Run(" -> " + exploreRoute[i]);
                            text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                            exploreTextBlock.Inlines.Add(text);
                        }
                    }

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
                    exploreGraph.Children.Add(myImage3);
                }
                else
                {
                    text = new Run("\n\nNama akun : " + currentAccount + " dan " + currentTargetFriend);
                    text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                    exploreTextBlock.Inlines.Add(text);
                    text = new Run("\n\nTidak ada jalur koneksi yang tersedia");
                    text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                    exploreTextBlock.Inlines.Add(text);
                    text = new Run("\nAnda harus memulai koneksi baru itu sendiri");
                    text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                    exploreTextBlock.Inlines.Add(text);
                }

                // Merender ulang komponen XAML exploreCanvas
                exploreCanvas.Children.Add(exploreTextBlock);
            }
        }

        private void DFS_Checked(object sender, RoutedEventArgs e)
        {
            selectedRadio = "DFS";
        }

        private void BFS_Checked(object sender, RoutedEventArgs e)
        {
            selectedRadio = "BFS";
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
                friendCanvas.Children.Clear();
                exploreCanvas.Children.Clear();
                graphCanvas.Children.Clear();
                exploreGraph.Children.Clear();
                uniqueAccounts.Clear();
                selectedRadio = "";
                currentAccount = null;
                currentTargetFriend = null;
                lastIndexCurrentAccount = -1;
                lastIndexCurrentTargetFriend = -1;
                selectedRadio = "";
                BFS_Radio.IsChecked = false;
                DFS_Radio.IsChecked = false;


                // Get directory file .txt yang dipilih
                string sFileName = fileDialog.FileName;
                currentFilename = sFileName.Substring(sFileName.LastIndexOf('\\') + 1).Replace(".txt", "");
                fileOpenCount++;
                currentFilename = currentFilename + "-" + fileOpenCount.ToString();

                // Baca line per line kemudian dimasukkan ke array of string lines
                lines = System.IO.File.ReadAllLines(@sFileName);

                System.Windows.Controls.Image myImage3 = new System.Windows.Controls.Image();
                myImage3.Source = null;

                // Membuat Graph yang disimpan sebagai .png
                MakeGraph(currentFilename, false);

                string path = Environment.CurrentDirectory;
                BitmapImage bi3 = new BitmapImage();
                bi3.BeginInit();
                bi3.UriSource = new Uri(@path + ("/graph-" + currentFilename + ".png"), UriKind.Absolute); // TODO : Unique generator
                bi3.EndInit();
                myImage3.Stretch = Stretch.None;
                myImage3.Source = bi3;
                myImage3.Width = 600;
                myImage3.Height = 500;
                myImage3.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                myImage3.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                graphCanvas.Children.Add(myImage3);

                // Membuat Adjancency List berdasarkan input .txt
                generateAdjacencyList();

                // handler untuk event ChangeComboBox
                handleUpdateComboBox();
            }
        }

        private void generateAdjacencyList()
        {
            int countLines = lines.Length;
            adjacencyList = new Dictionary<string, List<string>>();

            // Membaca dan mempersiapkan adjancency list dengan membaca input
            // secara baris per baris.
            for (int i=1;i<countLines;i++)
            {
                string[] splitLine = lines[i].ToString().Split(' ');
                string source = splitLine[0];
                string dest = splitLine[1];

                if (!adjacencyList.ContainsKey(source))
                {
                    adjacencyList[source] = new List<string>();
                }
                if (!adjacencyList.ContainsKey(dest))
                {
                    adjacencyList[dest] = new List<string>();
                }
                adjacencyList[source].Add(dest);
                adjacencyList[dest].Add(source);
            }
        }

        private void MakeGraph(string filename, bool isExploreGraph)
        {
            // Membuat Objek Graph
            Graph graph = new Graph("graph");

            if (!isExploreGraph)
            {
                // Create Graph Content
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] splitLine = lines[i].ToString().Split(' ');
                    string source = splitLine[0];
                    string dest = splitLine[1];

                    // Styling Graph
                    var edge = graph.AddEdge(source, dest);
                    edge.Attr.ArrowheadAtSource = ArrowStyle.None;
                    edge.Attr.ArrowheadAtTarget = ArrowStyle.None;
                    Node src = graph.FindNode(source);
                    Node target = graph.FindNode(dest);
                    src.Attr.Shape = Shape.Circle;
                    target.Attr.Shape = Shape.Circle;
                    src.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                    target.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                    src.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                    target.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                    edge.Attr.Color = Microsoft.Msagl.Drawing.Color.GhostWhite;
                    edge.Attr.Weight = 700;

                    // Menambah akun unik ke uniqueAccounts
                    uniqueAccounts.Add(source);
                    uniqueAccounts.Add(dest);
                }

                // Create Graph Image
                Microsoft.Msagl.GraphViewerGdi.GraphRenderer renderer = new Microsoft.Msagl.GraphViewerGdi.GraphRenderer(graph);
                renderer.CalculateLayout();
                graph.Attr.BackgroundColor = Microsoft.Msagl.Drawing.Color.Transparent;
                int height = 500;
                if (graph.Width > graph.Height && graph.Width > 400)
                {
                    height = 200;
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
                    height = 300;
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
                // Create Graph Content
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] splitLine = lines[i].ToString().Split(' ');
                    string source = splitLine[0];
                    string dest = splitLine[1];

                    // Styling Graph
                    var edge = graph.AddEdge(source, dest);
                    edge.Attr.ArrowheadAtSource = ArrowStyle.None;
                    edge.Attr.ArrowheadAtTarget = ArrowStyle.None;
                    Node src = graph.FindNode(source);
                    Node target = graph.FindNode(dest);
                    src.Attr.Shape = Shape.Circle;
                    target.Attr.Shape = Shape.Circle;

                    if (exploreRoute.Contains(source))
                    {
                        src.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightPink;
                        src.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                    }
                    else
                    {
                        src.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                        src.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                    }

                    if (exploreRoute.Contains(dest))
                    {
                        target.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightPink;
                        target.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                    }
                    else
                    {
                        target.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PeachPuff;
                        target.Attr.Color = Microsoft.Msagl.Drawing.Color.Purple;
                    }

                    if (exploreRoute.Contains(source) && exploreRoute.Contains(dest))
                    {
                        edge.Attr.Color = Microsoft.Msagl.Drawing.Color.Red;
                    }
                    else
                    {
                        edge.Attr.Color = Microsoft.Msagl.Drawing.Color.LightPink;
                    }
                }

                // Create Graph Image
                Microsoft.Msagl.GraphViewerGdi.GraphRenderer renderer = new Microsoft.Msagl.GraphViewerGdi.GraphRenderer(graph);
                renderer.CalculateLayout();
                graph.Attr.BackgroundColor = Microsoft.Msagl.Drawing.Color.Transparent;
                int height = 350;
                if (graph.Width > graph.Height && graph.Width * (height / graph.Height) > 300)
                {
                    height = 160;
                }
                else if (graph.Width / graph.Height > 1.3)
                {
                    height = 75;
                }
                else if (graph.Width / graph.Height > 1.5)
                {
                    height = 100;
                }
                else if (graph.Width * (height / graph.Height) > 500)
                {
                    height = 200;
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

                friendCanvas.Children.Clear();
                exploreCanvas.Children.Clear();
                friendsTextBlock.Inlines.Clear();
                descriptionTextBlock.Inlines.Clear();
                descriptionTextBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                exploreTextBlock.Inlines.Clear();
                exploreTextBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

                // Memanggil kembali rekomendasi teman.
                Friend_Recommendation();
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

        private void Friend_Recommendation()
        {
            // Mencetak judul
            Run text;
            text = new Run("\nFriend Recommendations for " + currentAccount);
            var bc = new BrushConverter();
            text.Foreground = (System.Windows.Media.Brush)bc.ConvertFrom("#FF522E92");
            text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
            descriptionTextBlock.Inlines.Add(text);

            HashSet<string> uniqueFriendRecommendations = new HashSet<string>();
            HashSet<string> sortedFriendRecommendations = new HashSet<string>();
            Dictionary<string, List<string>> mutualConnections = new Dictionary<string, List<string>>();
            List<int> numOfMutuals = new List<int>();

            // Pencarian Friend Recommendation berdasarkan mutual friends
            var currentNode = adjacencyList[currentAccount];
            for (int m = 0; m < currentNode.Count; m++)
            {
                var currentMutualNode = adjacencyList[currentNode[m]];
                for (int n = 0; n < currentMutualNode.Count; n++)
                {
                    var candidateFriend = currentMutualNode[n];
                    if (!mutualConnections.ContainsKey(candidateFriend))
                    {
                        List<string> temp = new List<string>();
                        mutualConnections[candidateFriend] = temp;
                    }
                    if (candidateFriend != currentAccount && !currentNode.Contains(candidateFriend))
                    {
                        uniqueFriendRecommendations.Add(candidateFriend);
                        if (!mutualConnections[candidateFriend].Contains(currentNode[m]))
                        {
                            mutualConnections[candidateFriend].Add(currentNode[m]);
                        }
                    }
                }
            }

            // Sort untuk ditampilkan dari mutual friends terbanyak lebih dahulu
            foreach (string friendRecommendation in uniqueFriendRecommendations)
            {
                numOfMutuals.Add(mutualConnections[friendRecommendation].Count);
            }
            numOfMutuals.Sort();

            for(int i=numOfMutuals.Count-1; i>=0; i--)
            {
                foreach (string friendRecommendation in uniqueFriendRecommendations)
                {
                    if(mutualConnections[friendRecommendation].Count == numOfMutuals[i])
                    {
                        sortedFriendRecommendations.Add(friendRecommendation);
                    }
                }
            }

            friendCanvas.Children.Add(descriptionTextBlock);
            // Mencetak Friend Recommendation ke layar
            int currentSpaceIncrement = 0;
            if(sortedFriendRecommendations.Count == 0)
            {
                friendsTextBlock = new TextBlock();
                friendsTextBlock.TextAlignment = TextAlignment.Center;

                text = new Run("\n\n\nTidak ada rekomendasi teman untuk akun ini");
                text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                friendsTextBlock.Inlines.Add(text);

                friendCanvas.Children.Add(friendsTextBlock);
            }
            foreach (string friendRecommendation in sortedFriendRecommendations)
            {
                friendsTextBlock = new TextBlock();
                friendsTextBlock.TextAlignment = TextAlignment.Center;

                for (int k = 0; k < currentSpaceIncrement; k++)
                {
                    friendsTextBlock.Inlines.Add(new Run("\n"));
                }
                text = new Run("\n\n\nNama akun : " + friendRecommendation);
                text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                friendsTextBlock.Inlines.Add(text);
                text = new Run("\n" + mutualConnections[friendRecommendation].Count + " mutual friends : ");
                text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                friendsTextBlock.Inlines.Add(text);
                for (int k = 0; k < mutualConnections[friendRecommendation].Count; k++)
                {
                    if(k != 0)
                    {
                        text = new Run(", " + mutualConnections[friendRecommendation][k]);
                    }
                    else
                    {
                        text = new Run("" + mutualConnections[friendRecommendation][k]);
                    }
                    text.Style = System.Windows.Application.Current.TryFindResource("VigaFont") as System.Windows.Style;
                    friendsTextBlock.Inlines.Add(text);
                }
                currentSpaceIncrement = currentSpaceIncrement + 4;
                friendCanvas.Children.Add(friendsTextBlock);
            }
        }

        private bool BFS_Explore()
        {
            // Inisiasi variabel
            Queue<string> BFSQueue = new Queue<string>();
            Dictionary<string, bool> visited = new Dictionary<string, bool>();
            bool solutionFound = false;
            List<string> expandNode;
            Dictionary<string, List<string>> Route = new Dictionary<string, List<string>>();
            string expandAccount;

            foreach (string node in uniqueAccounts)
            {
                visited[node] = false;
            }

            // BFS secara iteratif
            expandAccount = currentAccount;
            visited[expandAccount] = true;
            while (!solutionFound)
            {
                expandNode = adjacencyList[expandAccount];
                for (int i=0;i<expandNode.Count;i++)
                {
                    string currentFocusAccount = expandNode[i];
                    if (!visited[currentFocusAccount])
                    {
                        visited[currentFocusAccount] = true;
                        Route[currentFocusAccount] = new List<string>();
                        if (Route.ContainsKey(expandAccount))
                        {
                            Route[currentFocusAccount] = Route[currentFocusAccount].Concat(Route[expandAccount]).ToList();
                        }
                        Route[currentFocusAccount].Add(expandAccount);
                        BFSQueue.Enqueue(currentFocusAccount);
                        if (currentFocusAccount == currentTargetFriend)
                        {
                            Route[currentFocusAccount].Add(currentFocusAccount);
                            exploreRoute = Route[currentFocusAccount];
                            solutionFound = true;
                            break;
                        }
                    }
                }
                if(BFSQueue.Count != 0)
                {
                    expandAccount = BFSQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }
            return solutionFound;
        }

        private bool DFS_Explore()
        {
            // Main untuk pencarian dengan DFS. Menginisiasi variabel dan memanggil
            // DFS_Recursion (DFS secara rekursif).
            Dictionary<string, bool> visited = new Dictionary<string, bool>();
            List<string> Route = new List<string>();
            DFSSolution = false;
            int num_of_visited;

            foreach (string node in uniqueAccounts)
            {
                visited[node] = false;
            }

            num_of_visited = visited.Count;

            DFS_Recursion(currentAccount, visited, num_of_visited, Route);
            return DFSSolution;
        }

        private void DFS_Recursion(string currentFocusAccount, Dictionary<string, bool> visited, int num_of_visited, List<string> Route)
        {
            if (currentFocusAccount == currentTargetFriend)
            {
                // Basis apabila telah ditemukan friend yang hendak di-explore.
                DFSSolution = true;
                Route.Add(currentTargetFriend);
                exploreRoute = Route;
            }
            else if (num_of_visited < 1 && currentFocusAccount != currentTargetFriend && !DFSSolution)
            {
                // Basis terminasi karena tidak ditemukan path menuju target.
            }
            else
            {
                // Rekurens
                Route.Add(currentFocusAccount);
                visited[currentFocusAccount] = true;
                num_of_visited--;
                List<string> expandNode = adjacencyList[currentFocusAccount];
                int i = 0;

                while (i<expandNode.Count && !DFSSolution)
                {
                    if(!visited[expandNode[i]])
                    {
                        DFS_Recursion(expandNode[i], visited, num_of_visited, Route);
                    }
                    i++;
                }
                if(!DFSSolution)
                {
                    Route.Remove(currentFocusAccount);
                }
            }
        }
    }
}
