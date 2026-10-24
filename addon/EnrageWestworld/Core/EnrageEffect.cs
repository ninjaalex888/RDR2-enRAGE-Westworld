namespace EnrageWestworld.Core
{
    /// A change for enRAGE's own records (thought, memory, history, hate, marry, like), applied by the game layer.
    public sealed class EnrageEffect
    {
        public string PersonId;
        public string Kind;
        public string Mood;
        public string Emotion;
        public string Text;
        public int Amount;
        public int Hours;
        public string Cause;
        public string OtherId;
        public string OtherName;
        public bool Romance = true;   // false: from the 0.4 world features (gated by their own settings)

        public override string ToString()
        {
            return Kind + " " + PersonId + " " + (Mood ?? Emotion ?? "") + " " + Amount + " " + Text + (OtherName != null ? " (" + OtherName + ")" : "");
        }
    }

    /// Something the game layer must do in the world for the engine (0.4).
    public sealed class WorldRequest
    {
        public string Kind;      // revive, avenger, paper, gooddeed, notice
        public string PersonId;
        public string Text;
        public string Town;
        public Reverie Reverie;
        public Avenger Avenger;

        public override string ToString()
        {
            return Kind + " " + (PersonId ?? "") + " " + (Text ?? "") + (Town != null ? " @" + Town : "");
        }
    }
}
