using System.Net.Configuration;

namespace Markdown
{
    internal class MarkTagMapping
    {
        public readonly string Mark;
        public readonly string TagName;

        public readonly bool IsOpen;

        public MarkTagMapping(string mark, string tagName, bool isOpen = true)
        {
            Mark = mark;
            TagName = tagName;
            IsOpen = isOpen;
        }

        public string Tag => IsOpen ? OpenTag : CloseTag;
        public string OpenTag => $"<{TagName}>";
        public string CloseTag => $@"<\{TagName}>";

        public bool IsPairWith(MarkTagMapping otherMapping)
        {
            return TagName == otherMapping.TagName && IsOpen == !otherMapping.IsOpen;
        }
    }
}