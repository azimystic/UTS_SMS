using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UTS_SMS.Models;
using System.Data;

namespace UTS_SMS
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<BillingTransaction> BillingTransactions { get; set; }
        public DbSet<BillingMaster> BillingMaster { get; set; }
        public DbSet<ClassFee> ClassFees { get; set; }
        public DbSet<Attendance> Attendance { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassSection> ClassSections { get; set; }
         public DbSet<Campus> Campuses { get; set; }
        public DbSet<TeacherAssignment> TeacherAssignments { get; set; }
        public DbSet<Diary> Diaries { get; set; }
        public DbSet<Timetable> Timetables { get; set; }
        public DbSet<TimetableSlot> TimetableSlots { get; set; }
        public DbSet<TimetableBreak> TimetableBreaks { get; set; }
        public DbSet<SubjectsGrouping> SubjectsGroupings { get; set; }
        public DbSet<SubjectsGroupingDetails> SubjectsGroupingDetails { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamCategory> ExamCategories { get; set; }
        public DbSet<ExamMarks> ExamMarks { get; set; }
        public DbSet<EmployeeAttendance> EmployeeAttendance { get; set; }
        public DbSet<SalaryDefinition> SalaryDefinitions { get; set; }
        public DbSet<PayrollMaster> PayrollMasters { get; set; }
        public DbSet<PayrollTransaction> PayrollTransactions { get; set; }
        public DbSet<AcademicYear> AcademicYear { get; set; }
        public DbSet<StudentHistory> StudentHistories { get; set; }
        public DbSet<NamazAttendance> NamazAttendance { get; set; }
        public DbSet<AcademicCalendar> AcademicCalendars { get; set; }
        public DbSet<DiaryImage> DiaryImages { get; set; }
        public DbSet<AdmissionInquiry> AdmissionInquiries { get; set; }
        public DbSet<AssignedDuty> AssignedDuties { get; set; }
        public DbSet<AssignedDutyStudent> AssignedDutyStudents { get; set; }
        public DbSet<StudentComplaint> StudentComplaints { get; set; }
        public DbSet<Family> Families { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
        public DbSet<StudentSurveyResponse> StudentSurveyResponses { get; set; }
        public DbSet<TestReturn> TestReturns { get; set; }
        public DbSet<TeacherPerformance> TeacherPerformances { get; set; }
        public DbSet<StudentChargeAssignment> StudentChargeAssignments { get; set; }
        public DbSet<ClassFeeExtraCharges> ClassFeeExtraCharges { get; set; }
        public DbSet<ClassFeeExtraChargeExclusion> ClassFeeExtraChargeExclusions { get; set; }
        public DbSet<ClassFeeExtraChargePaymentHistory> ClassFeeExtraChargePaymentHistories { get; set; }
        public DbSet<StudentFineCharge> StudentFineCharges { get; set; }
        public DbSet<FineChargeTemplate> FineChargeTemplates { get; set; }
        
        // Certificate Management System
        public DbSet<CertificateType> CertificateTypes { get; set; }
        public DbSet<CertificateRequest> CertificateRequests { get; set; }
        
        // ✅ ADD STUDENT CATEGORY TABLES
        public DbSet<StudentCategory> StudentCategories { get; set; }
        public DbSet<EmployeeCategoryDiscount> EmployeeCategoryDiscounts { get; set; }
        public DbSet<StudentCategoryAssignment> StudentCategoryAssignments { get; set; }
        public DbSet<PickupCard> PickupCards { get; set; }
        public DbSet<AdminNotification> AdminNotifications { get; set; }
        
        public DbSet<StudentMigration> StudentMigrations { get; set; }
        public DbSet<SalaryDeduction> SalaryDeductions { get; set; }
        
        // Exam Date Sheet Tables
        public DbSet<ExamDateSheet> ExamDateSheets { get; set; }
        public DbSet<ExamDateSheetClassSection> ExamDateSheetClassSections { get; set; }
        
        // Calendar Events
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        
        // Notifications
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotificationRead> UserNotificationReads { get; set; }
        // ToDo Items
        public DbSet<ToDo> ToDos { get; set; }
        
        // MailBox/Messaging System
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageRecipient> MessageRecipients { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        
     
        
        // Academic Material System
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<ChapterSection> ChapterSections { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<ChapterMaterial> ChapterMaterials { get; set; }
        // Substitutions
        public DbSet<Substitution> Substitutions { get; set; }
        
        // AI Chat System
        public DbSet<AiChatConversation> AiChatConversations { get; set; }
        public DbSet<AiChatMessage> AiChatMessages { get; set; }
        
        // Employee Roles and Leaves Management
        public DbSet<EmployeeRole> EmployeeRoles { get; set; }
        public DbSet<EmployeeRoleConfig> EmployeeRoleConfigs { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<LeaveConfig> LeaveConfigs { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveBalanceHistory> LeaveBalanceHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<StudentHistory>()
          .HasOne(sd => sd.Student)
          .WithMany()
          .HasForeignKey(sd => sd.StudentId)
          .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentHistory>()
          .HasOne(sd => sd.Exam)
          .WithMany()
          .HasForeignKey(sd => sd.ExamId)
          .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentHistory>()
          .HasOne(sd => sd.Class)
          .WithMany()
          .HasForeignKey(sd => sd.ClassId)
          .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<StudentHistory>()
          .HasOne(sd => sd.Section)
          .WithMany()
          .HasForeignKey(sd => sd.SectionId)
          .OnDelete(DeleteBehavior.Restrict);
          
            modelBuilder.Entity<PayrollMaster>()
                .HasOne(pm => pm.Campus)
                .WithMany()
                .HasForeignKey(pm => pm.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PayrollTransaction>()
                .HasOne(pt => pt.Campus)
                .WithMany()
                .HasForeignKey(pt => pt.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

             
            modelBuilder.Entity<SubjectsGrouping>()
           .HasOne(ta => ta.Campus)
           .WithMany()
           .HasForeignKey(ta => ta.CampusId)
           .OnDelete(DeleteBehavior.Restrict);
            // Create unique index to prevent duplicate attendance records
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.StudentId, a.Date, a.AcademicYear })
                .IsUnique();

            // Create a unique index on ClassLevel to prevent duplicates
            modelBuilder.Entity<ClassFee>()
                .HasIndex(c => c.ClassId)
                .IsUnique();
            modelBuilder.Entity<BillingTransaction>()
              .HasOne(ta => ta.Account)
              .WithMany()
              .HasForeignKey(ta => ta.OnlineAccount)
              .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<BillingTransaction>()
              .HasOne(ta => ta.Campus)
              .WithMany()
              .HasForeignKey(ta => ta.CampusId)
              .OnDelete(DeleteBehavior.Restrict);  // prevent cascade delete
                                                   // Configure relationships between ApplicationUser and Student/Employee
            modelBuilder.Entity<ApplicationUser>()
     .HasOne(u => u.Campus)
     .WithMany()
     .HasForeignKey(u => u.CampusId)
     .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Employee)
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Student)
                .WithMany()
                .HasForeignKey(u => u.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull);


            modelBuilder.Entity<Subject>()
           .HasIndex(s => s.Code)
           .IsUnique()
           .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<Class>()
                .HasIndex(c => new { c.Name, c.CampusId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<ClassSection>()
                .HasIndex(cs => new { cs.Name, cs.ClassId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            modelBuilder.Entity<ClassSection>()
               .HasOne(u => u.Campus)
               .WithMany()
               .HasForeignKey(u => u.CampusId)
               .OnDelete(DeleteBehavior.Restrict);
            // Ensure one active teacher assignment per subject-class-section
            modelBuilder.Entity<TeacherAssignment>()
                .HasIndex(ta => new { ta.SubjectId, ta.ClassId, ta.SectionId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            modelBuilder.Entity<TeacherAssignment>()
              .HasOne(ta => ta.Teacher)
              .WithMany()
              .HasForeignKey(ta => ta.TeacherId)
              .OnDelete(DeleteBehavior.Restrict);  // prevent cascade delete
            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.Class)
                .WithMany()
                .HasForeignKey(ta => ta.ClassId)
                .OnDelete(DeleteBehavior.Restrict);  // prevent cascade delete

            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.Section)
                .WithMany()
                .HasForeignKey(ta => ta.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeacherAssignment>()
                .HasOne(ta => ta.Subject)
                .WithMany()
                .HasForeignKey(ta => ta.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(ta => ta.ClassObj)
                .WithMany()
                .HasForeignKey(ta => ta.Class)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Student>()
               .HasOne(ta => ta.SectionObj)
               .WithMany()
               .HasForeignKey(ta => ta.Section)
               .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Chapter>()
        .HasOne(c => c.Campus)
        .WithMany()
        .HasForeignKey(c => c.CampusId)
        .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Student>()
             .HasOne(sgd => sgd.SubjectsGrouping)
             .WithMany()
             .HasForeignKey(sgd => sgd.SubjectsGroupingId)
             .OnDelete(DeleteBehavior.NoAction);
            // ExamCategory: CampusId can be -1 for "All Campuses", so no foreign key constraint
            modelBuilder.Entity<ExamCategory>(entity =>
            {
                // Note: CampusId = -1 means "All Campuses", no FK constraint
                entity.HasOne(ec => ec.Campus)
                    .WithMany()
                    .HasForeignKey(ec => ec.CampusId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Exam configurations
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasOne(e => e.ExamCategory)
                    .WithMany()
                    .HasForeignKey(e => e.ExamCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // CampusId = -1 means "All Campuses", so no FK constraint
                entity.HasOne(e => e.Campus)
                    .WithMany()
                    .HasForeignKey(e => e.CampusId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 📓 Diary: One diary per TeacherAssignment per date
            modelBuilder.Entity<Diary>()
                .HasIndex(d => new { d.TeacherAssignmentId, d.Date })
                .IsUnique();  // Only one diary per assignment per day

            // ✅ Set CreatedAt on Diary to default
            modelBuilder.Entity<Diary>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
            modelBuilder.Entity<ClassFee>()
           .HasOne(cf => cf.Class)
           .WithMany()
           .HasForeignKey(cf => cf.ClassId)
           .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Class>()
     .HasIndex(c => c.ClassTeacherId)
     .HasFilter("[IsActive] = 1 AND [ClassTeacherId] IS NOT NULL")
     .IsUnique();

            

            modelBuilder.Entity<Timetable>(entity =>
            {
                entity.HasIndex(t => new { t.ClassId, t.SectionId })
                      .IsUnique()
                      .HasFilter("[IsActive] = 1");

                entity.HasOne(t => t.Class)
                      .WithMany()
                      .HasForeignKey(t => t.ClassId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(t => t.Section)
                      .WithMany()
                      .HasForeignKey(t => t.SectionId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // TimetableSlot configuration
            modelBuilder.Entity<TimetableSlot>(entity =>
            {
                entity.HasOne(ts => ts.Timetable)
                      .WithMany(t => t.TimetableSlots)
                      .HasForeignKey(ts => ts.TimetableId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(ts => ts.TeacherAssignment)
                      .WithMany()
                      .HasForeignKey(ts => ts.TeacherAssignmentId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // TimetableBreak configuration
            modelBuilder.Entity<TimetableBreak>(entity =>
            {
                entity.HasOne(tb => tb.Timetable)
                      .WithMany(t => t.TimetableBreaks)
                      .HasForeignKey(tb => tb.TimetableId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            // In OnModelCreating method:
            modelBuilder.Entity<SubjectsGroupingDetails>()
                .HasKey(sgd => new { sgd.SubjectId, sgd.SubjectsGroupingId });

            modelBuilder.Entity<SubjectsGroupingDetails>()
                .HasOne(sgd => sgd.Subject)
                .WithMany()
                .HasForeignKey(sgd => sgd.SubjectId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Class>()
          .HasOne(sgd => sgd.ClassTeacher)
          .WithMany()
          .HasForeignKey(sgd => sgd.ClassTeacherId)
          .OnDelete(DeleteBehavior.NoAction);

            // Configure ExamMarks entity
            modelBuilder.Entity<ExamMarks>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ObtainedMarks).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalMarks).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PassingMarks).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Percentage).HasColumnType("decimal(5,2)");

                // Configure relationships
                entity.HasOne(e => e.Exam)
                    .WithMany()
                    .HasForeignKey(e => e.ExamId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Subject)
                    .WithMany()
                    .HasForeignKey(e => e.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Class)
                    .WithMany()
                    .HasForeignKey(e => e.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Section)
                    .WithMany()
                    .HasForeignKey(e => e.SectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeAttendance>()
               .HasOne(sgd => sgd.Employee)
               .WithMany()
               .HasForeignKey(sgd => sgd.EmployeeId)
               .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Attendance>()
              .HasOne(sgd => sgd.Student)
              .WithMany()
              .HasForeignKey(sgd => sgd.StudentId)
              .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Attendance>()
            .HasOne(sgd => sgd.ClassObj)
            .WithMany()
            .HasForeignKey(sgd => sgd.ClassId)
            .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Attendance>()
            .HasOne(sgd => sgd.SectionObj)
            .WithMany()
            .HasForeignKey(sgd => sgd.SectionId)
            .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<BillingMaster>()
             .HasOne(sgd => sgd.Student)
             .WithMany()
             .HasForeignKey(sgd => sgd.StudentId)
             .OnDelete(DeleteBehavior.NoAction);
            
            modelBuilder.Entity<BillingMaster>()
             .HasOne(bm => bm.ClassObj)
             .WithMany()
             .HasForeignKey(bm => bm.ClassId)
             .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Diary>()
            .HasOne(sgd => sgd.TeacherAssignment)
            .WithMany()
            .HasForeignKey(sgd => sgd.TeacherAssignmentId)
            .OnDelete(DeleteBehavior.NoAction);

            // NamazAttendance configurations
            modelBuilder.Entity<NamazAttendance>()
                .HasIndex(na => new { na.StudentId, na.Date, na.AcademicYear })
                .HasFilter("[StudentId] IS NOT NULL");

            modelBuilder.Entity<NamazAttendance>()
                .HasIndex(na => new { na.EmployeeId, na.Date, na.AcademicYear })
                .HasFilter("[EmployeeId] IS NOT NULL");

            modelBuilder.Entity<NamazAttendance>()
                .HasOne(na => na.Student)
                .WithMany()
                .HasForeignKey(na => na.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<NamazAttendance>()
                .HasOne(na => na.Employee)
                .WithMany()
                .HasForeignKey(na => na.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<NamazAttendance>()
                .HasOne(na => na.Campus)
                .WithMany()
                .HasForeignKey(na => na.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // AcademicCalendar configurations
            modelBuilder.Entity<AcademicCalendar>()
                .HasIndex(ac => new { ac.CampusId, ac.Date })
                .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<AcademicCalendar>()
                .HasOne(ac => ac.Campus)
                .WithMany()
                .HasForeignKey(ac => ac.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // DiaryImage configurations
            modelBuilder.Entity<DiaryImage>()
                .HasOne(di => di.Diary)
                .WithMany(d => d.DiaryImages)
                .HasForeignKey(di => di.DiaryId)
                .OnDelete(DeleteBehavior.Cascade); // Allow cascade delete for images when diary is deleted

            // AdmissionInquiry configurations
            modelBuilder.Entity<AdmissionInquiry>()
                .HasOne(ai => ai.ClassInterested)
                .WithMany()
                .HasForeignKey(ai => ai.ClassInterestedId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdmissionInquiry>()
                .HasOne(ai => ai.Campus)
                .WithMany()
                .HasForeignKey(ai => ai.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // AssignedDuty configurations
            modelBuilder.Entity<AssignedDuty>()
                .HasOne(ad => ad.Employee)
                .WithMany()
                .HasForeignKey(ad => ad.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AssignedDuty>()
                .HasOne(ad => ad.Campus)
                .WithMany()
                .HasForeignKey(ad => ad.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentComplaint configurations
            modelBuilder.Entity<StudentComplaint>()
                .HasOne(sc => sc.Student)
                .WithMany()
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentComplaint>()
                .HasOne(sc => sc.Campus)
                .WithMany()
                .HasForeignKey(sc => sc.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Family configurations
            modelBuilder.Entity<Family>()
                .HasIndex(f => f.FatherCNIC)
                .HasFilter("[IsActive] = 1");

            modelBuilder.Entity<Family>()
                .HasOne(f => f.Campus)
                .WithMany()
                .HasForeignKey(f => f.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Student-Family relationship
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Family)
                .WithMany(f => f.Students)
                .HasForeignKey(s => s.FamilyId)
                .OnDelete(DeleteBehavior.SetNull);

            // Asset configurations
            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Account)
                .WithMany()
                .HasForeignKey(a => a.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Asset>()
                .HasOne(a => a.Campus)
                .WithMany()
                .HasForeignKey(a => a.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Expense configurations
            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Campus)
                .WithMany()
                .HasForeignKey(e => e.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // SurveyQuestion configurations
            modelBuilder.Entity<SurveyQuestion>()
                .HasOne(sq => sq.Campus)
                .WithMany()
                .HasForeignKey(sq => sq.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentSurveyResponse configurations
            modelBuilder.Entity<StudentSurveyResponse>()
                .HasIndex(ssr => new { ssr.StudentId, ssr.SurveyQuestionId, ssr.TeacherId })
                .IsUnique(); // One response per student per question per teacher

            modelBuilder.Entity<StudentSurveyResponse>()
                .HasOne(ssr => ssr.Student)
                .WithMany()
                .HasForeignKey(ssr => ssr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentSurveyResponse>()
                .HasOne(ssr => ssr.SurveyQuestion)
                .WithMany(sq => sq.StudentResponses)
                .HasForeignKey(ssr => ssr.SurveyQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentSurveyResponse>()
                .HasOne(ssr => ssr.Campus)
                .WithMany()
                .HasForeignKey(ssr => ssr.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentSurveyResponse>()
                .HasOne(ssr => ssr.Teacher)
                .WithMany()
                .HasForeignKey(ssr => ssr.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            // TestReturn configurations
            modelBuilder.Entity<TestReturn>()
                .HasOne(tr => tr.Exam)
                .WithMany()
                .HasForeignKey(tr => tr.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestReturn>()
                .HasOne(tr => tr.Subject)
                .WithMany()
                .HasForeignKey(tr => tr.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestReturn>()
                .HasOne(tr => tr.Class)
                .WithMany()
                .HasForeignKey(tr => tr.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestReturn>()
                .HasOne(tr => tr.Section)
                .WithMany()
                .HasForeignKey(tr => tr.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestReturn>()
                .HasOne(tr => tr.Teacher)
                .WithMany()
                .HasForeignKey(tr => tr.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestReturn>()
                .HasOne(tr => tr.Campus)
                .WithMany()
                .HasForeignKey(tr => tr.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // TeacherPerformance configurations
            modelBuilder.Entity<TeacherPerformance>()
                .HasIndex(tp => new { tp.TeacherId, tp.Month, tp.Year })
                .IsUnique(); // One performance record per teacher per month

            modelBuilder.Entity<TeacherPerformance>()
                .HasOne(tp => tp.Teacher)
                .WithMany()
                .HasForeignKey(tp => tp.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TeacherPerformance>()
                .HasOne(tp => tp.Campus)
                .WithMany()
                .HasForeignKey(tp => tp.CampusId)
                .OnDelete(DeleteBehavior.Restrict);
       
            // ClassFeeExtraCharges configurations
            modelBuilder.Entity<ClassFeeExtraCharges>()
                .HasOne(cfec => cfec.Class)
                .WithMany()
                .HasForeignKey(cfec => cfec.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassFeeExtraCharges>()
                .HasOne(cfec => cfec.Campus)
                .WithMany()
                .HasForeignKey(cfec => cfec.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // ClassFeeExtraChargeExclusion configurations
            modelBuilder.Entity<ClassFeeExtraChargeExclusion>()
                .HasOne(cfece => cfece.ClassFeeExtraCharge)
                .WithMany(cfec => cfec.ExcludedStudents)
                .HasForeignKey(cfece => cfece.ClassFeeExtraChargeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassFeeExtraChargeExclusion>()
                .HasOne(cfece => cfece.Student)
                .WithMany()
                .HasForeignKey(cfece => cfece.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassFeeExtraChargeExclusion>()
                .HasOne(cfece => cfece.Campus)
                .WithMany()
                .HasForeignKey(cfece => cfece.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // ClassFeeExtraChargePaymentHistory configurations
            modelBuilder.Entity<ClassFeeExtraChargePaymentHistory>()
                .HasOne(cfecph => cfecph.Student)
                .WithMany()
                .HasForeignKey(cfecph => cfecph.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassFeeExtraChargePaymentHistory>()
                .HasOne(cfecph => cfecph.ClassFeeExtraCharge)
                .WithMany()
                .HasForeignKey(cfecph => cfecph.ClassFeeExtraChargeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassFeeExtraChargePaymentHistory>()
                .HasOne(cfecph => cfecph.ClassPaidFor)
                .WithMany()
                .HasForeignKey(cfecph => cfecph.ClassIdPaidFor)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassFeeExtraChargePaymentHistory>()
                .HasOne(cfecph => cfecph.BillingMaster)
                .WithMany()
                .HasForeignKey(cfecph => cfecph.BillingMasterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClassFeeExtraChargePaymentHistory>()
                .HasOne(cfecph => cfecph.Campus)
                .WithMany()
                .HasForeignKey(cfecph => cfecph.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentChargeAssignment configurations
            modelBuilder.Entity<StudentChargeAssignment>()
                .HasIndex(sca => new { sca.StudentId, sca.ClassFeeExtraChargeId })
                .IsUnique();

            modelBuilder.Entity<StudentChargeAssignment>()
                .HasOne(sca => sca.Student)
                .WithMany()
                .HasForeignKey(sca => sca.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentChargeAssignment>()
                .HasOne(sca => sca.ClassFeeExtraCharge)
                .WithMany()
                .HasForeignKey(sca => sca.ClassFeeExtraChargeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentChargeAssignment>()
                .HasOne(sca => sca.Campus)
                .WithMany()
                .HasForeignKey(sca => sca.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            // StudentFineCharge configurations
            modelBuilder.Entity<StudentFineCharge>()
                .HasOne(sfc => sfc.Student)
                .WithMany()
                .HasForeignKey(sfc => sfc.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentFineCharge>()
                .HasOne(sfc => sfc.Campus)
                .WithMany()
                .HasForeignKey(sfc => sfc.CampusId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentFineCharge>()
                .HasOne(sfc => sfc.BillingMaster)
                .WithMany()
                .HasForeignKey(sfc => sfc.BillingMasterId)
                .OnDelete(DeleteBehavior.SetNull);

            // FineChargeTemplate configurations
            modelBuilder.Entity<FineChargeTemplate>()
                .HasOne(fct => fct.Campus)
                .WithMany()
                .HasForeignKey(fct => fct.CampusId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // ✅ ADD STUDENT CATEGORY CONFIGURATIONS
            
            // StudentCategory configurations
            modelBuilder.Entity<StudentCategory>()
                .HasOne(sc => sc.Campus)
                .WithMany()
                .HasForeignKey(sc => sc.CampusId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // EmployeeCategoryDiscount configurations
            modelBuilder.Entity<EmployeeCategoryDiscount>()
                .HasOne(ecd => ecd.StudentCategory)
                .WithMany(sc => sc.EmployeeCategoryDiscounts)
                .HasForeignKey(ecd => ecd.StudentCategoryId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // StudentCategoryAssignment configurations
            modelBuilder.Entity<StudentCategoryAssignment>()
                .HasOne(sca => sca.Student)
                .WithMany()
                .HasForeignKey(sca => sca.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<StudentCategoryAssignment>()
                .HasOne(sca => sca.StudentCategory)
                .WithMany()
                .HasForeignKey(sca => sca.StudentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<StudentCategoryAssignment>()
                .HasOne(sca => sca.Employee)
                .WithMany()
                .HasForeignKey(sca => sca.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // StudentMigration configurations
            modelBuilder.Entity<StudentMigration>()
                .HasOne(sm => sm.Student)
                .WithMany()
                .HasForeignKey(sm => sm.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<StudentMigration>()
                .HasOne(sm => sm.FromCampus)
                .WithMany()
                .HasForeignKey(sm => sm.FromCampusId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<StudentMigration>()
                .HasOne(sm => sm.ToCampus)
                .WithMany()
                .HasForeignKey(sm => sm.ToCampusId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // SalaryDeduction configurations
            modelBuilder.Entity<SalaryDeduction>()
                .HasOne(sd => sd.Student)
                .WithMany()
                .HasForeignKey(sd => sd.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<SalaryDeduction>()
                .HasOne(sd => sd.Employee)
                .WithMany()
                .HasForeignKey(sd => sd.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<SalaryDeduction>()
                .HasOne(sd => sd.BillingMaster)
                .WithMany()
                .HasForeignKey(sd => sd.BillingMasterId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<SalaryDeduction>()
                .HasOne(sd => sd.Campus)
                .WithMany()
                .HasForeignKey(sd => sd.CampusId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // AdminNotification configurations
            modelBuilder.Entity<AdminNotification>()
                .HasOne(an => an.Student)
                .WithMany()
                .HasForeignKey(an => an.StudentId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<AdminNotification>()
                .HasOne(an => an.PickupCard)
                .WithMany()
                .HasForeignKey(an => an.PickupCardId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<AdminNotification>()
                .HasOne(an => an.Campus)
                .WithMany()
                .HasForeignKey(an => an.CampusId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // ExamDateSheet configurations
            modelBuilder.Entity<ExamDateSheet>()
                .HasOne(eds => eds.ExamCategory)
                .WithMany()
                .HasForeignKey(eds => eds.ExamCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ExamDateSheet>()
                .HasOne(eds => eds.Exam)
                .WithMany()
                .HasForeignKey(eds => eds.ExamId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ExamDateSheet>()
                .HasOne(eds => eds.Subject)
                .WithMany()
                .HasForeignKey(eds => eds.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ExamDateSheet>()
                .HasOne(eds => eds.Campus)
                .WithMany()
                .HasForeignKey(eds => eds.CampusId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // ExamDateSheetClassSection configurations
            modelBuilder.Entity<ExamDateSheetClassSection>()
                .HasOne(edscs => edscs.ExamDateSheet)
                .WithMany(eds => eds.ClassSections)
                .HasForeignKey(edscs => edscs.ExamDateSheetId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<ExamDateSheetClassSection>()
                .HasOne(edscs => edscs.Class)
                .WithMany()
                .HasForeignKey(edscs => edscs.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ExamDateSheetClassSection>()
                .HasOne(edscs => edscs.Section)
                .WithMany()
                .HasForeignKey(edscs => edscs.SectionId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Notification configurations
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Campus)
                .WithMany()
                .HasForeignKey(n => n.CampusId)
                .OnDelete(DeleteBehavior.Restrict);
                
            
        }
    }
}