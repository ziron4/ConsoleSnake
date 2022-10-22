namespace ConsoleSnake
{
    internal class Position
    {
        public int x;
        public int y;
        public Position(int x, int y)
        { 
            this.x = x;
            this.y = y;
        }
    }

    internal class Snake
    {
        public List<Position> positions;
        private int feedCount = 0;
        public bool isSnakeDead = false;
        public Position Head { get { return positions.Last(); } }

        public Snake(int x, int y)
        {
            positions = new() {
                new (x, y)
            };
        }
        public void Move(int relativex, int relativey)
        {
            Position first = positions.First();
            Position last  = positions.Last();
            Position current = new(last.x + relativex, last.y + relativey);

            // Kill the snake if it's current position is within it self.
            if (!isSnakeDead)
            {
                isSnakeDead = positions.Any(p => ((p.x == current.x) && (p.y == current.y)));
            }

            positions.Add(current);

            // Remove if the snake is not grown.
            if (feedCount > 0)
            {
                feedCount--;
            }
            else
            {
                positions.Remove(first);
            }
        }

        public void Kill()
        {
            isSnakeDead = true;
        }

        public void Feed()
        {
            feedCount++;
        }
    }
}
