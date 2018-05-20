namespace Picture
{
	unsafe public struct Component
    {
        public int cid;
        public int ssx;
        public int ssy;
        public int width;
        public int height;
        public int stride;
        public int qtsel;
        public int actabsel;
        public int dctabsel;
        public int dcpred;
        public byte* pixels;
    }
}
