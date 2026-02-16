using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTS_SMS.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicYear",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYear", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Longitudes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Latitudes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Logo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AcademicCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HolidayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HolidayType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicCalendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicCalendars_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsHoliday = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "CertificateTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReportFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateTypes_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRoleConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRoleConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRoleConfigs_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CNIC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EducationLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Degree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MajorSubject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatricMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InterMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CGPA = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    University = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CNIC_Front = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CNIC_Back = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatricCertificate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterCertificate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DegreeCertificate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherQualifications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JoiningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeavingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OnTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    OffTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    LateTimeFlexibility = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "ExamCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamCategories_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Families",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FatherCNIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FatherPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    MotherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MotherCNIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MotherPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    HomeAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FatherSourceOfIncome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFatherDeceased = table.Column<bool>(type: "bit", nullable: false),
                    GuardianName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GuardianPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Families_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FineChargeTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FineChargeTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FineChargeTemplates_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllocationPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllowedDays = table.Column<int>(type: "int", nullable: false),
                    IsCarryForward = table.Column<bool>(type: "bit", nullable: false),
                    MaxCarryForwardDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveConfigs_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "StudentCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsSystemDefined = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    DefaultAdmissionFeeDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DefaultTuitionFeeDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SiblingCount = table.Column<int>(type: "int", nullable: true),
                    PerSiblingAdmissionDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PerSiblingTuitionDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCategories_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subjects_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "SubjectsGroupings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectsGroupings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectsGroupings_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SurveyQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuestionOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyQuestions_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Condition = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_BankAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Assets_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_BankAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Expenses_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignedDuties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    DutyTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DutyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecurrenceInterval = table.Column<int>(type: "int", nullable: true),
                    NextDueDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedDuties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignedDuties_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssignedDuties_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassTeacherId = table.Column<int>(type: "int", nullable: true),
                    GradeLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentAcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classes_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Classes_Employees_ClassTeacherId",
                        column: x => x.ClassTeacherId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeRoleConfigId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRoles_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_EmployeeRoles_EmployeeRoleConfigs_EmployeeRoleConfigId",
                        column: x => x.EmployeeRoleConfigId,
                        principalTable: "EmployeeRoleConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_EmployeeRoles_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: true),
                    TotalAllocated = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Used = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CarriedForward = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "PayrollMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ForMonth = table.Column<int>(type: "int", nullable: false),
                    ForYear = table.Column<int>(type: "int", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Allowances = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Bonus = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AttendanceDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollMasters_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollMasters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "SalaryDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HouseRentAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MedicalAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransportationAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherAllowances = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProvidentFund = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryDefinitions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "TeacherPerformances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AttendanceScore = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    PunctualityScore = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    TestAverageScore = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    SurveyScore = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    TestReturnScore = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    CheckingQualityScore = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    TotalWorkingDays = table.Column<int>(type: "int", nullable: false),
                    AttendedDays = table.Column<int>(type: "int", nullable: false),
                    OnTimeDays = table.Column<int>(type: "int", nullable: false),
                    AverageTestMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalSurveyResponses = table.Column<int>(type: "int", nullable: false),
                    PositiveSurveyResponses = table.Column<int>(type: "int", nullable: false),
                    TestsReturnedOnTime = table.Column<int>(type: "int", nullable: false),
                    TotalTestsToReturn = table.Column<int>(type: "int", nullable: false),
                    GoodCheckingCount = table.Column<int>(type: "int", nullable: false),
                    BetterCheckingCount = table.Column<int>(type: "int", nullable: false),
                    BadCheckingCount = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherPerformances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherPerformances_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherPerformances_Employees_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExamCategoryId = table.Column<int>(type: "int", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exams_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Exams_ExamCategories_ExamCategoryId",
                        column: x => x.ExamCategoryId,
                        principalTable: "ExamCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCategoryDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentCategoryId = table.Column<int>(type: "int", nullable: false),
                    EmployeeCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AdmissionFeeDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TuitionFeeDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCategoryDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCategoryDiscounts_StudentCategories_StudentCategoryId",
                        column: x => x.StudentCategoryId,
                        principalTable: "StudentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChapterNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Chapters_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "SubjectsGroupingDetails",
                columns: table => new
                {
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    SubjectsGroupingId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectsGroupingDetails", x => new { x.SubjectId, x.SubjectsGroupingId });
                    table.ForeignKey(
                        name: "FK_SubjectsGroupingDetails_SubjectsGroupings_SubjectsGroupingId",
                        column: x => x.SubjectsGroupingId,
                        principalTable: "SubjectsGroupings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_SubjectsGroupingDetails_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AdmissionInquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClassInterestedId = table.Column<int>(type: "int", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    PreviousSchool = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InquiryStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InquiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FollowUpRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionInquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionInquiries_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionInquiries_Classes_ClassInterestedId",
                        column: x => x.ClassInterestedId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassFeeExtraCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassFeeExtraCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraCharges_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraCharges_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    TuitionFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdmissionFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassFees_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ClassFees_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    CurrentAcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassSections_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSections_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalanceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LeaveRequestId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalanceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceHistories_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceHistories_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAttendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    IsLocationValid = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayrollMasterId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendance_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAttendance_PayrollMasters_PayrollMasterId",
                        column: x => x.PayrollMasterId,
                        principalTable: "PayrollMasters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayrollTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollMasterId = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OnlinePaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OnlineAccount = table.Column<int>(type: "int", nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollTransactions_BankAccounts_OnlineAccount",
                        column: x => x.OnlineAccount,
                        principalTable: "BankAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollTransactions_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollTransactions_PayrollMasters_PayrollMasterId",
                        column: x => x.PayrollMasterId,
                        principalTable: "PayrollMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "ExamDateSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExamCategoryId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    TotalMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PassingMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamDateSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamDateSheets_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamDateSheets_ExamCategories_ExamCategoryId",
                        column: x => x.ExamCategoryId,
                        principalTable: "ExamCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamDateSheets_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamDateSheets_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChapterSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterSections_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentCNIC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FatherCNIC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotherName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotherCNIC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MotherPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Class = table.Column<int>(type: "int", nullable: false),
                    Section = table.Column<int>(type: "int", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    SubjectsGroupingId = table.Column<int>(type: "int", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FatherPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HomeAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFatherDeceased = table.Column<bool>(type: "bit", nullable: false),
                    GuardianName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuardianPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FatherSourceOfIncome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousSchool = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TuitionFeeDiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AdmissionFeeDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StudentCategoryId = table.Column<int>(type: "int", nullable: true),
                    MatricRollNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterRollNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RollNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PersonalTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FatherCNIC_Front = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FatherCNIC_Back = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BForm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentCNIC_Front = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentCNIC_Back = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatricCertificate_01 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterCertificate_01 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatricCertificate_02 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterCertificate_02 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdmissionFeePaid = table.Column<bool>(type: "bit", nullable: false),
                    HasLeft = table.Column<bool>(type: "bit", nullable: false),
                    LeftDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegisteredBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Students_ClassSections_Section",
                        column: x => x.Section,
                        principalTable: "ClassSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Classes_Class",
                        column: x => x.Class,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Students_StudentCategories_StudentCategoryId",
                        column: x => x.StudentCategoryId,
                        principalTable: "StudentCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Students_SubjectsGroupings_SubjectsGroupingId",
                        column: x => x.SubjectsGroupingId,
                        principalTable: "SubjectsGroupings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeacherAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ClassId1 = table.Column<int>(type: "int", nullable: true),
                    ClassSectionId = table.Column<int>(type: "int", nullable: true),
                    SubjectId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_ClassSections_ClassSectionId",
                        column: x => x.ClassSectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_ClassSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Classes_ClassId1",
                        column: x => x.ClassId1,
                        principalTable: "Classes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Employees_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Subjects_SubjectId1",
                        column: x => x.SubjectId1,
                        principalTable: "Subjects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TestReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: false),
                    ExamDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsReturnedOnTime = table.Column<bool>(type: "bit", nullable: false),
                    CheckingQuality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestReturns_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestReturns_ClassSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestReturns_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestReturns_Employees_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestReturns_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestReturns_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Timetables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfLectures = table.Column<int>(type: "int", nullable: false),
                    LectureDuration = table.Column<int>(type: "int", nullable: false),
                    BreakDuration = table.Column<int>(type: "int", nullable: false),
                    BreakAfterPeriod = table.Column<int>(type: "int", nullable: false),
                    ZeroPeriodDuration = table.Column<int>(type: "int", nullable: false),
                    ZeroPeriodStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BreakStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timetables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timetables_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Timetables_ClassSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Timetables_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamDateSheetClassSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamDateSheetId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamDateSheetClassSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamDateSheetClassSections_ClassSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamDateSheetClassSections_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamDateSheetClassSections_ExamDateSheets_ExamDateSheetId",
                        column: x => x.ExamDateSheetId,
                        principalTable: "ExamDateSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "ChapterMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    ChapterSectionId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Heading = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterMaterials_ChapterSections_ChapterSectionId",
                        column: x => x.ChapterSectionId,
                        principalTable: "ChapterSections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ChapterMaterials_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChapterId = table.Column<int>(type: "int", nullable: false),
                    ChapterSectionId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OptionA = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionB = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionD = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrectOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_ChapterSections_ChapterSectionId",
                        column: x => x.ChapterSectionId,
                        principalTable: "ChapterSections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Questions_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    FamilyId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssignedDutyStudents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedDutyId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StudentNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignedDutyStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignedDutyStudents_AssignedDuties_AssignedDutyId",
                        column: x => x.AssignedDutyId,
                        principalTable: "AssignedDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_AssignedDutyStudents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendance_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Attendance_ClassSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attendance_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attendance_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BillingMaster",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    ForMonth = table.Column<int>(type: "int", nullable: false),
                    ForYear = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Dues = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TuitionFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MiscallaneousCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdmissionFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fine = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousDues = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemarksPreviousDues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAmountPayable = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingMaster_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_BillingMaster_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingMaster_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClassFeeExtraChargeExclusions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassFeeExtraChargeId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ExcludedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExcludedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassFeeExtraChargeExclusions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraChargeExclusions_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraChargeExclusions_ClassFeeExtraCharges_ClassFeeExtraChargeId",
                        column: x => x.ClassFeeExtraChargeId,
                        principalTable: "ClassFeeExtraCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraChargeExclusions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamMarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    TotalMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PassingMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ObtainedMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ExamDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamMarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamMarks_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ExamMarks_ClassSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMarks_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMarks_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMarks_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamMarks_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamazAttendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamazAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamazAttendance_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NamazAttendance_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NamazAttendance_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PickupCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CNIC = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Relation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PersonPicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CNICPicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickupCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PickupCards_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "StudentCategoryAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudentCategoryId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PaymentMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomAdmissionPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CustomTuitionPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AppliedAdmissionFeeDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedTuitionFeeDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCategoryAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCategoryAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentCategoryAssignments_StudentCategories_StudentCategoryId",
                        column: x => x.StudentCategoryId,
                        principalTable: "StudentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentCategoryAssignments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentChargeAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ClassFeeExtraChargeId = table.Column<int>(type: "int", nullable: false),
                    IsAssigned = table.Column<bool>(type: "bit", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentChargeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentChargeAssignments_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentChargeAssignments_ClassFeeExtraCharges_ClassFeeExtraChargeId",
                        column: x => x.ClassFeeExtraChargeId,
                        principalTable: "ClassFeeExtraCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentChargeAssignments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentComplaints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ComplaintTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ComplaintDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ComplaintType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ComplaintDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReporterType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReporterPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReporterEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvestigationNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolutionDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolutionComments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TeacherId = table.Column<int>(type: "int", nullable: true),
                    TeacherComments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TeacherCommentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SatisfactionLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FeedbackComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    RequiresParentNotification = table.Column<bool>(type: "bit", nullable: false),
                    ParentNotified = table.Column<bool>(type: "bit", nullable: false),
                    ParentNotificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FollowUpActions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NextFollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentComplaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentComplaints_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentComplaints_Employees_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentComplaints_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    Award = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    FinalPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ComputedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComputedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentHistories_ClassSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ClassSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentHistories_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentHistories_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentHistories_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentMigrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    FromCampusId = table.Column<int>(type: "int", nullable: false),
                    ToCampusId = table.Column<int>(type: "int", nullable: false),
                    FromClassId = table.Column<int>(type: "int", nullable: false),
                    FromSectionId = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OutstandingDues = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentMigrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentMigrations_Campuses_FromCampusId",
                        column: x => x.FromCampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMigrations_Campuses_ToCampusId",
                        column: x => x.ToCampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentMigrations_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentSurveyResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SurveyQuestionId = table.Column<int>(type: "int", nullable: false),
                    Response = table.Column<bool>(type: "bit", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    TeacherId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSurveyResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSurveyResponses_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentSurveyResponses_Employees_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentSurveyResponses_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentSurveyResponses_SurveyQuestions_SurveyQuestionId",
                        column: x => x.SurveyQuestionId,
                        principalTable: "SurveyQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Diaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherAssignmentId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LessonSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HomeworkGiven = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    ChapterId = table.Column<int>(type: "int", nullable: true),
                    ChapterSectionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diaries_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Diaries_ChapterSections_ChapterSectionId",
                        column: x => x.ChapterSectionId,
                        principalTable: "ChapterSections",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Diaries_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Diaries_TeacherAssignments_TeacherAssignmentId",
                        column: x => x.TeacherAssignmentId,
                        principalTable: "TeacherAssignments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TimetableBreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimetableId = table.Column<int>(type: "int", nullable: false),
                    AfterPeriod = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetableBreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimetableBreaks_Timetables_TimetableId",
                        column: x => x.TimetableId,
                        principalTable: "Timetables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "TimetableSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimetableId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TeacherAssignmentId = table.Column<int>(type: "int", nullable: true),
                    IsBreak = table.Column<bool>(type: "bit", nullable: false),
                    IsZeroPeriod = table.Column<bool>(type: "bit", nullable: false),
                    CustomTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetableSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimetableSlots_TeacherAssignments_TeacherAssignmentId",
                        column: x => x.TeacherAssignmentId,
                        principalTable: "TeacherAssignments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimetableSlots_Timetables_TimetableId",
                        column: x => x.TimetableId,
                        principalTable: "Timetables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Messages_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Notifications_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToDos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToDos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToDos_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ToDos_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BillingTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillingMasterId = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OnlinePaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OnlineAccount = table.Column<int>(type: "int", nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingTransactions_BankAccounts_OnlineAccount",
                        column: x => x.OnlineAccount,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingTransactions_BillingMaster_BillingMasterId",
                        column: x => x.BillingMasterId,
                        principalTable: "BillingMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_BillingTransactions_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassFeeExtraChargePaymentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ClassFeeExtraChargeId = table.Column<int>(type: "int", nullable: false),
                    ClassIdPaidFor = table.Column<int>(type: "int", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillingMasterId = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassFeeExtraChargePaymentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraChargePaymentHistories_BillingMaster_BillingMasterId",
                        column: x => x.BillingMasterId,
                        principalTable: "BillingMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraChargePaymentHistories_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraChargePaymentHistories_ClassFeeExtraCharges_ClassFeeExtraChargeId",
                        column: x => x.ClassFeeExtraChargeId,
                        principalTable: "ClassFeeExtraCharges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraChargePaymentHistories_Classes_ClassIdPaidFor",
                        column: x => x.ClassIdPaidFor,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassFeeExtraChargePaymentHistories_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryDeductions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    BillingMasterId = table.Column<int>(type: "int", nullable: false),
                    AmountDeducted = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ForMonth = table.Column<int>(type: "int", nullable: false),
                    ForYear = table.Column<int>(type: "int", nullable: false),
                    DeductionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryDeductions_BillingMaster_BillingMasterId",
                        column: x => x.BillingMasterId,
                        principalTable: "BillingMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryDeductions_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryDeductions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryDeductions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentFineCharges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ChargeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChargeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillingMasterId = table.Column<int>(type: "int", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFineCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentFineCharges_BillingMaster_BillingMasterId",
                        column: x => x.BillingMasterId,
                        principalTable: "BillingMaster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentFineCharges_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentFineCharges_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdminNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    PickupCardId = table.Column<int>(type: "int", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminNotifications_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminNotifications_PickupCards_PickupCardId",
                        column: x => x.PickupCardId,
                        principalTable: "PickupCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdminNotifications_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DiaryImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiaryId = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaryImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiaryImages_Diaries_DiaryId",
                        column: x => x.DiaryId,
                        principalTable: "Diaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Substitutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimetableSlotId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalTeacherId = table.Column<int>(type: "int", nullable: false),
                    SubstituteEmployeeId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Substitutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Substitutions_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Substitutions_Employees_OriginalTeacherId",
                        column: x => x.OriginalTeacherId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Substitutions_Employees_SubstituteEmployeeId",
                        column: x => x.SubstituteEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Substitutions_TimetableSlots_TimetableSlotId",
                        column: x => x.TimetableSlotId,
                        principalTable: "TimetableSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "MessageAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageAttachments_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "MessageRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageRecipients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_MessageRecipients_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationReads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationReads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationReads_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_UserNotificationReads_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    CertificateTypeId = table.Column<int>(type: "int", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    GeneratedFineId = table.Column<int>(type: "int", nullable: true),
                    CampusId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateRequests_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_CertificateRequests_CertificateTypes_CertificateTypeId",
                        column: x => x.CertificateTypeId,
                        principalTable: "CertificateTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_CertificateRequests_StudentFineCharges_GeneratedFineId",
                        column: x => x.GeneratedFineId,
                        principalTable: "StudentFineCharges",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CertificateRequests_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicCalendars_CampusId_Date",
                table: "AcademicCalendars",
                columns: new[] { "CampusId", "Date" },
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_CampusId",
                table: "AdminNotifications",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_PickupCardId",
                table: "AdminNotifications",
                column: "PickupCardId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminNotifications_StudentId",
                table: "AdminNotifications",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionInquiries_CampusId",
                table: "AdmissionInquiries",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionInquiries_ClassInterestedId",
                table: "AdmissionInquiries",
                column: "ClassInterestedId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CampusId",
                table: "AspNetUsers",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmployeeId",
                table: "AspNetUsers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_FamilyId",
                table: "AspNetUsers",
                column: "FamilyId",
                unique: true,
                filter: "[FamilyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StudentId",
                table: "AspNetUsers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AccountId",
                table: "Assets",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CampusId",
                table: "Assets",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedDuties_CampusId",
                table: "AssignedDuties",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedDuties_EmployeeId",
                table: "AssignedDuties",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedDutyStudents_AssignedDutyId",
                table: "AssignedDutyStudents",
                column: "AssignedDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignedDutyStudents_StudentId",
                table: "AssignedDutyStudents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_CampusId",
                table: "Attendance",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_ClassId",
                table: "Attendance",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_SectionId",
                table: "Attendance",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_StudentId_Date_AcademicYear",
                table: "Attendance",
                columns: new[] { "StudentId", "Date", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_CampusId",
                table: "BankAccounts",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingMaster_CampusId",
                table: "BillingMaster",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingMaster_ClassId",
                table: "BillingMaster",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingMaster_StudentId",
                table: "BillingMaster",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingTransactions_BillingMasterId",
                table: "BillingTransactions",
                column: "BillingMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingTransactions_CampusId",
                table: "BillingTransactions",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingTransactions_OnlineAccount",
                table: "BillingTransactions",
                column: "OnlineAccount");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_CampusId",
                table: "CalendarEvents",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_CampusId",
                table: "CertificateRequests",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_CertificateTypeId",
                table: "CertificateRequests",
                column: "CertificateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_GeneratedFineId",
                table: "CertificateRequests",
                column: "GeneratedFineId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_StudentId",
                table: "CertificateRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTypes_CampusId",
                table: "CertificateTypes",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterMaterials_ChapterId",
                table: "ChapterMaterials",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterMaterials_ChapterSectionId",
                table: "ChapterMaterials",
                column: "ChapterSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_CampusId",
                table: "Chapters",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_SubjectId",
                table: "Chapters",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterSections_ChapterId",
                table: "ChapterSections",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_CampusId",
                table: "Classes",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassTeacherId",
                table: "Classes",
                column: "ClassTeacherId",
                unique: true,
                filter: "[IsActive] = 1 AND [ClassTeacherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_Name_CampusId",
                table: "Classes",
                columns: new[] { "Name", "CampusId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraChargeExclusions_CampusId",
                table: "ClassFeeExtraChargeExclusions",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraChargeExclusions_ClassFeeExtraChargeId",
                table: "ClassFeeExtraChargeExclusions",
                column: "ClassFeeExtraChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraChargeExclusions_StudentId",
                table: "ClassFeeExtraChargeExclusions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraChargePaymentHistories_BillingMasterId",
                table: "ClassFeeExtraChargePaymentHistories",
                column: "BillingMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraChargePaymentHistories_CampusId",
                table: "ClassFeeExtraChargePaymentHistories",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraChargePaymentHistories_ClassFeeExtraChargeId",
                table: "ClassFeeExtraChargePaymentHistories",
                column: "ClassFeeExtraChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraChargePaymentHistories_ClassIdPaidFor",
                table: "ClassFeeExtraChargePaymentHistories",
                column: "ClassIdPaidFor");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraChargePaymentHistories_StudentId",
                table: "ClassFeeExtraChargePaymentHistories",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraCharges_CampusId",
                table: "ClassFeeExtraCharges",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeExtraCharges_ClassId",
                table: "ClassFeeExtraCharges",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFees_CampusId",
                table: "ClassFees",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassFees_ClassId",
                table: "ClassFees",
                column: "ClassId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSections_CampusId",
                table: "ClassSections",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSections_ClassId",
                table: "ClassSections",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSections_Name_ClassId",
                table: "ClassSections",
                columns: new[] { "Name", "ClassId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Diaries_CampusId",
                table: "Diaries",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Diaries_ChapterId",
                table: "Diaries",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Diaries_ChapterSectionId",
                table: "Diaries",
                column: "ChapterSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Diaries_TeacherAssignmentId_Date",
                table: "Diaries",
                columns: new[] { "TeacherAssignmentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiaryImages_DiaryId",
                table: "DiaryImages",
                column: "DiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendance_EmployeeId",
                table: "EmployeeAttendance",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendance_PayrollMasterId",
                table: "EmployeeAttendance",
                column: "PayrollMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCategoryDiscounts_StudentCategoryId",
                table: "EmployeeCategoryDiscounts",
                column: "StudentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleConfigs_CampusId",
                table: "EmployeeRoleConfigs",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoles_CampusId",
                table: "EmployeeRoles",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoles_EmployeeId",
                table: "EmployeeRoles",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoles_EmployeeRoleConfigId",
                table: "EmployeeRoles",
                column: "EmployeeRoleConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CampusId",
                table: "Employees",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCategories_CampusId",
                table: "ExamCategories",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDateSheetClassSections_ClassId",
                table: "ExamDateSheetClassSections",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDateSheetClassSections_ExamDateSheetId",
                table: "ExamDateSheetClassSections",
                column: "ExamDateSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDateSheetClassSections_SectionId",
                table: "ExamDateSheetClassSections",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDateSheets_CampusId",
                table: "ExamDateSheets",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDateSheets_ExamCategoryId",
                table: "ExamDateSheets",
                column: "ExamCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDateSheets_ExamId",
                table: "ExamDateSheets",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDateSheets_SubjectId",
                table: "ExamDateSheets",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_CampusId",
                table: "ExamMarks",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_ClassId",
                table: "ExamMarks",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_ExamId",
                table: "ExamMarks",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_SectionId",
                table: "ExamMarks",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_StudentId",
                table: "ExamMarks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_SubjectId",
                table: "ExamMarks",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CampusId",
                table: "Exams",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ExamCategoryId",
                table: "Exams",
                column: "ExamCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_AccountId",
                table: "Expenses",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CampusId",
                table: "Expenses",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Families_CampusId",
                table: "Families",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Families_FatherCNIC",
                table: "Families",
                column: "FatherCNIC",
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_FineChargeTemplates_CampusId",
                table: "FineChargeTemplates",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceHistories_CampusId",
                table: "LeaveBalanceHistories",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceHistories_EmployeeId",
                table: "LeaveBalanceHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceHistories_LeaveRequestId",
                table: "LeaveBalanceHistories",
                column: "LeaveRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_CampusId",
                table: "LeaveBalances",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId",
                table: "LeaveBalances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveConfigs_CampusId",
                table: "LeaveConfigs",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_CampusId",
                table: "LeaveRequests",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmployeeId",
                table: "LeaveRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_MessageId",
                table: "MessageAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageRecipients_MessageId",
                table: "MessageRecipients",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageRecipients_UserId",
                table: "MessageRecipients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CampusId",
                table: "Messages",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_NamazAttendance_CampusId",
                table: "NamazAttendance",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_NamazAttendance_EmployeeId_Date_AcademicYear",
                table: "NamazAttendance",
                columns: new[] { "EmployeeId", "Date", "AcademicYear" },
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NamazAttendance_StudentId_Date_AcademicYear",
                table: "NamazAttendance",
                columns: new[] { "StudentId", "Date", "AcademicYear" },
                filter: "[StudentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CampusId",
                table: "Notifications",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollMasters_CampusId",
                table: "PayrollMasters",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollMasters_EmployeeId",
                table: "PayrollMasters",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollTransactions_CampusId",
                table: "PayrollTransactions",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollTransactions_OnlineAccount",
                table: "PayrollTransactions",
                column: "OnlineAccount");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollTransactions_PayrollMasterId",
                table: "PayrollTransactions",
                column: "PayrollMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_PickupCards_StudentId",
                table: "PickupCards",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ChapterId",
                table: "Questions",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ChapterSectionId",
                table: "Questions",
                column: "ChapterSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryDeductions_BillingMasterId",
                table: "SalaryDeductions",
                column: "BillingMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryDeductions_CampusId",
                table: "SalaryDeductions",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryDeductions_EmployeeId",
                table: "SalaryDeductions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryDeductions_StudentId",
                table: "SalaryDeductions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryDefinitions_EmployeeId",
                table: "SalaryDefinitions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCategories_CampusId",
                table: "StudentCategories",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCategoryAssignments_EmployeeId",
                table: "StudentCategoryAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCategoryAssignments_StudentCategoryId",
                table: "StudentCategoryAssignments",
                column: "StudentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCategoryAssignments_StudentId",
                table: "StudentCategoryAssignments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentChargeAssignments_CampusId",
                table: "StudentChargeAssignments",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentChargeAssignments_ClassFeeExtraChargeId",
                table: "StudentChargeAssignments",
                column: "ClassFeeExtraChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentChargeAssignments_StudentId_ClassFeeExtraChargeId",
                table: "StudentChargeAssignments",
                columns: new[] { "StudentId", "ClassFeeExtraChargeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentComplaints_CampusId",
                table: "StudentComplaints",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentComplaints_StudentId",
                table: "StudentComplaints",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentComplaints_TeacherId",
                table: "StudentComplaints",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFineCharges_BillingMasterId",
                table: "StudentFineCharges",
                column: "BillingMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFineCharges_CampusId",
                table: "StudentFineCharges",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFineCharges_StudentId",
                table: "StudentFineCharges",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentHistories_ClassId",
                table: "StudentHistories",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentHistories_ExamId",
                table: "StudentHistories",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentHistories_SectionId",
                table: "StudentHistories",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentHistories_StudentId",
                table: "StudentHistories",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMigrations_FromCampusId",
                table: "StudentMigrations",
                column: "FromCampusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMigrations_StudentId",
                table: "StudentMigrations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentMigrations_ToCampusId",
                table: "StudentMigrations",
                column: "ToCampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_CampusId",
                table: "Students",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_Class",
                table: "Students",
                column: "Class");

            migrationBuilder.CreateIndex(
                name: "IX_Students_FamilyId",
                table: "Students",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_Section",
                table: "Students",
                column: "Section");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentCategoryId",
                table: "Students",
                column: "StudentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_SubjectsGroupingId",
                table: "Students",
                column: "SubjectsGroupingId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSurveyResponses_CampusId",
                table: "StudentSurveyResponses",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSurveyResponses_StudentId_SurveyQuestionId_TeacherId",
                table: "StudentSurveyResponses",
                columns: new[] { "StudentId", "SurveyQuestionId", "TeacherId" },
                unique: true,
                filter: "[TeacherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSurveyResponses_SurveyQuestionId",
                table: "StudentSurveyResponses",
                column: "SurveyQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSurveyResponses_TeacherId",
                table: "StudentSurveyResponses",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_CampusId",
                table: "Subjects",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectsGroupingDetails_SubjectsGroupingId",
                table: "SubjectsGroupingDetails",
                column: "SubjectsGroupingId");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectsGroupings_CampusId",
                table: "SubjectsGroupings",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Substitutions_CampusId",
                table: "Substitutions",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Substitutions_OriginalTeacherId",
                table: "Substitutions",
                column: "OriginalTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Substitutions_SubstituteEmployeeId",
                table: "Substitutions",
                column: "SubstituteEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Substitutions_TimetableSlotId",
                table: "Substitutions",
                column: "TimetableSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyQuestions_CampusId",
                table: "SurveyQuestions",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_CampusId",
                table: "TeacherAssignments",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_ClassId",
                table: "TeacherAssignments",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_ClassId1",
                table: "TeacherAssignments",
                column: "ClassId1");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_ClassSectionId",
                table: "TeacherAssignments",
                column: "ClassSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_SectionId",
                table: "TeacherAssignments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_SubjectId_ClassId_SectionId",
                table: "TeacherAssignments",
                columns: new[] { "SubjectId", "ClassId", "SectionId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_SubjectId1",
                table: "TeacherAssignments",
                column: "SubjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherId",
                table: "TeacherAssignments",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherPerformances_CampusId",
                table: "TeacherPerformances",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherPerformances_TeacherId_Month_Year",
                table: "TeacherPerformances",
                columns: new[] { "TeacherId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestReturns_CampusId",
                table: "TestReturns",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_TestReturns_ClassId",
                table: "TestReturns",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_TestReturns_ExamId",
                table: "TestReturns",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_TestReturns_SectionId",
                table: "TestReturns",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestReturns_SubjectId",
                table: "TestReturns",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TestReturns_TeacherId",
                table: "TestReturns",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableBreaks_TimetableId",
                table: "TimetableBreaks",
                column: "TimetableId");

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_CampusId",
                table: "Timetables",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_ClassId_SectionId",
                table: "Timetables",
                columns: new[] { "ClassId", "SectionId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_SectionId",
                table: "Timetables",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSlots_TeacherAssignmentId",
                table: "TimetableSlots",
                column: "TeacherAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSlots_TimetableId",
                table: "TimetableSlots",
                column: "TimetableId");

            migrationBuilder.CreateIndex(
                name: "IX_ToDos_CampusId",
                table: "ToDos",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_ToDos_UserId",
                table: "ToDos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationReads_NotificationId",
                table: "UserNotificationReads",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationReads_UserId",
                table: "UserNotificationReads",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicCalendars");

            migrationBuilder.DropTable(
                name: "AcademicYear");

            migrationBuilder.DropTable(
                name: "AdminNotifications");

            migrationBuilder.DropTable(
                name: "AdmissionInquiries");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "AssignedDutyStudents");

            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "BillingTransactions");

            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "CertificateRequests");

            migrationBuilder.DropTable(
                name: "ChapterMaterials");

            migrationBuilder.DropTable(
                name: "ClassFeeExtraChargeExclusions");

            migrationBuilder.DropTable(
                name: "ClassFeeExtraChargePaymentHistories");

            migrationBuilder.DropTable(
                name: "ClassFees");

            migrationBuilder.DropTable(
                name: "DiaryImages");

            migrationBuilder.DropTable(
                name: "EmployeeAttendance");

            migrationBuilder.DropTable(
                name: "EmployeeCategoryDiscounts");

            migrationBuilder.DropTable(
                name: "EmployeeRoles");

            migrationBuilder.DropTable(
                name: "ExamDateSheetClassSections");

            migrationBuilder.DropTable(
                name: "ExamMarks");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "FineChargeTemplates");

            migrationBuilder.DropTable(
                name: "LeaveBalanceHistories");

            migrationBuilder.DropTable(
                name: "LeaveBalances");

            migrationBuilder.DropTable(
                name: "LeaveConfigs");

            migrationBuilder.DropTable(
                name: "MessageAttachments");

            migrationBuilder.DropTable(
                name: "MessageRecipients");

            migrationBuilder.DropTable(
                name: "NamazAttendance");

            migrationBuilder.DropTable(
                name: "PayrollTransactions");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "SalaryDeductions");

            migrationBuilder.DropTable(
                name: "SalaryDefinitions");

            migrationBuilder.DropTable(
                name: "StudentCategoryAssignments");

            migrationBuilder.DropTable(
                name: "StudentChargeAssignments");

            migrationBuilder.DropTable(
                name: "StudentComplaints");

            migrationBuilder.DropTable(
                name: "StudentHistories");

            migrationBuilder.DropTable(
                name: "StudentMigrations");

            migrationBuilder.DropTable(
                name: "StudentSurveyResponses");

            migrationBuilder.DropTable(
                name: "SubjectsGroupingDetails");

            migrationBuilder.DropTable(
                name: "Substitutions");

            migrationBuilder.DropTable(
                name: "TeacherPerformances");

            migrationBuilder.DropTable(
                name: "TestReturns");

            migrationBuilder.DropTable(
                name: "TimetableBreaks");

            migrationBuilder.DropTable(
                name: "ToDos");

            migrationBuilder.DropTable(
                name: "UserNotificationReads");

            migrationBuilder.DropTable(
                name: "PickupCards");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AssignedDuties");

            migrationBuilder.DropTable(
                name: "CertificateTypes");

            migrationBuilder.DropTable(
                name: "StudentFineCharges");

            migrationBuilder.DropTable(
                name: "Diaries");

            migrationBuilder.DropTable(
                name: "EmployeeRoleConfigs");

            migrationBuilder.DropTable(
                name: "ExamDateSheets");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropTable(
                name: "PayrollMasters");

            migrationBuilder.DropTable(
                name: "ClassFeeExtraCharges");

            migrationBuilder.DropTable(
                name: "SurveyQuestions");

            migrationBuilder.DropTable(
                name: "TimetableSlots");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "BillingMaster");

            migrationBuilder.DropTable(
                name: "ChapterSections");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "TeacherAssignments");

            migrationBuilder.DropTable(
                name: "Timetables");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "ExamCategories");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "ClassSections");

            migrationBuilder.DropTable(
                name: "Families");

            migrationBuilder.DropTable(
                name: "StudentCategories");

            migrationBuilder.DropTable(
                name: "SubjectsGroupings");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Campuses");
        }
    }
}
