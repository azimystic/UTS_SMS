using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UTS_SMS.Models;
using System.Text;

namespace UTS_SMS.Services
{
    /// <summary>
    /// Extracts text from uploaded PDF academic materials, chunks it,
    /// generates embeddings via Gemini, and stores them in ChromaDB.
    /// </summary>
    public class PdfIngestionService
    {
        private readonly ApplicationDbContext _context;
        private readonly VectorStoreService _vectorStore;
        private readonly AiChatOptions _options;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<PdfIngestionService> _logger;

        public PdfIngestionService(
            ApplicationDbContext context,
            VectorStoreService vectorStore,
            IOptions<AiChatOptions> options,
            IWebHostEnvironment env,
            ILogger<PdfIngestionService> logger)
        {
            _context = context;
            _vectorStore = vectorStore;
            _options = options.Value;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Ingest all active PDF materials that haven't been indexed yet.
        /// Returns (processed, failed, skipped) counts.
        /// </summary>
        public async Task<(int processed, int failed, int skipped)> IngestAllPdfsAsync(
            Action<string>? onProgress = null)
        {
            var pdfMaterials = await _context.ChapterMaterials
                .Include(m => m.Chapter)
                    .ThenInclude(c => c.Subject)
                .Where(m => m.Type == MaterialType.PDF && m.IsActive)
                .ToListAsync();

            int processed = 0, failed = 0, skipped = 0;

            foreach (var material in pdfMaterials)
            {
                try
                {
                    var filePath = Path.Combine(_env.WebRootPath, material.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("PDF file not found: {FilePath}", filePath);
                        onProgress?.Invoke($"⚠️ Skipped (file not found): {material.OriginalFileName}");
                        skipped++;
                        continue;
                    }

                    // Check if already indexed
                    bool alreadyIndexed = await _vectorStore.IsDocumentIndexedAsync(material.Id);
                    if (alreadyIndexed)
                    {
                        onProgress?.Invoke($"⏭️ Already indexed: {material.OriginalFileName}");
                        skipped++;
                        continue;
                    }

                    onProgress?.Invoke($"📄 Extracting text from: {material.OriginalFileName}...");

                    // Step 1: Extract text from PDF
                    var pages = ExtractTextFromPdf(filePath);

                    // Step 2: Chunk the text
                    var chunks = ChunkPages(pages, material);

                    if (chunks.Count == 0)
                    {
                        onProgress?.Invoke($"⚠️ No text extracted from: {material.OriginalFileName}");
                        skipped++;
                        continue;
                    }

                    onProgress?.Invoke($"🔢 Embedding {chunks.Count} chunks from: {material.OriginalFileName}...");

                    // Step 3: Embed and store in ChromaDB
                    await _vectorStore.StoreChunksAsync(chunks);

                    onProgress?.Invoke($"✅ Indexed: {material.OriginalFileName} ({chunks.Count} chunks)");
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest PDF: {MaterialId} - {FileName}", material.Id, material.OriginalFileName);
                    onProgress?.Invoke($"❌ Failed: {material.OriginalFileName} - {ex.Message}");
                    failed++;
                }
            }

            return (processed, failed, skipped);
        }

        /// <summary>
        /// Ingest a single PDF material by ID.
        /// </summary>
        public async Task IngestSinglePdfAsync(int chapterMaterialId, Action<string>? onProgress = null)
        {
            var material = await _context.ChapterMaterials
                .Include(m => m.Chapter)
                    .ThenInclude(c => c.Subject)
                .FirstOrDefaultAsync(m => m.Id == chapterMaterialId && m.Type == MaterialType.PDF);

            if (material == null)
                throw new ArgumentException($"PDF material with ID {chapterMaterialId} not found.");

            var filePath = Path.Combine(_env.WebRootPath, material.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"PDF file not found at: {filePath}");

            // Remove old chunks if re-indexing
            await _vectorStore.DeleteDocumentChunksAsync(material.Id);

            onProgress?.Invoke($"📄 Extracting text from: {material.OriginalFileName}...");
            var pages = ExtractTextFromPdf(filePath);
            var chunks = ChunkPages(pages, material);

            onProgress?.Invoke($"🔢 Embedding {chunks.Count} chunks...");
            await _vectorStore.StoreChunksAsync(chunks);

            onProgress?.Invoke($"✅ Done: {chunks.Count} chunks indexed.");
        }

        /// <summary>
        /// Uses iText7 to extract text from each page of a PDF.
        /// Returns a dictionary of pageNumber -> extractedText.
        /// </summary>
        private Dictionary<int, string> ExtractTextFromPdf(string filePath)
        {
            var pages = new Dictionary<int, string>();

            using var reader = new PdfReader(filePath);
            using var pdfDocument = new PdfDocument(reader);

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, strategy);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    pages[i] = text.Trim();
                }
            }

            return pages;
        }

        /// <summary>
        /// Splits extracted page text into overlapping chunks of configurable size.
        /// Each chunk preserves metadata about its source material.
        /// </summary>
        private List<DocumentChunk> ChunkPages(Dictionary<int, string> pages, ChapterMaterial material)
        {
            var chunks = new List<DocumentChunk>();
            int chunkIndex = 0;

            foreach (var (pageNumber, pageText) in pages)
            {
                int start = 0;
                while (start < pageText.Length)
                {
                    int length = Math.Min(_options.ChunkSize, pageText.Length - start);
                    var chunkText = pageText.Substring(start, length);

                    // Try to break at a sentence boundary if not at the end
                    if (start + length < pageText.Length)
                    {
                        var lastPeriod = chunkText.LastIndexOf('.');
                        var lastNewline = chunkText.LastIndexOf('\n');
                        var breakPoint = Math.Max(lastPeriod, lastNewline);

                        if (breakPoint > _options.ChunkSize / 2)
                        {
                            chunkText = chunkText.Substring(0, breakPoint + 1);
                            length = breakPoint + 1;
                        }
                    }

                    chunks.Add(new DocumentChunk
                    {
                        Id = $"mat_{material.Id}_p{pageNumber}_c{chunkIndex}",
                        ChapterMaterialId = material.Id,
                        SubjectName = material.Chapter?.Subject?.Name ?? "Unknown",
                        ChapterName = material.Chapter?.Name ?? "Unknown",
                        OriginalFileName = material.OriginalFileName ?? "Unknown",
                        FilePath = material.FilePath,
                        PageNumber = pageNumber,
                        ChunkIndex = chunkIndex,
                        Text = chunkText.Trim()
                    });

                    chunkIndex++;
                    start += length - _options.ChunkOverlap;
                    if (start < 0) start = 0;
                }
            }

            return chunks;
        }
    }
}
