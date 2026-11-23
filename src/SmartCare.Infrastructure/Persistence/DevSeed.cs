using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartCare.Application.Security;
using SmartCare.Domain.Common;
using SmartCare.Domain.Entities;
using SmartCare.Infrastructure.Identity;

namespace SmartCare.Infrastructure.Persistence;

/// <summary>Sample data for development and demos. Runs only when Seed:DevData is true and only on an empty database.</summary>
public static class DevSeed
{
    public static async Task RunAsync(AppDbContext db, UserManager<ApplicationUser> users, string password)
    {
        if (await db.Patients.AnyAsync()) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Department Dept(string name) => db.Departments.First(d => d.Name == name);

        async Task<Staff> MakeStaff(string fullName, string role, string department, string email)
        {
            var staff = new Staff { FullName = fullName, RoleName = role, DepartmentId = Dept(department).Id, Email = email };
            db.Staff.Add(staff);
            await db.SaveChangesAsync();

            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = fullName, StaffId = staff.Id };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
                throw new InvalidOperationException("Dev user failed: " + string.Join("; ", created.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, role);
            staff.UserId = user.Id;
            await db.SaveChangesAsync();
            return staff;
        }

        var doctor = await MakeStaff("Dr. Demo Doctor", Roles.Doctor, "Outpatient", "doctor@smartcare.local");
        await MakeStaff("Demo Nurse", Roles.Nurse, "Nursing", "nurse@smartcare.local");
        await MakeStaff("Demo Pharmacist", Roles.Pharmacist, "Pharmacy", "pharmacist@smartcare.local");
        await MakeStaff("Demo Lab Tech One", Roles.LabTechnician, "Laboratory", "lab@smartcare.local");
        await MakeStaff("Demo Lab Tech Two", Roles.LabTechnician, "Laboratory", "lab2@smartcare.local");

        // A test patient with a severe penicillin allergy and an open visit
        var patient = new Patient
        {
            Mrn = "MRN-0001", FirstName = "Test", LastName = "Patient", DateOfBirth = new DateOnly(1990, 5, 12),
            Gender = Gender.Female, Phone = "+250780000001"
        };
        patient.Allergies.Add(new Allergy { Substance = "Penicillin", Severity = AllergySeverity.Severe, Reaction = "Rash, swelling" });
        db.Patients.Add(patient);
        db.Encounters.Add(new Encounter { Patient = patient, DoctorId = doctor.Id, Type = EncounterType.Outpatient, ChiefComplaint = "Fever and cough" });

        // Prices (Seat E owns the real catalog; these let billing events carry a price)
        PriceItem Price(string code, string name, PriceCategory cat, decimal price) =>
            new() { Code = code, Name = name, Category = cat, UnitPrice = price, EffectiveFrom = today.AddDays(-30) };
        var pAmox = Price("DRG-AMOX500", "Amoxicillin 500mg capsule", PriceCategory.Pharmacy, 300);
        var pPara = Price("DRG-PARA500", "Paracetamol 500mg tablet", PriceCategory.Pharmacy, 100);
        var pHb = Price("LAB-HB", "Hemoglobin test", PriceCategory.Lab, 3000);
        var pMal = Price("LAB-MAL", "Malaria rapid test", PriceCategory.Lab, 2000);
        var pCxr = Price("RAD-CXR", "Chest X-ray", PriceCategory.Radiology, 15000);
        db.PriceItems.AddRange(pAmox, pPara, pHb, pMal, pCxr);

        // Pharmacy: Amoxicillin is a penicillin (allergy demo). Paracetamol is low and near expiry (alert demo).
        var amox = new Drug { Code = "AMOX500", Name = "Amoxicillin", GenericName = "Amoxicillin", DrugClass = "Penicillin", Form = "Capsule", Strength = "500mg", ReorderLevel = 50, ReorderQuantity = 200, PriceItemId = pAmox.Id };
        var para = new Drug { Code = "PARA500", Name = "Paracetamol", GenericName = "Acetaminophen", Form = "Tablet", Strength = "500mg", ReorderLevel = 100, ReorderQuantity = 500, PriceItemId = pPara.Id };
        db.Drugs.AddRange(amox, para);
        var supplier = new Supplier { Name = "Demo Supplier Ltd" };
        db.Suppliers.Add(supplier);
        db.StockBatches.AddRange(
            new StockBatch { Drug = amox, BatchNumber = "AMX-1001", ExpiryDate = today.AddMonths(12), QuantityOnHand = 120, UnitCost = 200, SupplierId = supplier.Id },
            new StockBatch { Drug = para, BatchNumber = "PAR-2001", ExpiryDate = today.AddDays(60), QuantityOnHand = 80, UnitCost = 60, SupplierId = supplier.Id });

        // Lab: numeric test with normal and critical ranges, a text test, and a radiology test
        db.TestCatalogItems.AddRange(
            new TestCatalogItem { Code = "HB", Name = "Hemoglobin", Kind = TestKind.Lab, Unit = "g/dL", ReferenceLow = 12, ReferenceHigh = 16, CriticalLow = 7, CriticalHigh = 20, PriceItemId = pHb.Id },
            new TestCatalogItem { Code = "MAL", Name = "Malaria rapid test", Kind = TestKind.Lab, PriceItemId = pMal.Id },
            new TestCatalogItem { Code = "CXR", Name = "Chest X-ray", Kind = TestKind.Radiology, PriceItemId = pCxr.Id });

        await db.SaveChangesAsync();
    }
}
