using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace UTS_SMS.Services
{
    public class PdfService : IPdfService
    {
        private readonly IConverter _converter;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PdfService(IConverter converter, ApplicationDbContext context, IWebHostEnvironment env)
        {
            _converter = converter;
            _context = context;
            _env = env;
        }

        public async Task<byte[]> GenerateTransactionReceiptPdfAsync(int transactionId)
        {
            var transaction = await _context.BillingTransactions
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.ClassObj)
                .Include(t => t.BillingMaster)
                    .ThenInclude(b => b.Student)
                        .ThenInclude(s => s.SectionObj)
                .Include(t => t.Campus)
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new Exception($"Transaction with ID {transactionId} not found");

            // Calculate already paid
            var alreadyPaid = await _context.BillingTransactions
                .Where(t => t.BillingMasterId == transaction.BillingMasterId && t.Id < transaction.Id)
                .SumAsync(t => (decimal?)t.AmountPaid) ?? 0;

            // Get extra charges
            var extraChargePayments = await _context.ClassFeeExtraChargePaymentHistories
                .Where(ph => ph.BillingMasterId == transaction.BillingMasterId)
                .Include(ph => ph.ClassFeeExtraCharge)
                .ToListAsync();

            var extraChargeItems = extraChargePayments.Select(ph => new
            {
                ChargeName = ph.ClassFeeExtraCharge.ChargeName,
                Amount = ph.AmountPaid,
                Category = ph.ClassFeeExtraCharge.Category,
                Type = "ExtraCharge"
            }).ToList();

            // Get fines
            var fineCharges = await _context.StudentFineCharges
                .Where(sfc => sfc.BillingMasterId == transaction.BillingMasterId && sfc.IsPaid)
                .Select(sfc => new
                {
                    ChargeName = sfc.ChargeName,
                    Amount = sfc.Amount,
                    Category = "Fine/Charge",
                    Type = "Fine"
                })
                .ToListAsync();

            var allCharges = extraChargeItems.Concat(fineCharges).ToList();
            var extraChargesTotal = allCharges.Sum(i => i.Amount);

            // Calculate remaining
            decimal netPayable;
            decimal remaining;
            if (alreadyPaid > 0)
            {
                netPayable = transaction.BillingMaster.TotalPayable - alreadyPaid;
                remaining = netPayable - transaction.AmountPaid;
            }
            else
            {
                netPayable = transaction.BillingMaster.TotalPayable;
                remaining = transaction.BillingMaster.TotalPayable - transaction.AmountPaid;
            }

            var printTime = DateTime.Now;

            // Generate HTML content
            var htmlContent = GenerateReceiptHtml(transaction, alreadyPaid, netPayable, remaining, printTime, extraChargesTotal, allCharges);

            // Configure PDF settings
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A6,
                    Margins = new MarginSettings { Top = 0, Bottom = 0, Left = 0, Right = 0 }
                },
                Objects = {
                    new ObjectSettings {
                        PagesCount = true,
                        HtmlContent = htmlContent,
                        WebSettings = { DefaultEncoding = "utf-8" },
                        HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = false },
                        FooterSettings = { FontSize = 9, Line = false, Center = "" }
                    }
                }
            };

            return _converter.Convert(doc);
        }

        private string GenerateReceiptHtml(BillingTransaction transaction, decimal alreadyPaid, decimal netPayable, decimal remaining, DateTime printTime, decimal extraChargesTotal, dynamic allCharges)
        {
            var student = transaction.BillingMaster.Student;
            var campus = transaction.Campus;
            var logoPath = "";

            if (!string.IsNullOrEmpty(campus.Logo))
            {
                var fullPath = Path.Combine(_env.WebRootPath, campus.Logo.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    var imageBytes = File.ReadAllBytes(fullPath);
                    var base64 = Convert.ToBase64String(imageBytes);
                    var extension = Path.GetExtension(fullPath).ToLower();
                    var mimeType = extension switch
                    {
                        ".png" => "image/png",
                        ".jpg" => "image/jpeg",
                        ".jpeg" => "image/jpeg",
                        ".gif" => "image/gif",
                        _ => "image/png"
                    };
                    logoPath = $"data:{mimeType};base64,{base64}";
                }
            }

            var watermarkHtml = remaining <= 0 ? "<div class='watermark'>PAID</div>" : "";

            var extraChargesHtml = "";
            if (transaction.BillingMaster.MiscallaneousCharges > 0)
            {
                if (allCharges != null && ((IEnumerable<dynamic>)allCharges).Any())
                {
                    extraChargesHtml = $@"
                        <div class='fee-row' style='margin-bottom: 1px;'>
                            <span><strong>Extra Charges:</strong></span>
                            <span><strong>?{transaction.BillingMaster.MiscallaneousCharges:N0}</strong></span>
                        </div>";

                    foreach (var item in allCharges)
                    {
                        var badgeClass = item.Type == "Fine" ? "badge-fine" :
                                       item.Category == "MonthlyCharges" ? "badge-monthly" :
                                       item.Category == "OncePerClass" ? "badge-once" : "badge-lifetime";

                        var badgeText = item.Type == "Fine" ? "Fine" :
                                      item.Category == "MonthlyCharges" ? "Mon" :
                                      item.Category == "OncePerClass" ? "Once" : "Life";

                        extraChargesHtml += $@"
                        <div class='extra-charge-item'>
                            <div style='display: flex; justify-content: space-between; align-items: center;'>
                                <span>
                                    • {item.ChargeName}
                                    <span class='badge {badgeClass}'>{badgeText}</span>
                                </span>
                                <span>?{item.Amount:N0}</span>
                            </div>
                        </div>";
                    }
                }
                else
                {
                    extraChargesHtml = $@"
                        <div class='fee-row'>
                            <span>Extra Charges:</span>
                            <span>?{transaction.BillingMaster.MiscallaneousCharges:N0}</span>
                        </div>";
                }
            }

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        @page {{
            size: A6 portrait;
            margin: 0;
        }}

        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            margin: 0;
            padding: 0;
            font-family: Arial, sans-serif;
            font-size: 9px;
            line-height: 1.2;
        }}

        .receipt-container {{
            width: 105mm;
            height: 148mm;
            position: relative;
            padding: 8mm;
            box-sizing: border-box;
            background: white;
        }}

        .watermark {{
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%) rotate(-45deg);
            font-size: 48px;
            font-weight: bold;
            color: red;
            opacity: 0.15;
            z-index: 1;
            pointer-events: none;
        }}

        .content {{
            position: relative;
            z-index: 2;
        }}

        .header {{
            text-align: center;
            border-bottom: 2px solid #2563eb;
            padding-bottom: 4px;
            margin-bottom: 6px;
        }}

        .header img {{
            height: 24px;
            width: 24px;
            object-fit: contain;
            margin-bottom: 2px;
        }}

        .header h1 {{
            font-size: 14px;
            font-weight: bold;
            color: #2563eb;
            margin: 2px 0;
        }}

        .header p {{
            font-size: 7px;
            color: #666;
            margin: 1px 0;
        }}

        .header h2 {{
            font-size: 11px;
            font-weight: bold;
            color: #1e40af;
            margin-top: 3px;
        }}

        .grid-2 {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 4px;
            margin-bottom: 5px;
        }}

        .section {{
            margin-bottom: 5px;
        }}

        .section-title {{
            font-weight: bold;
            color: #1e40af;
            margin-bottom: 3px;
            font-size: 9px;
            border-bottom: 1px solid #e5e7eb;
            padding-bottom: 1px;
        }}

        .info-box {{
            background: #f9fafb;
            padding: 4px;
            border-radius: 3px;
            margin-bottom: 5px;
        }}

        .fee-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 2px;
            font-size: 8px;
        }}

        .fee-row.bold {{
            font-weight: bold;
            border-top: 1px solid #d1d5db;
            padding-top: 2px;
            margin-top: 2px;
        }}

        .fee-row.success {{
            color: #059669;
        }}

        .fee-row.error {{
            color: #dc2626;
        }}

        .fee-row.primary {{
            color: #2563eb;
        }}

        .payment-box {{
            background: #d1fae5;
            padding: 4px;
            border-radius: 3px;
            margin-bottom: 5px;
        }}

        .extra-charge-item {{
            padding-left: 8px;
            margin: 2px 0;
            border-left: 2px solid #bfdbfe;
            font-size: 7px;
        }}

        .badge {{
            display: inline-block;
            padding: 1px 3px;
            border-radius: 2px;
            font-size: 6px;
            font-weight: 600;
        }}

        .badge-fine {{ background: #fee2e2; color: #991b1b; }}
        .badge-monthly {{ background: #fed7aa; color: #9a3412; }}
        .badge-once {{ background: #dbeafe; color: #1e40af; }}
        .badge-lifetime {{ background: #e9d5ff; color: #6b21a8; }}

        .footer {{
            border-top: 1px solid #d1d5db;
            padding-top: 4px;
            margin-top: 5px;
            display: flex;
            justify-content: space-between;
            align-items: flex-end;
            font-size: 7px;
        }}

        .signature {{
            text-align: center;
        }}

        .signature-line {{
            border-top: 1px solid #666;
            width: 60px;
            margin-bottom: 2px;
        }}

        strong {{
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class='receipt-container'>
        {watermarkHtml}
        <div class='content'>
            <div class='header'>
                {(string.IsNullOrEmpty(logoPath) ? "" : $"<img src='{logoPath}' alt='{campus.Name} Logo' />")}
                <h1>{campus.Name}</h1>
                <p>{campus.Address}</p>
                {(string.IsNullOrEmpty(campus.Phone) ? "" : $"<p>Phone: {campus.Phone}</p>")}
                <h2>FEE RECEIPT</h2>
            </div>

            <div class='grid-2' style='font-size: 8px;'>
                <div>
                    <div><strong>Receipt:</strong> #{transaction.Id:000000}</div>
                    <div><strong>Date:</strong> {transaction.PaymentDate:dd MMM yyyy}</div>
                    <div><strong>Time:</strong> {transaction.PaymentDate:hh:mm tt}</div>
                </div>
                <div>
                    <div><strong>Month:</strong> {new DateTime(transaction.BillingMaster.ForYear, transaction.BillingMaster.ForMonth, 1):MMM yyyy}</div>
                    <div><strong>Received By:</strong> {transaction.ReceivedBy}</div>
                </div>
            </div>

            <div class='section'>
                <div class='section-title'>Student Details</div>
                <div class='info-box'>
                    <div class='grid-2' style='font-size: 8px;'>
                        <div>
                            <div><strong>Name:</strong> {student.StudentName}</div>
                            <div><strong>Class:</strong> {student.ClassObj?.Name}</div>
                            <div><strong>Section:</strong> {student.SectionObj?.Name}</div>
                        </div>
                        <div>
                            <div><strong>Father:</strong> {student.FatherName}</div>
                            <div><strong>Phone:</strong> {student.FatherPhone}</div>
                            <div><strong>Gender:</strong> {student.Gender}</div>
                        </div>
                    </div>
                </div>
            </div>

            <div class='section'>
                <div class='section-title'>Fee Details</div>
                <div class='fee-row'>
                    <span>Tuition Fee:</span>
                    <span>?{transaction.BillingMaster.TuitionFee:N0}</span>
                </div>
                {(transaction.BillingMaster.AdmissionFee > 0 ? $@"
                <div class='fee-row'>
                    <span>Admission Fee:</span>
                    <span>?{transaction.BillingMaster.AdmissionFee:N0}</span>
                </div>" : "")}
                {extraChargesHtml}
                {(transaction.BillingMaster.Fine > 0 ? $@"
                <div class='fee-row'>
                    <span>Fine:</span>
                    <span>?{transaction.BillingMaster.Fine:N0}</span>
                </div>" : "")}
                {(transaction.BillingMaster.PreviousDues > 0 ? $@"
                <div class='fee-row'>
                    <span>Previous Dues:</span>
                    <span>?{transaction.BillingMaster.PreviousDues:N0}</span>
                </div>" : "")}
                <div class='fee-row bold'>
                    <span>Total Payable:</span>
                    <span>?{transaction.BillingMaster.TotalPayable:N0}</span>
                </div>

                {(alreadyPaid > 0 ? $@"
                <div class='fee-row primary' style='font-size: 8px;'>
                    <span>Already Paid:</span>
                    <span>-?{alreadyPaid:N0}</span>
                </div>
                <div class='fee-row bold'>
                    <span>Net Payable:</span>
                    <span>?{netPayable:N0}</span>
                </div>" : "")}
            </div>

            <div class='section'>
                <div class='section-title'>Payment Details</div>
                <div class='payment-box'>
                    <div class='fee-row success bold'>
                        <span>Amount Paid:</span>
                        <span>?{transaction.AmountPaid:N0}</span>
                    </div>
                    {(transaction.CashPaid > 0 ? $@"
                    <div class='fee-row'>
                        <span>Cash:</span>
                        <span>?{transaction.CashPaid:N0}</span>
                    </div>" : "")}
                    {(transaction.OnlinePaid > 0 ? $@"
                    <div class='fee-row'>
                        <span>Online:</span>
                        <span>?{transaction.OnlinePaid:N0}</span>
                    </div>" : "")}
                    {(!string.IsNullOrEmpty(transaction.TransactionReference) ? $@"
                    <div class='fee-row' style='font-size: 7px;'>
                        <span>Ref:</span>
                        <span>{transaction.TransactionReference}</span>
                    </div>" : "")}
                    <div class='fee-row bold {(remaining > 0 ? "error" : remaining < 0 ? "primary" : "success")}'>
                        <span>{(remaining > 0 ? "Dues" : remaining < 0 ? "Advance" : "Clear")}:</span>
                        <span>?{Math.Abs(remaining):N0}</span>
                    </div>
                </div>
            </div>

            <div class='footer'>
                <div style='color: #6b7280;'>
                    <div>Computer generated receipt</div>
                    <div>Printed: {printTime:dd MMM yy hh:mm tt}</div>
                </div>
                <div class='signature'>
                    <div class='signature-line'></div>
                    <div style='color: #6b7280;'>Authorized Sign</div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>";

            return html;
        }
    }
}
