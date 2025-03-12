namespace RenPyReader.DataModels
{
    public class RenPySearchResult(string title, string content, int line, long parentId)
    {
        public string Title { get; set; } = title;

        public string Content { get; set; } = content;

        public int Line { get; set; } = line;

        public long ParentId { get; set; } = parentId;

        public override string ToString() => $"{Title} | {Content.Length} | {Line} | {ParentId}";
    }
}
