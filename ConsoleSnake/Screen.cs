namespace ConsoleSnake
{
    internal class Screen
    {
        public int Width { get; }
        public int Height { get; }
        private readonly List<Element> waiting_to_be_drawn_elements;
        private readonly List<Element> drawn_elements;
        private readonly List<Element> cleared_elements;

        private class Element 
        { 
            public int X { set; get; } 
            public int Y { set; get; } 
            public char Character { set; get; } 
            public Element(int x, int y, char character)
            {
                this.X = x;
                this.Y = y;
                this.Character = character;
            }
        }

        public Screen(int width, int height)
        {
            Width = width;
            Height = height;
            Console.CursorVisible = false;
            drawn_elements = new();
            cleared_elements = new();
            waiting_to_be_drawn_elements = new();

            for (int x = 1; x < Width; x++)
            {
                WriteAtPosition_dont_add_as_drawn(x, 0,      '-');
                WriteAtPosition_dont_add_as_drawn(x, Height, '-');
            }
            for (int y = 1; y < Height; y++)
            {
                WriteAtPosition_dont_add_as_drawn(0,     y, '|');
                WriteAtPosition_dont_add_as_drawn(Width, y, '|');
            }
            // Corners
            WriteAtPosition_dont_add_as_drawn(0,          0, '#');
            WriteAtPosition_dont_add_as_drawn(0,     Height, '#');
            WriteAtPosition_dont_add_as_drawn(Width,      0, '#');
            WriteAtPosition_dont_add_as_drawn(Width, Height, '#');

        }

        public void WriteAtPosition(int x, int y, char item)
        {
            waiting_to_be_drawn_elements.Add(new Element(x, y, item));
        }

        private void WriteAtPosition_dont_add_as_drawn(int x, int y, char item)
        {
            Console.SetCursorPosition(ConvertX(x), ConvertY(y));
            Console.Write(item);
        }

        public void ClearElements()
        {
            foreach (var item in drawn_elements)
            {
                cleared_elements.Add(item);
            }
            drawn_elements.Clear();
        }
        public void Refresh()
        {
            foreach (var element in cleared_elements)
            {
                Console.SetCursorPosition(ConvertX(element.X), ConvertY(element.Y));
                if (!waiting_to_be_drawn_elements.Contains(element))
                {
                    Console.Write(' ');
                }
            }

            foreach (var element in waiting_to_be_drawn_elements)
            {
                Console.SetCursorPosition(ConvertX(element.X), ConvertY(element.Y));
                if (!drawn_elements.Contains(element))
                {
                    Console.Write(element.Character);
                    drawn_elements.Add(element);
                }
            }

            waiting_to_be_drawn_elements.Clear();
            cleared_elements.Clear();
        }

        /// <summary>
        /// Converts normal x value to screen position
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int ConvertX(int x)
        {
            return x;
        }

        /// <summary>
        /// Converts normal y value to screen position
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        private int ConvertY(int y)
        {
            return Height - y;
        }

    }
}
