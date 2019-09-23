namespace KachingPlugIn.Models
{
    public class KachingTag
    {
        public string Tag { get; set;  }
        public L10nString Name { get; set; }

        public override string ToString()
        {
            return Tag;
        }
    }
}
