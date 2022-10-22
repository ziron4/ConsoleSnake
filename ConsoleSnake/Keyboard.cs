namespace ConsoleSnake
{
    internal class Keyboard
    {
        private bool stop = false;
        public delegate void InputEvent(ConsoleKey key);  // delegate
        public event InputEvent? Keypressed; // event

        public Keyboard()
        {
            Thread t = new(() => {
                while(!stop)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    Keypressed?.Invoke(key.Key);
                }
            }) { IsBackground = true, Name="Keyboard listener" };
            t.Start();
        }

        ~Keyboard()
        {
            // The thread first stops with next userinput...
            // I really don't want to abort the thread...
            stop = true;
        }
    }
}
