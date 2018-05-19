namespace SimpleBlockchain
{
    public class Block
    {
        public int Index { get; set; }

        public long TimeStamp { get; set; }

        // ReSharper disable once InconsistentNaming
        public int BPM { get; set; }

        public string Hash { get; set; }

        public string PrevHash { get; set; }

        public int Difficulty { get; set; }

        public string Nonce { get; set; }
    }
}