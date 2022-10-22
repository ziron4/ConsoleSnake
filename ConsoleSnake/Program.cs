namespace ConsoleSnake
{
    internal class Program
    {
        private enum Direction
        {
            None,
            Up,
            Down,
            Left,
            Right
        }

        static Direction latestDirection = Direction.Right;
        const int initialSnakeX = 10;
        const int initialSnakeY = 10;
        const int screenWidth  = 60;
        const int screenHeight = 20;
        const int sleepTimeInitial = 100;
        const int difficultyIncrease = 1;
        const int yAxisSpeedDeltaMs = 40; // For adjusting y speed in relation to x speed. Example if collum vs row is not same distance.

        static void Main()
        {
            // Setup
            Console.Clear();
            Random random = new();
            Screen screen = new(screenWidth, screenHeight);
            Snake  snake  = new(initialSnakeX, initialSnakeY);
            Fruit  fruit  = new(random.Next(1, screenWidth - 1), random.Next(1, screenHeight - 1));
            Keyboard keyboard = new(); keyboard.Keypressed += Keyboard_Keypressed;

            bool gameWon = false;
            int sleepTime = sleepTimeInitial;
            int sleepTimeXY_offset = 0;

            // Main loop
            while (!snake.isSnakeDead && !gameWon)
            {
                // Choose direction
                switch (latestDirection)
                {
                    case Direction.Left:  snake.Move(-1,  0); break;
                    case Direction.Right: snake.Move( 1,  0); break;
                    case Direction.Up:    snake.Move( 0,  1); sleepTimeXY_offset = yAxisSpeedDeltaMs; break;
                    case Direction.Down:  snake.Move( 0, -1); sleepTimeXY_offset = yAxisSpeedDeltaMs; break;
                }
 
                screen.ClearElements();

                // Draw Fruit.
                screen.WriteAtPosition(fruit.x, fruit.y, 'O');

                // Draw Snake.
                foreach (var position in snake.positions)
                {
                    screen.WriteAtPosition(position.x, position.y, '#');
                }

                // Snake eat the damn fruit.
                if (   (fruit.x == snake.Head.x)
                    && (fruit.y == snake.Head.y))
                {
                    snake.Feed();
                    fruit = new(random.Next(1, screenWidth-1), random.Next(1, screenHeight-1));
                    sleepTime -= difficultyIncrease;
                }

                // Collission detection
                if (   (snake.Head.x == 0)
                    || (snake.Head.x == screenWidth)
                    || (snake.Head.y == 0)
                    || (snake.Head.y == screenHeight)
                    )
                {
                    snake.Kill();
                }

                // Update elements on screen.
                screen.Refresh();

                int sleepTimeTotal = sleepTime + sleepTimeXY_offset;
                Thread.Sleep(Math.Max(sleepTimeTotal, 1));
            }

            if (snake.isSnakeDead)
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


        private static void Keyboard_Keypressed(ConsoleKey key)
        {
            static bool isOpposite(Direction current, Direction newDir)
            {
                return current switch
                {
                    Direction.Left  => (newDir == Direction.Right),
                    Direction.Right => (newDir == Direction.Left),
                    Direction.Up    => (newDir == Direction.Down),
                    Direction.Down  => (newDir == Direction.Up),
                    _ => false,
                };
            }
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

            Direction direction = keyToDirection(key);
            
            // Make sure we only use specific keys
            if (direction != Direction.None)
            {
                if (!isOpposite(latestDirection, direction))
                {
                    latestDirection = direction;
                }
            }
        }
    }
}