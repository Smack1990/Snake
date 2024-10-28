using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Snake;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    private readonly Dictionary<GridValue, ImageSource> gridValToImage = new()
    {
        { GridValue.Empty, Images.Empty },
        { GridValue.Snake, Images.Body },
        { GridValue.Food, Images.Food }
    };

    private readonly int rows = 40, cols = 40;
    private readonly Image[,] gridImages;
    private GameState gameState;
    public Player currentPlayer;
    private bool gameRunning;
    private bool isPaused = false;
    public MainWindow()
    {
        InitializeComponent();
        gridImages = SetupGrid();
        gameState = new GameState(rows, cols);
        currentPlayer = new Player("Guest", 0);

        DisplayHighscores();
        NamePanel.Visibility = Visibility.Visible;
        NameInput.Focus();
    }

    private void StartGame_Click(object sender, RoutedEventArgs e)
    {

        string playerName = NameInput.Text.Trim();
        


        if (string.IsNullOrEmpty(playerName))
        {

            MessageBox.Show("Please enter your name to start the game.");
            return;
        }
        if (Player.Highscores.ContainsKey(playerName))
        {
            var result = MessageBox.Show($"Name {playerName} already exists: Are you {playerName}?",
                "Confirm Name",
                MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                currentPlayer = new Player(playerName, Player.Highscores[playerName]);
                BeginGame();
                return;

            }
            else
            {
                NameInput.Clear();
                return;
            }
        }
        else
        {
            currentPlayer = new Player(playerName, 0);
            BeginGame();
        }






 

    }

    private void BeginGame()
    {
        DisplayHighscores();
        NamePanel.Visibility = Visibility.Hidden;
        Overlay.Visibility = Visibility.Visible;
        OverlayText.Text = "Press any key to start";

        this.Focus();

        gameRunning = false;
    }

    private async Task RunGame()
    {
        DisplayHighscores();
        Draw();
        await ShowCountDown();
        Overlay.Visibility = Visibility.Hidden;
        await GameLoop();
        //await ShowGameOver();
        await RestartGame();
        if (currentPlayer != null)
        {
            currentPlayer.AddOrUpdateScore(currentPlayer.Name, gameState.Score);
        }

        gameState = new GameState(rows, cols);
    }




    private void DisplayHighscores()
    {
        if (currentPlayer != null)
        {
            var topScores = currentPlayer.GetTopScores()
                                         .Take(5)
                                         .Select(entry => $"{entry.Key}: {entry.Value}")
                                         .ToList();

            HighscoreList.ItemsSource = topScores;
        }
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (NamePanel.Visibility == Visibility.Visible)
        {
            if (NameInput.IsFocused) return;

            e.Handled = true;
            return;
        }

        if (!gameRunning)
        {
            gameRunning = true;
            Overlay.Visibility = Visibility.Hidden;
            await RunGame();
            gameRunning = false;
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (gameState.GameOver) return;

        switch (e.Key)
        {
            case Key.Left: gameState.ChangeDirection(Direction.Left); break;
            case Key.Right: gameState.ChangeDirection(Direction.Right); break;
            case Key.Up: gameState.ChangeDirection(Direction.Up); break;
            case Key.Down: gameState.ChangeDirection(Direction.Down); break;
            case Key.Escape: Application.Current.Shutdown(); break;
            case Key.Space: TogglePauseGame(); break;
        }
    }
    private void TogglePauseGame()
    {
        if (isPaused)
        {
            isPaused = false;
            Overlay.Visibility = Visibility.Collapsed;
            OverlayText.Text = "";
        }
        else
        {

            isPaused = true;
            Overlay.Visibility = Visibility.Visible;
            OverlayText.Text = "Game Paused\n Press any key to continue";
        }
    }


    private async Task GameLoop()
    {
        int delay = 100;
        while (!gameState.GameOver)
        {
            await Task.Delay(delay);
            gameState.Move();
            Draw();

            if (gameState.Score > currentPlayer.Score)
            {
                currentPlayer.Score = gameState.Score;

                delay = Math.Max(20, delay - 1);


                currentPlayer.AddOrUpdateScore(currentPlayer.Name, currentPlayer.Score);
                DisplayHighscores();
            }
        }
    }

    private readonly Dictionary<Direction, int> dirToRotation = new()
    {
        {Direction.Up, 0 },
        { Direction.Right, 90 },
        { Direction.Down, 180 },
        { Direction.Left, 270 }
        };

    private Image[,] SetupGrid()
    {
        Image[,] images = new Image[rows, cols];
        GameGrid.Rows = rows;
        GameGrid.Columns = cols;
        GameGrid.Width = GameGrid.Height * (cols / (double)rows);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Image image = new Image
                {
                    Source = Images.Empty,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };
                images[r, c] = image;
                GameGrid.Children.Add(image);

            }
        }
        return images;
    }
    private void Draw()
    {
        DrawGrid();
        DrawSnakeHead();
        ScoreText.Text = $" {currentPlayer.Name} | Score: {gameState.Score}";
    }

    private void DrawGrid()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                GridValue gridVal = gameState.Grid[r, c];
                gridImages[r, c].Source = gridValToImage[gridVal];
                gridImages[r, c].RenderTransform = Transform.Identity;
            }
        }
    }
    private void DrawSnakeHead()
    {
        Position headPos = gameState.HeadPosition();
        Image image = gridImages[headPos.Row, headPos.Col];
        image.Source = Images.Head;

        int rotation = dirToRotation[gameState.Dir];
        image.RenderTransform = new RotateTransform(rotation);
    }

    private async Task DrawDeadSnake()
    {
        List<Position> positions = new List<Position>(gameState.SnakePositions());
        for (int i = 0; i < positions.Count; i++)
        {
            Position pos = positions[i];
            ImageSource source = (i == 0) ? Images.DeadHead : Images.DeadBody;
            gridImages[pos.Row, pos.Col].Source = source;
            await Task.Delay(50);
        }


    }
    private async Task ShowCountDown()
    {
        for (int i = 3; i >= 1; i--)
        {
            OverlayText.Text = i.ToString();
            await Task.Delay(500);
        }
    }
    private async Task ShowGameOver()
    {
        await DrawDeadSnake();
        await Task.Delay(1000);
        Overlay.Visibility = Visibility.Visible;
        OverlayText.Text = "Press a key for\n  a new game!";
    }

    private async Task RestartGame()
    {
        await DrawDeadSnake();
        await Task.Delay(1000);
        Overlay.Visibility = Visibility.Visible;

        var result = MessageBox.Show($"Do you want to play again?",
    "Play again?",
    MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
            
            BeginGame();
            return;

        }
        {
            NameInput.Clear();
            NamePanel.Visibility = Visibility.Visible; 
            NameInput.Visibility = Visibility.Visible;
            NameInput.Focus();  
        }
    }

}
