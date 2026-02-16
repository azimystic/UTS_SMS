using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;

namespace SMS.Helpers
{
    public static class ClassFeeHelper
    {
        /// <summary>
        /// Calculate total optional charges for a class
        /// </summary>
        public static async Task<decimal> GetOptionalChargesSumAsync(ApplicationDbContext context, int classId)
        {
            return await context.ClassFeeExtraCharges
                .Where(c => c.ClassId == classId && c.IsActive && !c.IsDeleted)
                .SumAsync(c => (decimal?)c.Amount) ?? 0;
        }
        
        /// <summary>
        /// Calculate optional charges for a specific student
        /// </summary>
        public static async Task<decimal> GetStudentOptionalChargesSumAsync(ApplicationDbContext context, int classId, int studentId)
        {
            var assignedCharges = await context.StudentChargeAssignments
                .Where(sca => sca.StudentId == studentId && sca.IsAssigned)
                .Join(context.ClassFeeExtraCharges,
                    sca => sca.ClassFeeExtraChargeId,
                    charge => charge.Id,
                    (sca, charge) => new { charge.ClassId, charge.Amount, charge.IsActive, charge.IsDeleted })
                .Where(x => x.ClassId == classId && x.IsActive && !x.IsDeleted)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;
                
            return assignedCharges;
        }
    }
}
