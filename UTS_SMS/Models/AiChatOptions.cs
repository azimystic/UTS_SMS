namespace UTS_SMS.Models
{
    public class AiChatOptions
    {
        public string GeminiApiKey { get; set; } = string.Empty;
        public string GeminiModel { get; set; } = "gemini-2.0-flash";
        public string EmbeddingModel { get; set; } = "text-embedding-004";
        public int EmbeddingDimensions { get; set; } = 768;
        public string ChromaDbUrl { get; set; } = "http://localhost:8000";
        public string ChromaCollectionName { get; set; } = "school_materials";
        public int ChunkSize { get; set; } = 1000;
        public int ChunkOverlap { get; set; } = 200;
        public int MaxSearchResults { get; set; } = 5;
        public string GroqApiKey { get; set; } = string.Empty;
        public string GroqApiUrl { get; set; } = "https://api.groq.com/openai/v1";
        public List<string> GroqModels { get; set; } = new List<string>();
    }
}
