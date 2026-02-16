using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace UTS_SMS.Services
{
    public interface ISalaryDeductionService
    {
        Task ProcessMonthlyDeductionsAsync();
    }

    public class SalaryDeductionService : ISalaryDeductionService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SalaryDeductionService> _logger;

        public SalaryDeductionService(IServiceProvider serviceProvider, ILogger<SalaryDeductionService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ProcessMonthlyDeductionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var currentDate = DateTime.Now;
                var previousMonth = currentDate.AddMonths(-1);
                int forMonth = currentDate.Month;
                int forYear = currentDate.Year;

                _logger.LogInformation($"Processing salary deductions for {forMonth}/{forYear}");

                // Find all active students with PaymentMode = CutFromSalary or CustomRatio
                var studentsForDeduction = await context.StudentCategoryAssignments
                    .Include(sca => sca.Student)
                        .ThenInclude(s => s.ClassObj)
                    .Include(sca => sca.StudentCategory)
                    .Include(sca => sca.Employee)
                    .Where(sca => sca.IsActive &&
                                  sca.StudentCategory.CategoryType == "EmployeeParent" &&
                                  (sca.PaymentMode == "CutFromSalary" || sca.PaymentMode == "CustomRatio") &&
                                  sca.EmployeeId.HasValue)
                    .ToListAsync();

                foreach (var categoryAssignment in studentsForDeduction)
                {
                    var student = categoryAssignment.Student;
                    if (student == null || student.HasLeft) continue;

                    // Check if billing already exists for this month
                    var existingBilling = await context.BillingMaster
                        .FirstOrDefaultAsync(b => b.StudentId == student.Id &&
                                                  b.ForMonth == forMonth &&
                                                  b.ForYear == forYear);

                    if (existingBilling != null)
                    {
                        _logger.LogInformation($"Billing already exists for student {student.Id} for {forMonth}/{forYear}");
                        continue;
                    }

                    // Get class fee information
                    var classFee = await context.ClassFees
                        .FirstOrDefaultAsync(cf => cf.ClassId == student.Class);

                    if (classFee == null)
                    {
                        _logger.LogWarning($"Class fee not found for student {student.Id}");
                        continue;
                    }

                    // Get employee salary
                    var employeeSalary = await context.SalaryDefinitions
                        .Where(sd => sd.EmployeeId == categoryAssignment.EmployeeId && sd.IsActive)
                        .FirstOrDefaultAsync();

                    if (employeeSalary == null)
                    {
                        _logger.LogWarning($"Employee salary not found for employee {categoryAssignment.EmployeeId}");
                        continue;
                    }

                    // Calculate tuition fee with discount
                    var tuitionFee = classFee.TuitionFee * (1 - ((student.TuitionFeeDiscountPercent ?? 0) / 100m));

                    // Calculate extra charges
                    var extraChargeService = scope.ServiceProvider.GetRequiredService<IExtraChargeService>();
                    var extraChargesAmount = await extraChargeService.CalculateExtraCharges(student.Class, student.Id, student.CampusId);

                    // Calculate admission fee using a percentage discount
                    // Check if AdmissionNextMonth is enabled - if so, skip admission fee in salary deduction
                    var admissionFee = (student.AdmissionFeePaid) ? 0 :
                        Math.Max(0, classFee.AdmissionFee * (1 - (student.AdmissionFeeDiscountAmount ?? 0) / 100m));

                    // Calculate total fee
                    var totalFee = tuitionFee + extraChargesAmount + admissionFee;

                    // Calculate deduction amount
                    var deductionPercent = categoryAssignment.CustomTuitionPercent ?? 100m;
                    var calculatedDeduction = (totalFee * deductionPercent) / 100m;

                    // Get used salary by siblings (using a single query with join)
                    var usedSalary = await context.SalaryDeductions
                        .Where(sd => sd.EmployeeId == categoryAssignment.EmployeeId &&
                                     sd.ForMonth == forMonth &&
                                     sd.ForYear == forYear)
                        .SumAsync(sd => (decimal?)sd.AmountDeducted) ?? 0m;

                    var availableSalary = employeeSalary.NetSalary - usedSalary;

                    // Cap deduction to available salary
                    var finalDeduction = Math.Min(calculatedDeduction, Math.Max(0, availableSalary));

                    if (finalDeduction <= 0)
                    {
                        _logger.LogWarning($"No available salary for student {student.Id}");
                        continue;
                    }

                    // Get previous dues
                    var lastRecord = await context.BillingMaster
                        .Where(b => b.StudentId == student.Id)
                        .OrderByDescending(b => b.ForYear)
                        .ThenByDescending(b => b.ForMonth)
                        .FirstOrDefaultAsync();

                    decimal previousDues = lastRecord?.Dues ?? 0;

                    // Calculate total amount and remaining dues
                    var totalAmount = totalFee + previousDues;
                    var remainingDues = Math.Max(0, totalAmount - finalDeduction);

                    // Create billing master
                    var billingMaster = new BillingMaster
                    {
                        StudentId = student.Id,
                        ClassId = student.Class,
                        ForMonth = forMonth,
                        ForYear = forYear,
                        TuitionFee = tuitionFee,
                        AdmissionFee = admissionFee,
                        MiscallaneousCharges = extraChargesAmount,
                        PreviousDues = previousDues,
                        Fine = 0,
                        Dues = remainingDues,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System",
                        CampusId = student.CampusId,
                        RemarksPreviousDues = "Automated salary deduction"
                    };

                    context.BillingMaster.Add(billingMaster);
                    await context.SaveChangesAsync();

                    // Create billing transaction (using salary deduction amount)
                    var billingTransaction = new BillingTransaction
                    {
                        BillingMasterId = billingMaster.Id,
                        AmountPaid = finalDeduction,
                        CashPaid = 0,
                        OnlinePaid = 0,
                        PaymentDate = DateTime.Now,
                        ReceivedBy = "System-SalaryDeduction",
                        CampusId = student.CampusId
                    };

                    context.BillingTransactions.Add(billingTransaction);
                    await context.SaveChangesAsync();

                    // Create salary deduction record
                    var salaryDeduction = new SalaryDeduction
                    {
                        StudentId = student.Id,
                        EmployeeId = categoryAssignment.EmployeeId.Value,
                        BillingMasterId = billingMaster.Id,
                        AmountDeducted = finalDeduction,
                        ForMonth = forMonth,
                        ForYear = forYear,
                        DeductionDate = DateTime.Now,
                        CreatedBy = "System",
                        CampusId = student.CampusId
                    };

                    context.SalaryDeductions.Add(salaryDeduction);

                    // Mark admission fee as paid if applicable
                    if (!student.AdmissionFeePaid && admissionFee > 0)
                    {
                        student.AdmissionFeePaid = true;
                        context.Students.Update(student);
                    }

                    // Mark unpaid student fines/charges as paid
                    var unpaidFines = await context.StudentFineCharges
                        .Where(sfc => sfc.StudentId == student.Id && !sfc.IsPaid && sfc.IsActive)
                        .ToListAsync();

                    foreach (var fine in unpaidFines)
                    {
                        fine.IsPaid = true;
                        fine.PaidDate = DateTime.Now;
                        fine.BillingMasterId = billingMaster.Id;
                        fine.ModifiedBy = "System";
                        fine.ModifiedDate = DateTime.Now;
                    }

                     await context.SaveChangesAsync();

                    _logger.LogInformation($"Processed deduction for student {student.Id}: {finalDeduction}");
                }

                _logger.LogInformation("Monthly salary deductions processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing monthly salary deductions");
                throw;
            }
        }
    }
}
