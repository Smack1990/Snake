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
        // Retrieve the player name from the TextBox
        string playerName = NameInput.Text.Trim();
        DisplayHighscores();


        if (string.IsNullOrEmpty(playerName))
        {
            // Show a message if the name is empty and prevent starting the game
            MessageBox.Show("Please enter your name to start the game.");
            return;
        }

        // Initialize currentPlayer with the entered name and a starting score of 0
        currentPlayer = new Player(playerName, 0);

        // Read high scores from JSON after currentPlayer is initialized
        

        // Hide the name entry panel and show the overlay with start prompt
        NamePanel.Visibility = Visibility.Hidden;
        Overlay.Visibility = Visibility.Visible;
        OverlayText.Text = "Press any key to start"; // Update overlay text

        // Set focus back to the Window to capture key events
        this.Focus();

        gameRunning = false; // Allow game to start on key press
    }

    private async Task RunGame()
    {
        DisplayHighscores();
        Draw();
        await ShowCountDown();
        Overlay.Visibility = Visibility.Hidden;
        await GameLoop();
        await ShowGameOver();

        // Save the current player's score to the high scores list
        if (currentPlayer != null) // Double-check that currentPlayer is not null
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
                                         .Take(5) // Limit to top 5 entries
                                         .Select(entry => $"{entry.Key}: {entry.Value}")
                                         .ToList();

            HighscoreList.ItemsSource = topScores; // Now this will work within MainWindow
        }
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (NamePanel.Visibility == Visibility.Visible)
        {
            if (NameInput.IsFocused) return;  // Allow key input to the TextBox

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
        }
    }

    private async Task GameLoop()
    {
        int delay = 120;
        while (!gameState.GameOver)
        {
            await Task.Delay(delay);
            gameState.Move();
            Draw();

            if (gameState.Score > currentPlayer.Score)
            {
                // Update current player's score
                currentPlayer.Score = gameState.Score;

                // Adjust delay to increase speed gradually
                delay = Math.Max(20, delay - 3);

                // Update high score list
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
        GameGrid.Rows =rows;
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
        ScoreText.Text = $"SCORE: {gameState.Score}";
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
        OverlayText.Text = "PRESS ANY KEY TO START A NEW GAME";
    }


  
}
