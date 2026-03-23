namespace SchoolManagement.Models.Enums;

public enum UserRole
{
    // System / admin roles
    OwnerAdmin           = 1,
    SuperAdmin           = 2,
    Admin                = 3,

    // Academic roles
    Teacher              = 4,
    Student              = 5,
    HeadTeacher          = 6,
    Principal            = 7,
    VicePrincipal        = 8,
    Coordinator          = 9,

    // Parent / guardian roles
    Parent               = 10,
    Guardian             = 11,

    // Administrative / office roles
    SchoolAdministrator  = 12,
    OfficeStaff          = 13,
    Clerk                = 14,
    Accountant           = 15,
    Librarian            = 16,
    LabAssistant         = 17,
    ITStaff              = 18,
    Receptionist         = 19,
    Counselor            = 20,
    SpecialEducator      = 21,

    // Health roles
    Nurse                = 22,
    MedicalStaff         = 23,

    // Support / operations roles
    Driver               = 24,
    Conductor            = 25,
    Attendant            = 26,
    SecurityGuard        = 27,
    Cleaner              = 28,
    MaintenanceStaff     = 29,
}
