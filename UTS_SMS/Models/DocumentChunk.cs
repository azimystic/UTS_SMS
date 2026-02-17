namespace UTS_SMS.Models
{
    /// <summary>
    /// Represents a chunk of text extracted from a PDF, stored in the vector database.
    /// </summary>
    public class DocumentChunk
    {
        public string Id { get; set; } = string.Empty;
        public int ChapterMaterialId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int ChunkIndex { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
