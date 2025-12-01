using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace MauiGame
{
    public partial class MainPage : ContentPage
    {
        const int Size = 4; 
        int AttemptCounter = 0; 
        Label[,] _tiles = new Label[Size, Size]; 
        int[,] _board = new int[Size, Size]; 
        Stack<int[,]> _history = new Stack<int[,]>(); 
        Stack<int> _Shistory = new Stack<int>();
        int _score = 0;
        int _highScore = Preferences.Get("LastHighScore", 0);
        private readonly Random _random = new Random();


        public MainPage()
        {
            InitializeComponent();
            InitializeGame();
            StartNewGame();
        }

        void InitializeGame()
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    var label = new Label
                    {
                        BackgroundColor = Colors.LightGray,
                        TextColor = Colors.Black,
                        FontSize = 24,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        Margin = 2
                    };

                    _tiles[i, j] = label;
                    GameGrid.Add(label);
                    Grid.SetRow(label, i);
                    Grid.SetColumn(label, j);
                }
            }
        }

        void StartNewGame()
        {
            _score = 0;
            AttemptCounter = 0;
            ScoreLabel.Text = $"Счёт: {_score}";
            Array.Clear(_board, 0, _board.Length);
            AddRandomTile();
            AddRandomTile();
            UpdateUI();
        }

        void AddRandomTile()
        {
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(300);

                var emptyCells = new List<(int, int)>();
                for (int i = 0; i < Size; i++)
                {
                    for (int j = 0; j < Size; j++)
                    {
                        if (_board[i, j] == 0)
                        {
                            emptyCells.Add((i, j));
                        }
                    }
                }

                if (emptyCells.Count > 0)
                {
                    var randomCell = emptyCells[_random.Next(emptyCells.Count)];
                    _board[randomCell.Item1, randomCell.Item2] = _random.Next(10) == 0 ? 4 : 2;
                }
            });
        }


        void UpdateUI()
        {

            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    _tiles[i, j].Text = _board[i, j] == 0 ? "" : _board[i, j].ToString();
                    _tiles[i, j].BackgroundColor = GetTileColor(_board[i, j]);
                }
            }
            AttemptLabel.Text = $"Ходов: {AttemptCounter}";
            ScoreLabel.Text = $"Счёт: {_score}";
            HighScoreLabel.Text = $"Лучший: {_highScore}";
        }

        Color GetTileColor(int value)
        {
            switch (value)
            {


                case 0: return Colors.LightPink;
                case 2: return Colors.Beige;
                case 4: return Colors.Bisque;
                case 8: return Colors.BurlyWood;
                case 16: return Colors.Coral;
                case 32: return Colors.Orange;
                case 64: return Colors.OrangeRed;
                case 128: return Colors.Gold;
                case 256: return Colors.Yellow;
                case 512: return Colors.YellowGreen;
                case 1024: return Colors.LightGreen;
                case 2048: return Colors.Lime;
                default: return Colors.Black;
            }
        }


        async Task AnimateTiles(View fromTile, View toTile)
        {
                int i = 0;
                toTile.ZIndex = i;
                await toTile.ScaleTo(0.8, length: 200);
                toTile.ZIndex = i + 1;
                await fromTile.FadeTo(0.2, length: 100);
                await toTile.ScaleTo(1, length: 200);
                await fromTile.FadeTo(1, length: 100);
        }

        public static async Task ParallelAnimations(IEnumerable<Task> animations)
        {
            await Task.WhenAll(animations.ToArray());
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.Content.GestureRecognizers.Add(new SwipeGestureRecognizer
            {
                Direction = SwipeDirection.Left,
                Command = new Command(OnSwipeLeft)
            });
            this.Content.GestureRecognizers.Add(new SwipeGestureRecognizer
            {
                Direction = SwipeDirection.Right,
                Command = new Command(OnSwipeRight)
            });
            this.Content.GestureRecognizers.Add(new SwipeGestureRecognizer
            {
                Direction = SwipeDirection.Up,
                Command = new Command(OnSwipeUp)
            });
            this.Content.GestureRecognizers.Add(new SwipeGestureRecognizer
            {
                Direction = SwipeDirection.Down,
                Command = new Command(OnSwipeDown)
            });
        }

        void OnSwipeLeft() => MoveAndMergeTiles((i, j) => (i, j), (i, j) => (i, j - 1));
        void OnSwipeRight() => MoveAndMergeTiles((i, j) => (i, j), (i, j) => (i, j + 1));
        void OnSwipeUp() => MoveAndMergeTiles((i, j) => (i, j), (i, j) => (i - 1, j));
        void OnSwipeDown() => MoveAndMergeTiles((i, j) => (i, j), (i, j) => (i + 1, j));

        async void MoveAndMergeTiles(Func<int, int, (int, int)> current, Func<int, int, (int, int)> next)
        {
            List<Task> animationTasks = new List<Task>();

            _Shistory.Push(_score);
            _history.Push((int[,])_board.Clone());
            bool moved = false;

            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    var (row, col) = current(i, j);
                    if (_board[row, col] != 0)
                    {
                        var (nextRow, nextCol) = next(row, col);

                        while (IsInBounds(nextRow, nextCol) && _board[nextRow, nextCol] == 0)
                        {
                            _board[nextRow, nextCol] = _board[row, col];
                            _board[row, col] = 0;

                            row = nextRow;
                            col = nextCol;
                            (nextRow, nextCol) = next(row, col);


                            moved = true;
                        }

                        if (IsInBounds(nextRow, nextCol) && _board[nextRow, nextCol] == _board[row, col])
                        {
                            _board[nextRow, nextCol] *= 2;
                            _board[row, col] = 0;
                            _score += _board[nextRow, nextCol];
                            if (_score > _highScore)
                            {
                                _highScore = _score;
                                Preferences.Set("LastHighScore", _highScore);
                            }

                            if (_board[nextRow,nextCol] != 0)
                            {
                                animationTasks.Add(AnimateTiles(_tiles[row, col], _tiles[nextRow, nextCol]));
                            }


                            if (_board[nextRow,nextCol] == 2048)
                            {
                               await DisplayAlert(
                                   title: "Победа",
                                   message: "Вы справились!",
                                   cancel: "Ура-ура");

                                await Task.Delay(2000);

                                StartNewGame();
                            }

                            moved = true;
                        }
                    }
                }
            }

            if (moved)
            {
                AttemptCounter++;
                AddRandomTile();
                UpdateUI();
            }
        }

        bool IsInBounds(int row, int col)
        {
            if (row >= 0 && row < Size && col >= 0 && col < Size)
                return true;
            else
                return false;
        }

        void OnUndoClicked(object sender, EventArgs e)
        {
            if (_history.Count > 0)
            {
                _board = _history.Pop();
                _score = _Shistory.Pop();
                AttemptCounter--;
                UpdateUI();
            }
            else 
            {
                DisplayAlert(
                    title: "Упс",
                    message: "Сперва сделайте ход!",
                    cancel: "OK");
            }
        }

        void OnRestartClicked(object sender, EventArgs e)
        {
            StartNewGame();
        }
    }
}