using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Services
{
    public interface IExtraChargeService
    {
        Task<List<ClassFeeExtraCharges>> GetApplicableCharges(int classId, int studentId, int campusId);
        Task<decimal> CalculateExtraCharges(int classId, int studentId, int campusId);
        Task<bool> HasPaidCharge(int studentId, int chargeId, int? currentClassId = null);
        Task SavePaymentHistory(int studentId, int chargeId, int billingMasterId, int classId, decimal amount, int campusId);
    }

    public class ExtraChargeService : IExtraChargeService
    {
        private readonly ApplicationDbContext _context;

        public ExtraChargeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClassFeeExtraCharges>> GetApplicableCharges(int classId, int studentId, int campusId)
        {
            var applicableCharges = await _context.ClassFeeExtraCharges
                .Where(ec => ec.IsActive && !ec.IsDeleted && ec.CampusId == campusId)
                .Where(ec =>
                    //// 1. Charge is linked to the student's specific class
                    //ec.ClassId == classId ||

                    //// 2. Charge is global (no specific class, but belongs to campus)
                    //ec.ClassId == null ||

                    // 3. Charge is specifically assigned to this student in the assignment table
                    _context.StudentChargeAssignments.Any(sca =>
                        sca.ClassFeeExtraChargeId == ec.Id &&
                        sca.StudentId == studentId &&
                        sca.IsAssigned &&
                        sca.CampusId == campusId)
                )
                .Distinct()
                .ToListAsync();

            return applicableCharges;
        }

        public async Task<decimal> CalculateExtraCharges(int classId, int studentId, int campusId)
        {
            var applicableCharges = await GetApplicableCharges(classId, studentId, campusId);
            decimal totalExtraCharges = 0;

            foreach (var charge in applicableCharges)
            {
                bool shouldCharge = false;

                switch (charge.Category)
                {
                    case "MonthlyCharges":
                        // Always charged monthly
                        shouldCharge = true;
                        break;

                    case "OncePerLifetime":
                        // Check if student has ever paid this charge
                        var hasEverPaid = await _context.ClassFeeExtraChargePaymentHistories
                            .AnyAsync(ph => ph.StudentId == studentId && 
                                          ph.ClassFeeExtraChargeId == charge.Id);
                        shouldCharge = !hasEverPaid;
                        break;

                    case "OncePerClass":
                        // Check if student has paid this charge for current class
                        var hasPaidForThisClass = await _context.ClassFeeExtraChargePaymentHistories
                            .AnyAsync(ph => ph.StudentId == studentId && 
                                          ph.ClassFeeExtraChargeId == charge.Id &&
                                          ph.ClassIdPaidFor == classId);
                        shouldCharge = !hasPaidForThisClass;
                        break;
                }

                if (shouldCharge)
                {
                    totalExtraCharges += charge.Amount;
                }
            }

            return totalExtraCharges;
        }

        public async Task<bool> HasPaidCharge(int studentId, int chargeId, int? currentClassId = null)
        {
            var charge = await _context.ClassFeeExtraCharges.FindAsync(chargeId);
            if (charge == null) return false;

            switch (charge.Category)
            {
                case "OncePerLifetime":
                    return await _context.ClassFeeExtraChargePaymentHistories
                        .AnyAsync(ph => ph.StudentId == studentId && ph.ClassFeeExtraChargeId == chargeId);

                case "OncePerClass":
                    if (currentClassId.HasValue)
                    {
                        return await _context.ClassFeeExtraChargePaymentHistories
                            .AnyAsync(ph => ph.StudentId == studentId && 
                                          ph.ClassFeeExtraChargeId == chargeId &&
                                          ph.ClassIdPaidFor == currentClassId.Value);
                    }
                    return false;

                case "MonthlyCharges":
                default:
                    return false; // Always charge monthly
            }
        }

        public async Task SavePaymentHistory(int studentId, int chargeId, int billingMasterId, int classId, decimal amount, int campusId)
        {
            var charge = await _context.ClassFeeExtraCharges.FindAsync(chargeId);
            if (charge == null) return;

            // Only save history for OncePerClass and OncePerLifetime
            if (true)
            {
                var history = new ClassFeeExtraChargePaymentHistory
                {
                    StudentId = studentId,
                    ClassFeeExtraChargeId = chargeId,
                    ClassIdPaidFor = charge.Category == "OncePerClass" ? classId : null,
                    PaymentDate = DateTime.Now,
                    BillingMasterId = billingMasterId,
                    AmountPaid = amount,
                    CampusId = campusId
                };

                _context.ClassFeeExtraChargePaymentHistories.Add(history);
                await _context.SaveChangesAsync();
            }
        }
    }
}
