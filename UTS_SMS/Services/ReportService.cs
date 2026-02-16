using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Diagnostics;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;

namespace SMS.Services
{
    public class ReportService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IWebHostEnvironment environment, ILogger<ReportService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Generates a PDF report from a .docx template by replacing placeholders
        /// </summary>
        /// <param name="templateFileName">Name of the template file in wwwroot/reports</param>
        /// <param name="placeholders">Dictionary of placeholders and their values</param>
        /// <returns>Byte array of the generated PDF</returns>
        public async Task<byte[]> GeneratePdfFromTemplate(string templateFileName, Dictionary<string, string> placeholders)
        {
            try
            {
                // Validate template exists
                var templatePath = Path.Combine(_environment.WebRootPath, "reports", templateFileName);
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template file not found: {templateFileName}");
                }

                // Create temporary directory for processing
                var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);

                try
                {
                    // Copy template to temp location
                    var tempDocxPath = Path.Combine(tempDirectory, "temp.docx");
                    File.Copy(templatePath, tempDocxPath, true);

                    // Replace placeholders in the document
                    ReplacePlaceholders(tempDocxPath, placeholders);

                    // Convert to PDF using LibreOffice
                    var pdfPath = await ConvertToPdf(tempDocxPath, tempDirectory);

                    // Read PDF into byte array
                    var pdfBytes = await File.ReadAllBytesAsync(pdfPath);

                    return pdfBytes;
                }
                finally
                {
                    // Cleanup temporary files
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to delete temporary directory: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF from template");
                throw;
            }
        }

        /// <summary>
        /// Replaces placeholders in a .docx file
        /// </summary>
        private void ReplacePlaceholders(string docxPath, Dictionary<string, string> placeholders)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(docxPath, true))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body == null)
                {
                    throw new InvalidOperationException("Document body is null");
                }

                // Get all text elements
                var texts = body.Descendants<Text>().ToList();

                foreach (var text in texts)
                {
                    foreach (var placeholder in placeholders)
                    {
                        var key = "{" + placeholder.Key + "}";
                        if (text.Text.Contains(key))
                        {
                            text.Text = text.Text.Replace(key, placeholder.Value ?? string.Empty);
                        }
                    }
                }

                // Also handle table cells for tabular data
                var tableCells = body.Descendants<TableCell>().ToList();
                foreach (var cell in tableCells)
                {
                    var cellTexts = cell.Descendants<Text>().ToList();
                    foreach (var text in cellTexts)
                    {
                        foreach (var placeholder in placeholders)
                        {
                            var key = "{" + placeholder.Key + "}";
                            if (text.Text.Contains(key))
                            {
                                text.Text = text.Text.Replace(key, placeholder.Value ?? string.Empty);
                            }
                        }
                    }
                }

                wordDoc.MainDocumentPart.Document.Save();
            }
        }

        /// <summary>
        /// Converts a .docx file to PDF using LibreOffice
        /// </summary>
        private async Task<string> ConvertToPdf(string docxPath, string outputDirectory)
        {
            // Determine LibreOffice command based on OS
            string libreOfficeCommand = "libreoffice"; // Default for Linux/Mac
            if (OperatingSystem.IsWindows())
            {
                libreOfficeCommand = "soffice"; // Windows typically uses soffice
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = libreOfficeCommand,
                Arguments = $"--headless --convert-to pdf --outdir \"{outputDirectory}\" \"{docxPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    // Start reading streams immediately
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // Wait for the tasks and the process exit
                    await Task.WhenAll(outputTask, errorTask);

                    // It's good practice to use a CancellationToken or Timeout here 
                    // so your app doesn't hang forever if LibreOffice freezes.
                    await process.WaitForExitAsync();

                    var output = outputTask.Result;
                    var error = errorTask.Result;

                    if (process.ExitCode != 0)
                    {
                        _logger.LogError("LibreOffice conversion failed. Exit code: {ExitCode}", process.ExitCode);
                        _logger.LogError("STDOUT: {Output}", output);
                        _logger.LogError("STDERR: {Error}", error);

                        throw new InvalidOperationException($"LibreOffice returned error code {process.ExitCode}: {error}");
                    }

                    var pdfFileName = Path.GetFileNameWithoutExtension(docxPath) + ".pdf";
                    var pdfPath = Path.Combine(outputDirectory, pdfFileName);

                    if (!File.Exists(pdfPath))
                    {
                        throw new FileNotFoundException($"Conversion reported success, but file missing: {pdfPath}");
                    }

                    return pdfPath;
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                _logger.LogError(ex, "Failed to start the process. Is LibreOffice installed and the path correct?");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during PDF conversion.");
                throw;
            }
        }

        /// <summary>
        /// Generates multiple PDFs and merges them or creates a zip file
        /// </summary>
        public async Task<byte[]> GenerateBulkPdfs(string templateFileName, List<Dictionary<string, string>> placeholdersList, bool asZip = true)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var pdfDirectory = Path.Combine(tempDirectory, "pdfs");
            Directory.CreateDirectory(pdfDirectory);

            try
            {
                var pdfFiles = new List<string>();

                for (int i = 0; i < placeholdersList.Count; i++)
                {
                    var pdfBytes = await GeneratePdfFromTemplate(templateFileName, placeholdersList[i]);
                    
                    // Generate descriptive filename from placeholders if available
                    var filename = $"report_{i + 1}.pdf";
                    if (placeholdersList[i].TryGetValue("student_name", out var studentName) && !string.IsNullOrWhiteSpace(studentName))
                    {
                        // Sanitize filename to remove invalid characters
                        var sanitizedName = string.Join("_", studentName.Split(Path.GetInvalidFileNameChars()));
                        
                        if (placeholdersList[i].TryGetValue("roll_number", out var rollNumber) && !string.IsNullOrWhiteSpace(rollNumber))
                        {
                            filename = $"{sanitizedName}_{rollNumber}.pdf";
                        }
                        else
                        {
                            filename = $"{sanitizedName}.pdf";
                        }
                    }
                    
                    var pdfPath = Path.Combine(pdfDirectory, filename);
                    await File.WriteAllBytesAsync(pdfPath, pdfBytes);
                    pdfFiles.Add(pdfPath);
                }

                if (asZip)
                {
                    // Create a zip file containing all PDFs
                    var zipPath = Path.Combine(tempDirectory, "reports.zip");
                    System.IO.Compression.ZipFile.CreateFromDirectory(pdfDirectory, zipPath);
                    return await File.ReadAllBytesAsync(zipPath);
                }
                else
                {
                    // For now, return the first PDF (merging would require additional library)
                    // In production, you might want to use a library like iTextSharp for PDF merging
                    return await File.ReadAllBytesAsync(pdfFiles[0]);
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete temporary directory: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Generates multiple PDFs with dynamic rows and creates either a ZIP file or a single merged PDF
        /// </summary>
        /// <param name="templateFileName">Name of the template file</param>
        /// <param name="studentReportDataList">List of tuples containing common placeholders and subject row data for each student</param>
        /// <param name="templateRowPlaceholders">Placeholders in template row</param>
        /// <param name="asZip">Whether to return as ZIP file (true) or merged PDF (false)</param>
        /// <returns>Byte array of ZIP file or single merged PDF</returns>
        public async Task<byte[]> GenerateBulkPdfsWithDynamicRows(
            string templateFileName,
            List<(Dictionary<string, string> commonPlaceholders, List<Dictionary<string, string>> rowData)> studentReportDataList,
            List<string> templateRowPlaceholders,
            bool asZip = true)
        {
            // Create a unique temporary directory for this operation
            var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var pdfDirectory = Path.Combine(tempDirectory, "pdfs");
            Directory.CreateDirectory(pdfDirectory);

            try
            {
                var pdfFiles = new List<string>();

                for (int i = 0; i < studentReportDataList.Count; i++)
                {
                    var (commonPlaceholders, rowData) = studentReportDataList[i];
                    
                    var pdfBytes = await GeneratePdfWithDynamicRows(
                        templateFileName,
                        commonPlaceholders,
                        rowData,
                        templateRowPlaceholders);
                    
                    // Generate descriptive filename from placeholders if available
                    var filename = $"report_{i + 1}.pdf";
                    if (commonPlaceholders.TryGetValue("student_name", out var studentName) && !string.IsNullOrWhiteSpace(studentName))
                    {
                        // Sanitize filename to remove invalid characters
                        var sanitizedName = string.Join("_", studentName.Split(Path.GetInvalidFileNameChars()));
                        
                        if (commonPlaceholders.TryGetValue("roll_number", out var rollNumber) && !string.IsNullOrWhiteSpace(rollNumber))
                        {
                            filename = $"{sanitizedName}_{rollNumber}.pdf";
                        }
                        else
                        {
                            filename = $"{sanitizedName}.pdf";
                        }
                    }
                    
                    var pdfPath = Path.Combine(pdfDirectory, filename);
                    await File.WriteAllBytesAsync(pdfPath, pdfBytes);
                    pdfFiles.Add(pdfPath);
                }

                if (asZip)
                {
                    // Create a zip file containing all PDFs with unique filename
                    var zipPath = Path.Combine(tempDirectory, $"reports_{Guid.NewGuid()}.zip");
                    System.IO.Compression.ZipFile.CreateFromDirectory(pdfDirectory, zipPath);
                    return await File.ReadAllBytesAsync(zipPath);
                }
                else
                {
                    // Merge all PDFs into a single PDF with unique filename
                    var mergedPdfPath = Path.Combine(tempDirectory, $"merged_report_{Guid.NewGuid()}.pdf");
                    MergePdfs(pdfFiles, mergedPdfPath);
                    return await File.ReadAllBytesAsync(mergedPdfPath);
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete temporary directory: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Generates a PDF with dynamic table rows based on data list
        /// </summary>
        /// <param name="templateFileName">Name of the template file</param>
        /// <param name="commonPlaceholders">Common placeholders (exam name, date, etc.)</param>
        /// <param name="rowData">List of row data dictionaries</param>
        /// <param name="templateRowPlaceholders">Placeholders in template row (e.g., "student_name", "student_roll")</param>
        /// <returns>Byte array of the generated PDF</returns>
        public async Task<byte[]> GeneratePdfWithDynamicRows(
            string templateFileName, 
            Dictionary<string, string> commonPlaceholders,
            List<Dictionary<string, string>> rowData,
            List<string> templateRowPlaceholders)
        {
            try
            {
                var templatePath = Path.Combine(_environment.WebRootPath, "reports", templateFileName);
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template file not found: {templateFileName}");
                }

                var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);

                try
                {
                    var tempDocxPath = Path.Combine(tempDirectory, "temp.docx");
                    File.Copy(templatePath, tempDocxPath, true);

                    _logger.LogInformation($"Processing template: {templateFileName}");
                    _logger.LogInformation($"Common placeholders count: {commonPlaceholders.Count}");
                    _logger.LogInformation($"Row data count: {rowData.Count}");

                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(tempDocxPath, true))
                    {
                        var body = wordDoc.MainDocumentPart?.Document.Body;
                        if (body == null)
                        {
                            throw new InvalidOperationException("Document body is null");
                        }

                        // Log placeholders being replaced
                        foreach (var kvp in commonPlaceholders)
                        {
                            _logger.LogDebug($"Replacing {{{kvp.Key}}} with: {kvp.Value}");
                        }

                        // STEP 1: Replace common placeholders FIRST (before touching tables)
                        ReplaceTextInBody(body, commonPlaceholders);
                        
                        // Save after replacing common placeholders
                        wordDoc.MainDocumentPart.Document.Save();
                        _logger.LogInformation("Common placeholders replaced and saved");

                        // STEP 2: Find and handle dynamic table rows
                        var tables = body.Descendants<Table>().ToList();
                        _logger.LogInformation($"Found {tables.Count} tables in document");
                        
                        foreach (var table in tables)
                        {
                            var rows = table.Elements<TableRow>().ToList();
                            
                            // Find the row with template placeholders
                            TableRow templateRow = null;
                            foreach (var row in rows)
                            {
                                var rowText = row.InnerText;
                                if (templateRowPlaceholders.Any(p => rowText.Contains($"{{{p}}}")))
                                {
                                    templateRow = row;
                                    _logger.LogInformation($"Found template row with placeholders: {string.Join(", ", templateRowPlaceholders)}");
                                    break;
                                }
                            }

                            if (templateRow != null)
                            {
                                _logger.LogInformation($"Duplicating template row {rowData.Count} times");
                                
                                // Duplicate the template row for each data row
                                foreach (var data in rowData)
                                {
                                    var newRow = (TableRow)templateRow.CloneNode(true);
                                    
                                    // Replace placeholders in the new row
                                    var cellTexts = newRow.Descendants<Text>().ToList();
                                    foreach (var text in cellTexts)
                                    {
                                        foreach (var kvp in data)
                                        {
                                            var key = "{" + kvp.Key + "}";
                                            if (text.Text.Contains(key))
                                            {
                                                text.Text = text.Text.Replace(key, kvp.Value ?? string.Empty);
                                            }
                                        }
                                    }
                                    
                                    // Insert the new row after the template row
                                    templateRow.InsertAfterSelf(newRow);
                                }
                                
                                // Remove the template row
                                templateRow.Remove();
                                _logger.LogInformation("Template row removed after duplication");
                            }
                            else
                            {
                                _logger.LogWarning($"No template row found with placeholders: {string.Join(", ", templateRowPlaceholders)}");
                            }
                        }

                        // STEP 3: Final save after table modifications
                        wordDoc.MainDocumentPart.Document.Save();
                        _logger.LogInformation("Document saved after table modifications");
                    }

                    // Optional: Save debug copy to inspect
                    var debugPath = Path.Combine(tempDirectory, "debug_filled.docx");
                    File.Copy(tempDocxPath, debugPath, true);
                    _logger.LogInformation($"Debug copy saved to: {debugPath}");

                    // STEP 4: Convert to PDF
                    var pdfPath = await ConvertToPdf(tempDocxPath, tempDirectory);
                    var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
                    
                    _logger.LogInformation("PDF generated successfully");

                    return pdfBytes;
                }
                finally
                {
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to delete temporary directory: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF with dynamic rows");
                throw;
            }
        }

        /// <summary>
        /// Helper method to replace text in body - handles both regular text and table cells
        /// Also handles placeholders split across multiple Text elements
        /// Uses the same robust logic as ReplacePlaceholders method
        /// </summary>
        private void ReplaceTextInBody(Body body, Dictionary<string, string> placeholders)
        {
            // Use the same comprehensive replacement logic as ReplacePlaceholders
            // This ensures consistency across all replacement scenarios
            
            // 1. Replace in all text elements throughout the body
            var allTexts = body.Descendants<Text>().ToList();
            foreach (var text in allTexts)
            {
                foreach (var placeholder in placeholders)
                {
                    var key = "{" + placeholder.Key + "}";
                    if (text.Text.Contains(key))
                    {
                        text.Text = text.Text.Replace(key, placeholder.Value ?? string.Empty);
                    }
                }
            }

            // 2. Handle paragraphs where placeholders might be split
            var paragraphs = body.Descendants<Paragraph>().ToList();
            foreach (var para in paragraphs)
            {
                var fullText = para.InnerText;
                
                // Check if any placeholder exists in the paragraph
                bool hasPlaceholder = false;
                string replacedText = fullText;
                
                foreach (var placeholder in placeholders)
                {
                    var key = "{" + placeholder.Key + "}";
                    if (replacedText.Contains(key))
                    {
                        replacedText = replacedText.Replace(key, placeholder.Value ?? string.Empty);
                        hasPlaceholder = true;
                    }
                }
                
                if (hasPlaceholder)
                {
                    // Rebuild the paragraph with the replaced text
                    var textElements = para.Descendants<Text>().ToList();
                    if (textElements.Any())
                    {
                        // Put all text in first element
                        textElements[0].Text = replacedText;
                        
                        // Clear other elements
                        for (int i = 1; i < textElements.Count; i++)
                        {
                            textElements[i].Text = string.Empty;
                        }
                    }
                }
            }

            // 3. Explicitly handle table cells (ensures table content is covered)
            var tableCells = body.Descendants<TableCell>().ToList();
            foreach (var cell in tableCells)
            {
                // Get all text in the cell
                var cellParas = cell.Descendants<Paragraph>().ToList();
                foreach (var para in cellParas)
                {
                    var fullText = para.InnerText;
                    bool hasPlaceholder = false;
                    string replacedText = fullText;
                    
                    foreach (var placeholder in placeholders)
                    {
                        var key = "{" + placeholder.Key + "}";
                        if (replacedText.Contains(key))
                        {
                            replacedText = replacedText.Replace(key, placeholder.Value ?? string.Empty);
                            hasPlaceholder = true;
                        }
                    }
                    
                    if (hasPlaceholder)
                    {
                        var textElements = para.Descendants<Text>().ToList();
                        if (textElements.Any())
                        {
                            textElements[0].Text = replacedText;
                            for (int i = 1; i < textElements.Count; i++)
                            {
                                textElements[i].Text = string.Empty;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates a dynamic Excel report for ClassExamReport using template file with dynamic columns
        /// Then converts to PDF in landscape orientation
        /// </summary>
        /// <param name="templateFileName">Name of the Excel template file</param>
        /// <param name="headerData">Header information (exam category, class, section, date)</param>
        /// <param name="exams">List of exams with their subjects</param>
        /// <param name="students">List of students with their marks data</param>
        /// <returns>Byte array of the generated PDF file</returns>
        public async Task<byte[]> GenerateClassExamReportExcel(
            string templateFileName,
            Dictionary<string, string> headerData,
            List<ExamWithSubjects> exams,
            List<StudentExamData> students)
        {
            try
            {
                // Load template file
                var templatePath = Path.Combine(_environment.WebRootPath, "reports", templateFileName);
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template file not found: {templateFileName}");
                }

                // Create temp directory
                var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDirectory);

                try
                {
                    // Copy template to temp location
                    var tempFilePath = Path.Combine(tempDirectory, "temp.xlsx");
                    File.Copy(templatePath, tempFilePath, true);

                    using (var workbook = new XLWorkbook(tempFilePath))
                    {
                        var worksheet = workbook.Worksheets.First();

                        // === Set page setup for landscape and fit to page ===
                        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                        worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
                        worksheet.PageSetup.FitToPages(1, 0); // Fit all columns on one page width
                        worksheet.PageSetup.Scale = 100; // Start at 100%, will auto-scale to fit
                        worksheet.PageSetup.Margins.Left = 0.5;
                        worksheet.PageSetup.Margins.Right = 0.5;
                        worksheet.PageSetup.Margins.Top = 0.75;
                        worksheet.PageSetup.Margins.Bottom = 0.75;
                        worksheet.PageSetup.CenterHorizontally = true;

                        // === STEP 1: Replace header placeholders ===
                        ReplaceExcelPlaceholders(worksheet, headerData);

                        // === STEP 2: Find template row (contains {student_name}) ===
                        IXLRow templateRow = null;
                        int templateRowNumber = 0;

                        int maxRow = worksheet.LastRowUsed()?.RowNumber() ?? 100;
                        for (int row = 1; row <= maxRow; row++)
                        {
                            var cellValue = worksheet.Cell(row, 1).GetString();
                            if (cellValue.Contains("{student_name}"))
                            {
                                templateRow = worksheet.Row(row);
                                templateRowNumber = row;
                                break;
                            }
                        }

                        if (templateRow == null)
                        {
                            throw new InvalidOperationException("Template row with {student_name} not found in Excel template");
                        }

                        // === STEP 3: Find header rows (above template row) ===
                        int examHeaderRow = templateRowNumber - 2; // Row with exam names
                        int subjectHeaderRow = templateRowNumber - 1; // Row with subject codes

                        // === STEP 4: Create dynamic exam/subject columns ===
                        int currentCol = 4; // Start after Student Name (1), Roll No (2), Rank (3)
                        var columnMap = new Dictionary<(int ExamId, int SubjectId), int>();

                        foreach (var exam in exams)
                        {
                            int examStartCol = currentCol;
                            int subjectsCount = exam.Subjects.Count;

                            if (subjectsCount == 0) continue;

                            // Set exam name in header (merge across subjects)
                            var examHeaderCell = worksheet.Cell(examHeaderRow, examStartCol);
                            examHeaderCell.Value = exam.ExamName;
                            
                            if (subjectsCount > 1)
                            {
                                worksheet.Range(examHeaderRow, examStartCol, examHeaderRow, examStartCol + subjectsCount - 1).Merge();
                            }
                            
                            examHeaderCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            examHeaderCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            examHeaderCell.Style.Alignment.WrapText = true; // Enable text wrapping
                            examHeaderCell.Style.Font.Bold = true;
                            examHeaderCell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                            examHeaderCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            // Add subject columns
                            foreach (var subject in exam.Subjects)
                            {
                                // Subject code/name in sub-header
                                var subHeaderCell = worksheet.Cell(subjectHeaderRow, currentCol);
                                subHeaderCell.Value = !string.IsNullOrEmpty(subject.SubjectCode) 
                                    ? subject.SubjectCode 
                                    : subject.SubjectName;
                                subHeaderCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                subHeaderCell.Style.Alignment.WrapText = true; // Enable text wrapping
                                subHeaderCell.Style.Font.Bold = true;
                                subHeaderCell.Style.Fill.BackgroundColor = XLColor.LightCyan;
                                subHeaderCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                                // Map this exam-subject to column
                                columnMap[(exam.ExamId, subject.SubjectId)] = currentCol;

                                currentCol++;
                            }
                        }

                        // Position Total and Percentage columns after all exam columns
                        int totalColIndex = currentCol;
                        int percentageColIndex = currentCol + 1;

                        // Set Total column header
                        var totalHeaderCell = worksheet.Cell(examHeaderRow, totalColIndex);
                        totalHeaderCell.Value = "Total";
                        worksheet.Range(examHeaderRow, totalColIndex, subjectHeaderRow, totalColIndex).Merge();
                        totalHeaderCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        totalHeaderCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        totalHeaderCell.Style.Alignment.WrapText = true;
                        totalHeaderCell.Style.Font.Bold = true;
                        totalHeaderCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        totalHeaderCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        // Set Percentage column header
                        var percentageHeaderCell = worksheet.Cell(examHeaderRow, percentageColIndex);
                        percentageHeaderCell.Value = "Percentage";
                        worksheet.Range(examHeaderRow, percentageColIndex, subjectHeaderRow, percentageColIndex).Merge();
                        percentageHeaderCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        percentageHeaderCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        percentageHeaderCell.Style.Alignment.WrapText = true;
                        percentageHeaderCell.Style.Font.Bold = true;
                        percentageHeaderCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        percentageHeaderCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        // === STEP 5: Duplicate template row for each student ===
                        int currentRowNumber = templateRowNumber;

                        foreach (var student in students)
                        {
                            IXLRow studentRow;
                            
                            if (currentRowNumber == templateRowNumber)
                            {
                                // First student - use the template row itself
                                studentRow = templateRow;
                            }
                            else
                            {
                                // Insert new row and copy template formatting
                                worksheet.Row(currentRowNumber).InsertRowsBelow(1);
                                studentRow = worksheet.Row(currentRowNumber);
                                
                                // Copy style from template row
                                for (int col = 1; col <= percentageColIndex; col++)
                                {
                                    var templateCell = templateRow.Cell(col);
                                    var newCell = studentRow.Cell(col);
                                    newCell.Style = templateCell.Style;
                                }
                            }

                            // Fill static columns
                            studentRow.Cell(1).Value = student.StudentName;
                            studentRow.Cell(1).Style.Alignment.WrapText = true; // Enable text wrapping
                            studentRow.Cell(1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            studentRow.Cell(2).Value = student.RollNumber ?? "";
                            studentRow.Cell(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            studentRow.Cell(2).Style.Alignment.WrapText = true;
                            studentRow.Cell(2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            studentRow.Cell(3).Value = student.Rank;
                            studentRow.Cell(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            studentRow.Cell(3).Style.Alignment.WrapText = true;
                            studentRow.Cell(3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            // Fill in marks data for each exam-subject
                            foreach (var marks in student.Marks)
                            {
                                if (columnMap.TryGetValue((marks.ExamId, marks.SubjectId), out int col))
                                {
                                    var dataCell = studentRow.Cell(col);
                                    string cellValue = marks.HasMarks
                                        ? $"{marks.ObtainedMarks}/{marks.TotalMarks}"
                                        : "N/A";

                                    dataCell.Value = cellValue;
                                    dataCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    dataCell.Style.Alignment.WrapText = true; // Enable text wrapping
                                    dataCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                }
                            }

                            // Fill Total column
                            var totalCell = studentRow.Cell(totalColIndex);
                            totalCell.Value = $"{student.TotalObtained}";
                            totalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            totalCell.Style.Alignment.WrapText = true;
                            totalCell.Style.Font.Bold = true;
                            totalCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            // Fill Percentage column
                            var percentageCell = studentRow.Cell(percentageColIndex);
                            percentageCell.Value = student.Percentage.ToString("F2") + "%";
                            percentageCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            percentageCell.Style.Alignment.WrapText = true;
                            percentageCell.Style.Font.Bold = true;
                            percentageCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                            currentRowNumber++;
                        }

                        // === Auto-adjust columns and rows ===
                        // Auto-fit columns to content
                        worksheet.Columns().AdjustToContents();
                        
                        // Auto-fit rows to content (important for wrapped text)
                        worksheet.Rows().AdjustToContents();
                        
                        // Set minimum column widths to prevent too narrow columns
                        foreach (var column in worksheet.ColumnsUsed())
                        {
                            if (column.Width < 8)
                            {
                                column.Width = 8;
                            }
                            // Set maximum width to force wrapping for very long content
                            if (column.Width > 25)
                            {
                                column.Width = 25;
                            }
                        }
                        
                        // Re-adjust rows after setting column widths
                        worksheet.Rows().AdjustToContents();

                        // Save Excel to temp file
                        workbook.SaveAs(tempFilePath);
                    }

                    // Convert Excel to PDF using LibreOffice with landscape option
                    var pdfPath = await ConvertToPdfLandscape(tempFilePath, tempDirectory);
                    var pdfBytes = await File.ReadAllBytesAsync(pdfPath);

                    return pdfBytes;
                }
                finally
                {
                    // Cleanup
                    try
                    {
                        Directory.Delete(tempDirectory, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to delete temporary directory: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel ClassExamReport from template");
                throw;
            }
        }

        /// <summary>
        /// Converts Excel file to PDF in landscape orientation using LibreOffice
        /// </summary>
        private async Task<string> ConvertToPdfLandscape(string excelPath, string outputDirectory)
        {
            // Determine LibreOffice command based on OS
            string libreOfficeCommand = "libreoffice"; // Default for Linux/Mac
            if (OperatingSystem.IsWindows())
            {
                libreOfficeCommand = "soffice"; // Windows typically uses soffice
            }

            // LibreOffice command with landscape option
            // Using --infilter parameter to specify landscape orientation
            var processStartInfo = new ProcessStartInfo
            {
                FileName = libreOfficeCommand,
                Arguments = $"--headless --convert-to pdf --outdir \"{outputDirectory}\" \"{excelPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    // Start reading streams immediately
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // Wait for the tasks and the process exit
                    await Task.WhenAll(outputTask, errorTask);
                    await process.WaitForExitAsync();

                    var output = outputTask.Result;
                    var error = errorTask.Result;

                    if (process.ExitCode != 0)
                    {
                        _logger.LogError("LibreOffice conversion failed. Exit code: {ExitCode}", process.ExitCode);
                        _logger.LogError("STDOUT: {Output}", output);
                        _logger.LogError("STDERR: {Error}", error);

                        throw new InvalidOperationException($"LibreOffice returned error code {process.ExitCode}: {error}");
                    }

                    var pdfFileName = Path.GetFileNameWithoutExtension(excelPath) + ".pdf";
                    var pdfPath = Path.Combine(outputDirectory, pdfFileName);

                    if (!File.Exists(pdfPath))
                    {
                        throw new FileNotFoundException($"Conversion reported success, but file missing: {pdfPath}");
                    }

                    return pdfPath;
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                _logger.LogError(ex, "Failed to start the process. Is LibreOffice installed and the path correct?");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during PDF conversion.");
                throw;
            }
        }

        /// <summary>
        /// Helper method to replace placeholders in Excel worksheet
        /// </summary>
        private void ReplaceExcelPlaceholders(IXLWorksheet worksheet, Dictionary<string, string> placeholders)
        {
            foreach (var row in worksheet.RowsUsed())
            {
                foreach (var cell in row.Cells())
                {
                    var cellValue = cell.GetString();
                    if (string.IsNullOrEmpty(cellValue)) continue;

                    foreach (var kvp in placeholders)
                    {
                        var placeholder = "{" + kvp.Key + "}";
                        if (cellValue.Contains(placeholder))
                        {
                            cellValue = cellValue.Replace(placeholder, kvp.Value ?? string.Empty);
                        }
                    }

                    if (cell.GetString() != cellValue)
                    {
                        cell.Value = cellValue;
                    }
                }
            }
        }

        /// <summary>
        /// Merges multiple PDF files into a single PDF
        /// </summary>
        /// <param name="pdfFiles">List of PDF file paths to merge</param>
        /// <param name="outputPath">Output path for the merged PDF</param>
        private void MergePdfs(List<string> pdfFiles, string outputPath)
        {
            // Validate input
            if (pdfFiles == null || pdfFiles.Count == 0)
            {
                throw new ArgumentException("PDF files list cannot be null or empty", nameof(pdfFiles));
            }

            try
            {
                using (var writerStream = new FileStream(outputPath, FileMode.Create))
                using (var writer = new PdfWriter(writerStream))
                using (var mergedDocument = new PdfDocument(writer))
                {
                    var merger = new PdfMerger(mergedDocument);

                    foreach (var pdfFile in pdfFiles)
                    {
                        try
                        {
                            using (var readerStream = new FileStream(pdfFile, FileMode.Open, FileAccess.Read))
                            using (var reader = new PdfReader(readerStream))
                            using (var sourceDocument = new PdfDocument(reader))
                            {
                                // Add all pages from source document to merged document
                                merger.Merge(sourceDocument, 1, sourceDocument.GetNumberOfPages());
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to merge PDF file: {pdfFile}");
                            throw new InvalidOperationException($"Failed to merge PDF file: {Path.GetFileName(pdfFile)}", ex);
                        }
                    }
                }
                
                _logger.LogInformation($"Successfully merged {pdfFiles.Count} PDF files into {outputPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during PDF merge operation");
                throw;
            }
        }
    }

    // Supporting classes for Excel report
    public class ExamWithSubjects
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; }
        public List<SubjectInfo> Subjects { get; set; } = new List<SubjectInfo>();
    }

    public class SubjectInfo
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectName { get; set; }
    }

    public class StudentExamData
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string RollNumber { get; set; }
        public int Rank { get; set; }
        public decimal TotalObtained { get; set; }
        public decimal TotalMax { get; set; }
        public decimal Percentage { get; set; }
        public List<StudentMarksData> Marks { get; set; } = new List<StudentMarksData>();
    }

    public class StudentMarksData
    {
        public int ExamId { get; set; }
        public int SubjectId { get; set; }
        public bool HasMarks { get; set; }
        public decimal ObtainedMarks { get; set; }
        public decimal TotalMarks { get; set; }
    }
}
