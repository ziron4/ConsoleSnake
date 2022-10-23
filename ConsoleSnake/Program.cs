using CommandLine;
using System.Numerics;
using System.Runtime.InteropServices;
using uPLibrary.Networking.M2Mqtt;

namespace ConsoleSnake
{
    internal class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }
            [Option('h', "host", Required = false, HelpText = "Specify host to connect to.")]
            public string? Server { get; set; }
            //[Option('p', "port", Required = false, HelpText = "Specify port on host to connect to.")]
            //public int port { get; set; }
        }

        private class Player
        {
            public string clientId;
            public Direction direction;
            public Snake snake;
            public Player (string clientId, Direction direction, int initialX, int initialY)
            {
                this.clientId = clientId;
                this.direction = direction;
                snake = new(initialX, initialY);
            }
        }

        private enum Direction
        {
            None,
            Up,
            Down,
            Left,
            Right
        }

        //private static Direction latestDirection = Direction.Right;
        private static List<Player> players = new();

        private const int initialSnakeX = 10;
        private const int initialSnakeY = 10;
        private const int screenWidth  = 60;
        private const int screenHeight = 30;
        private const int sleepTimeInitial = 200;
        private const int difficultyIncrease = 1;
        private const int yAxisSpeedDeltaMs = 40; // For adjusting y speed in relation to x speed. Example if collum vs row is not same distance.
        private static bool isGameStarted = false;
        private static bool isMultiplayer = false;
        private const string multiplayerPrefix = "ConsoleSnake";
        private static string host = "";
        private static string? clientId;
        private static MqttClient? client;

        static void Main(string[] args)
        {
            Console.Clear();

            Random random = new();

            // ClientId
            clientId = $"{random.NextInt64()}";

            // Multiplayer Settings
            HandleCommandlineArguments(args);

            // Setup multiplayer communication
            if (isMultiplayer)
            {
                Console.Write("Connecting...   ");

                client = new MqttClient(host);
                string[] topics = { "ConsoleSnake/#" };
                byte[] qoss = { 0 };
                client.Subscribe(topics, qoss);
                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
                client.Connect(clientId);
                
                while (!client.IsConnected) { Thread.Sleep(100); };

                Console.WriteLine("Connected!");
            }

            // Setup
            Keyboard keyboard = new(); keyboard.Keypressed += Keyboard_Keypressed;

            bool gameWon = false;
            int sleepTime = sleepTimeInitial;
            int sleepTimeXY_offset = 0;

            if (client is not null)
            {
                // Wait for game to begin
                Console.WriteLine("Waiting for clients to connect");
                while (!isGameStarted)
                {
                    client.Publish($"{multiplayerPrefix}/{clientId}", System.Text.Encoding.UTF8.GetBytes("Connected"));
                    Thread.Sleep(1000);
                }
            }

            Console.Clear();
            Screen screen = new(screenWidth, screenHeight);
            Player yourself = new Player(clientId, Direction.Right, 10, 5 + (int)(Int64.Parse(clientId) % 10));
            players.Add(yourself);
            Fruit fruit = new(random.Next(1, screenWidth - 1), random.Next(1, screenHeight - 1));

            // Main loop
            while (!players.Any(player => player.snake.isSnakeDead) && !gameWon)
            {
                // Choose direction
                foreach (var player in players)
                {
                    switch (player.direction)
                    {
                        case Direction.Left:  player.snake.Move(-1,  0); break;
                        case Direction.Right: player.snake.Move( 1,  0); break;
                        case Direction.Up:    player.snake.Move( 0,  1); sleepTimeXY_offset = yAxisSpeedDeltaMs; break;
                        case Direction.Down:  player.snake.Move( 0, -1); sleepTimeXY_offset = yAxisSpeedDeltaMs; break;
                    }
                }
 
                screen.ClearElements();

                // Draw Fruit.
                screen.WriteAtPosition(fruit.x, fruit.y, 'O');

                // Draw Snake.
                foreach(var player in players)
                {
                    foreach (var position in player.snake.positions)
                    {
                        screen.WriteAtPosition(position.x, position.y, '#');
                    }
                }

                // Snake eat the damn fruit.
                if (   (fruit.x == yourself.snake.Head.x)
                    && (fruit.y == yourself.snake.Head.y))
                {
                    client?.Publish($"{multiplayerPrefix}/{clientId}", System.Text.Encoding.UTF8.GetBytes("Haps"));

                    yourself.snake.Feed();
                    fruit = new(random.Next(1, screenWidth - 1), random.Next(1, screenHeight - 1));
                    sleepTime -= difficultyIncrease;
                }

                // Collission detection
                foreach(var player in players)
                {
                    Snake snake = player.snake;
                    if ((snake.Head.x == 0)
                    || (snake.Head.x == screenWidth)
                    || (snake.Head.y == 0)
                    || (snake.Head.y == screenHeight)
                    )
                    {
                        snake.Kill();
                    }
                }
                
                // Update elements on screen.
                screen.Refresh();

                int sleepTimeTotal = sleepTime + sleepTimeXY_offset;
                Thread.Sleep(Math.Max(sleepTimeTotal, 1));
            }

            if (players.Any(player => player.snake.isSnakeDead))
            {
                ShowEndGame("Game Lost");
            }
            else if (gameWon)
            {
                ShowEndGame("Game Won");
            }
            else
            {
                ShowEndGame("WTF, how did you get here?!");
            }

        }

        private static void Client_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            try
            {
                // If not your self
                string playerClientId = e.Topic.Split('/')[1];
                string cmd = System.Text.Encoding.UTF8.GetString(e.Message);

                Player? player = null;
                // Register player
                if (playerClientId != clientId)
                {
                    if (!players.Any(player => player.clientId == playerClientId))
                    {
                        int initialY = 5 + (int)(Int64.Parse(playerClientId) % 10);
                        player = new(playerClientId, Direction.Right, 10, initialY);
                        players.Add(player);

                        Console.WriteLine($"ClientId: {player.clientId} connected");
                    }
                    else
                    {
                        player = players.First(player => player.clientId == playerClientId);
                    }

                    if (player is not null)
                    {
                        if (player.clientId != clientId)
                        {
                            // Move player
                            if (Enum.TryParse(cmd, out Direction newDirection))
                            {
                                if (!IsOpposite(player.direction, newDirection))
                                {
                                    player.direction = newDirection;
                                }
                            }

                            // Feed his snake
                            if (cmd.Equals("Haps"))
                            {
                                player.snake.Feed();
                            }
                        }
                    }
                }
                
                if (cmd.Equals("Start"))
                {
                    isGameStarted = true;
                }
            }
            catch(Exception)
            {
                // nej
            }
        }

        private static void HandleCommandlineArguments(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.Server is not null)
                       {
                           Console.WriteLine("Multiplayer mode active!");
                           host = o.Server;
                           isMultiplayer = true;
                       }
                   });
        }

        private static void ShowEndGame(string title)
        {
            Console.Clear();
            Console.WriteLine($"##################################");
            Console.WriteLine($"#                                #");
            Console.WriteLine($"#                                #");
            Console.WriteLine($"#           {title}           #");
            Console.WriteLine($"#                                #");
            Console.WriteLine($"#                                #");
            Console.WriteLine($"##################################");
        }

        private static bool IsOpposite(Direction current, Direction newDir)
        {
            return current switch
            {
                Direction.Left => (newDir == Direction.Right),
                Direction.Right => (newDir == Direction.Left),
                Direction.Up => (newDir == Direction.Down),
                Direction.Down => (newDir == Direction.Up),
                _ => false,
            };
        }

        private static void Keyboard_Keypressed(ConsoleKey key)
        {
            static Direction keyToDirection(ConsoleKey key)
            {
                return key switch
                {
                    ConsoleKey.LeftArrow  => Direction.Left,
                    ConsoleKey.RightArrow => Direction.Right,
                    ConsoleKey.UpArrow    => Direction.Up,
                    ConsoleKey.DownArrow  => Direction.Down,
                    _ => Direction.None,
                };
            }

            if (key == ConsoleKey.Enter)
            {
                if (client is not null)
                {
                    client.Publish($"{multiplayerPrefix}/{clientId}", System.Text.Encoding.UTF8.GetBytes("Start"));
                }
            }

            Direction direction = keyToDirection(key);
            
            // Make sure we only use specific keys
            if (direction != Direction.None)
            {
                Player yourself = players.First(player => player.clientId == clientId);
                if (!IsOpposite(yourself.direction, direction))
                {
                    yourself.direction = direction;

                    if (client is not null && client.IsConnected)
                    {
                        client.Publish($"{multiplayerPrefix}/{clientId}", System.Text.Encoding.UTF8.GetBytes($"{direction}"));
                    }
                }
            }
        }
    }
}