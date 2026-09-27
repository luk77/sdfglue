namespace SingleDocAppFramework.Ui
{
    // Window position and size in pixels
    public struct WindowRect
    {
        public int PosX;
        public int PosY;
        public int SizeX;
        public int SizeY;

        public WindowRect(int posX, int posY, int sizeX, int sizeY)
        {
            PosX    = posX;
            PosY    = posY;
            SizeX   = sizeX;
            SizeY   = sizeY;
        }
    }
}
