using UnityEngine;

namespace ThirdLamp
{
    public enum DocStyle { Handwritten, Typed, Letter, Newspaper, Postcard }

    /// <summary>A readable document. Important occult documents raise perception once.</summary>
    public class NoteReader : Interactable
    {
        public string verb = "Read";
        public string title = "note";
        [TextArea(5, 20)] public string body;
        public DocStyle style = DocStyle.Handwritten;
        public int perception;
        public string flagOnRead;

        public override string Prompt => $"{verb} {title}";

        public override void Interact()
        {
            Game.Audio.PlayAt("paper", transform.position, 0.5f, Random.Range(0.95f, 1.1f));
            Game.Hud.ShowDocument(this);
            if (!string.IsNullOrEmpty(flagOnRead)) Game.State.Set(flagOnRead);
            if (perception > 0) Game.Perception.Add(perception, "doc_" + (flagOnRead ?? title));
        }
    }
}
