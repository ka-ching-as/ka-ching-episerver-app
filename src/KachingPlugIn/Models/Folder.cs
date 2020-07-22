using System.Collections.Generic;

namespace KachingPlugIn.Models
{
    public class Folder
    {
        public Filter Filter { get; set; }
        public List<Folder> Children { get; set; }

        public Folder(string tag)
        {
            this.Filter = new Filter();
            this.Filter.Tag = tag;
        }

        public override string ToString()
        {
            return Description(0);
        }

        public int CountNodesInTree()
        {
            var result = 1;
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    result += child.CountNodesInTree();
                }
            }
            return result;
        }

        private string Description(int depth)
        {
            var result = new string('-', depth) + Filter.ToString();
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    result += "\n";
                    result += child.Description(depth + 1);
                }
            }
            return result;
        }
    }

    public class Filter
    {
        public string Tag { get; set; }

        public override string ToString()
        {
            return Tag;
        }
    }
}
