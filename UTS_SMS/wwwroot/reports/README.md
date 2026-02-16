# Report Templates Directory

This directory contains `.docx` templates for generating reports in the School Management System.

## Required Templates

Place the following `.docx` template files in this directory:

1. **AwardSheet.docx** - Test marks entry sheet (with dynamic rows)
2. **ClassExamReport.docx** - Class-wise exam broadsheet
3. **StudentReportCard.docx** - Individual student report card
4. **ReportCardHistory.docx** - Report card with exam history

## Template Guide

### 1. AwardSheet.docx (Dynamic Row Template)

**NEW FEATURE**: This template now uses **dynamic row generation**. You only need to provide ONE template row with placeholders, and the system will automatically duplicate it for all students.

#### Header Placeholders (Regular text):
```
{exam_name}         - Name of the exam
{exam_category}     - Category of exam (Mid Term, Final, etc.)
{class_name}        - Class name
{section_name}      - Section name
{subject_name}      - Subject name
{total_marks}       - Total marks for the subject
{date}              - Current date
{count}             - Total number of students
```

#### Table Row Placeholders (In a Word table):
Create a **single template row** in your Word table with these placeholders:
```
{serial_no}         - Auto-generated serial number
{student_name}      - Student's full name
{student_roll}      - Roll number
{student_obtained}  - Marks obtained
{student_grade}     - Grade (A+, A, B, etc.)
{student_remarks}   - Additional remarks
```

**Example Table Structure in Word:**
```
+-----+----------------+-----------+----------+-------+----------+
| Sr# | Student Name   | Roll No.  | Obtained | Grade | Remarks  |
+-----+----------------+-----------+----------+-------+----------+
| {serial_no} | {student_name} | {student_roll} | {student_obtained} | {student_grade} | {student_remarks} |
+-----+----------------+-----------+----------+-------+----------+
```

The system will:
1. Find the row with `{student_name}` placeholder
2. Duplicate it for EACH student in the class
3. Replace placeholders with actual data
4. Remove the template row
5. Convert to PDF

### 2. ClassExamReport.docx

**Static placeholders** (old method - still supported):
```
{exam_category}
{class_name}
{section_name}
{date}

{student_1_rank}, {student_1_name}, {student_1_roll}, {student_1_total}, {student_1_obtained}, {student_1_percentage}
{student_2_rank}, {student_2_name}, ...
... up to {student_50_*}
```

### 3. StudentReportCard.docx

Individual student report placeholders:
```
{student_name}
{father_name}
{roll_number}
{class_name}
{section_name}
{exam_name}
{exam_category}
{total_marks}
{obtained_marks}
{percentage}
{position}
{date}

{subject_1_name}, {subject_1_total}, {subject_1_obtained}, {subject_1_percentage}, {subject_1_grade}
... up to {subject_20_*}
```

### 4. ReportCardHistory.docx

Historical performance report:
```
{student_name}
{father_name}
{roll_number}
{class_name}
{section_name}
{exam_category}
{date}

{focus_exam_name}
{focus_total_marks}
{focus_obtained_marks}
{focus_percentage}

{history_1_exam}, {history_1_total}, {history_1_obtained}, {history_1_percentage}
... up to {history_5_*}
```

## Dynamic Row Feature

### When to Use Dynamic Rows vs Static Placeholders

**Use Dynamic Rows** (recommended):
- ? Variable number of records (students, marks, etc.)
- ? Tables that need to grow/shrink automatically
- ? Clean, maintainable templates
- ? Better Word table formatting

**Use Static Placeholders** (legacy):
- ? Fixed number of records (up to 50)
- ? Manual template maintenance
- ? Requires updating template if structure changes

### How Dynamic Rows Work

1. **In Your Word Template**: Create ONE row with placeholders
2. **In the Code**: Provide data as a list of dictionaries
3. **System Behavior**: 
   - Finds the template row
   - Duplicates it N times (N = number of students)
   - Fills each row with student data
   - Removes the original template row

### Migrating from Static to Dynamic

**Before (Static - 50 rows manually)**:
```csharp
placeholders["student_1_name"] = "Ali";
placeholders["student_2_name"] = "Sara";
// ... up to student_50_name
```

**After (Dynamic - automatic)**:
```csharp
var rowData = new List<Dictionary<string, string>>();
foreach(var student in students) {
    rowData.Add(new Dictionary<string, string> {
        { "student_name", student.Name },
        { "student_roll", student.Roll }
    });
}
```

## Quick Start

1. Create your `.docx` templates using Microsoft Word or LibreOffice Writer
2. For dynamic tables: Insert ONE template row with placeholders
3. For static content: Insert placeholders anywhere in the document
4. Save templates in this directory
5. Navigate to the Reports section in the application
6. Generate PDFs by filling in the required parameters

## System Requirements

- **LibreOffice** must be installed on the server for PDF conversion
- Template files must be in `.docx` format (DOCX)
- Ensure LibreOffice is available in the system PATH

Test LibreOffice installation:
```bash
# Windows
soffice --version

# Linux/Mac
libreoffice --version
```

## Example: Creating AwardSheet.docx

1. Open Microsoft Word or LibreOffice Writer
2. Add header with: `Award Sheet - {exam_name}` 
3. Add: `Class: {class_name}-{section_name} | Subject: {subject_name}`
4. Create a table:
   - Header row: Sr# | Student Name | Roll No. | Obtained | Grade | Remarks
   - Template row: `{serial_no}` | `{student_name}` | `{student_roll}` | `{student_obtained}` | `{student_grade}` | `{student_remarks}`
5. Save as `AwardSheet.docx` in this directory

The system will automatically generate rows for ALL students!

## Troubleshooting

### PDF Generation Fails
- Ensure LibreOffice is installed and accessible
- Check that template file exists in this directory
- Verify template file is not corrupted

### Placeholders Not Replaced
- Check placeholder format: `{placeholder_name}` (curly braces required)
- Ensure exact spelling matches
- Avoid special characters in placeholder names

### Dynamic Rows Not Working
- Verify template row contains at least one of the specified placeholders
- Check that row is inside a Word table
- Ensure table structure is valid

## Support

For assistance with template creation or troubleshooting, refer to the main documentation guide or contact the development team.
