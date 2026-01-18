using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Models;

namespace MedicalStoreERP.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply pending migrations
            await context.Database.MigrateAsync();

            // Seed Roles
            await SeedRolesAsync(roleManager);

            // Seed Users
            await SeedUsersAsync(userManager);

            // Seed Categories
            await SeedCategoriesAsync(context);

            // Seed Medicines
            await SeedMedicinesAsync(context);

            // Seed Suppliers
            await SeedSuppliersAsync(context);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "SuperAdmin", "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            // Seed SuperAdmin User
            var superAdminEmail = "superadmin@store.com";
            if (await userManager.FindByEmailAsync(superAdminEmail) == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FullName = "Super Administrator",
                    EmailConfirmed = true,
                    PhoneNumber = "9876543200",
                    Address = "Head Office",
                    City = "Mumbai",
                    State = "Maharashtra",
                    PinCode = "400001",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(superAdmin, "SuperAdmin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
            }

            // Seed Admin User
            var adminEmail = "admin@store.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    PhoneNumber = "9876543210",
                    Address = "123 Admin Street",
                    City = "Mumbai",
                    State = "Maharashtra",
                    PinCode = "400001",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // Seed Regular User
            var userEmail = "user@store.com";
            if (await userManager.FindByEmailAsync(userEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    FullName = "John Doe",
                    EmailConfirmed = true,
                    PhoneNumber = "9876543211",
                    Address = "456 User Lane",
                    City = "Delhi",
                    State = "Delhi",
                    PinCode = "110001",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, "User@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }
        }

        private static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            if (await context.Categories.AnyAsync()) return;

            var categories = new List<Category>
            {
                new Category { Name = "Pain Relief", Description = "Medicines for pain and fever relief", ImageUrl = "/images/categories/pain-relief.jpg", DisplayOrder = 1 },
                new Category { Name = "Antibiotics", Description = "Antibiotic medicines for infections", ImageUrl = "/images/categories/antibiotics.jpg", DisplayOrder = 2 },
                new Category { Name = "Vitamins & Supplements", Description = "Vitamins, minerals and dietary supplements", ImageUrl = "/images/categories/vitamins.jpg", DisplayOrder = 3 },
                new Category { Name = "Digestive Health", Description = "Medicines for digestive issues", ImageUrl = "/images/categories/digestive.jpg", DisplayOrder = 4 },
                new Category { Name = "Cold & Cough", Description = "Medicines for cold, cough and flu", ImageUrl = "/images/categories/cold-cough.jpg", DisplayOrder = 5 },
                new Category { Name = "Diabetes Care", Description = "Medicines for diabetes management", ImageUrl = "/images/categories/diabetes.jpg", DisplayOrder = 6 },
                new Category { Name = "Heart Care", Description = "Cardiovascular medicines", ImageUrl = "/images/categories/heart.jpg", DisplayOrder = 7 },
                new Category { Name = "Skin Care", Description = "Dermatological products", ImageUrl = "/images/categories/skin.jpg", DisplayOrder = 8 },
                new Category { Name = "Eye & Ear Care", Description = "Ophthalmic and otic preparations", ImageUrl = "/images/categories/eye-ear.jpg", DisplayOrder = 9 },
                new Category { Name = "First Aid", Description = "First aid and emergency supplies", ImageUrl = "/images/categories/first-aid.jpg", DisplayOrder = 10 }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        private static async Task SeedMedicinesAsync(ApplicationDbContext context)
        {
            if (await context.Medicines.AnyAsync()) return;

            var random = new Random();
            var baseDate = DateTime.Today;

            var medicines = new List<Medicine>
            {
                // Pain Relief (Category 1)
                new Medicine { Name = "Paracetamol 500mg", GenericName = "Paracetamol", BatchNo = "PCM001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(2), MRP = 25, SalePrice = 22, PurchasePrice = 15, StockQty = 500, HSNCode = "3004", GSTPercent = 12, Description = "For fever and mild pain", Manufacturer = "Cipla", Composition = "Paracetamol 500mg", Dosage = "1-2 tablets twice daily", PackSize = "Strip of 10", CategoryId = 1, IsFeatured = true },
                new Medicine { Name = "Dolo 650mg", GenericName = "Paracetamol", BatchNo = "DLO001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 35, SalePrice = 32, PurchasePrice = 22, StockQty = 400, HSNCode = "3004", GSTPercent = 12, Description = "For high fever and body pain", Manufacturer = "Micro Labs", Composition = "Paracetamol 650mg", Dosage = "1 tablet thrice daily", PackSize = "Strip of 15", CategoryId = 1, IsFeatured = true },
                new Medicine { Name = "Crocin Advance", GenericName = "Paracetamol", BatchNo = "CRC001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 40, SalePrice = 36, PurchasePrice = 26, StockQty = 350, HSNCode = "3004", GSTPercent = 12, Description = "Fast acting pain relief", Manufacturer = "GSK", Composition = "Paracetamol 500mg", Dosage = "1-2 tablets as needed", PackSize = "Strip of 15", CategoryId = 1 },
                new Medicine { Name = "Combiflam", GenericName = "Ibuprofen + Paracetamol", BatchNo = "CMB001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 45, SalePrice = 42, PurchasePrice = 30, StockQty = 300, HSNCode = "3004", GSTPercent = 12, Description = "For severe pain and inflammation", Manufacturer = "Sanofi", Composition = "Ibuprofen 400mg + Paracetamol 325mg", Dosage = "1 tablet twice daily", PackSize = "Strip of 10", CategoryId = 1, IsFeatured = true },
                new Medicine { Name = "Disprin", GenericName = "Aspirin", BatchNo = "DSP001", ManufactureDate = baseDate.AddMonths(-7), ExpiryDate = baseDate.AddYears(1).AddMonths(6), MRP = 20, SalePrice = 18, PurchasePrice = 12, StockQty = 450, HSNCode = "3004", GSTPercent = 12, Description = "Soluble aspirin for quick relief", Manufacturer = "Reckitt", Composition = "Aspirin 350mg", Dosage = "1-2 tablets dissolved in water", PackSize = "Strip of 10", CategoryId = 1 },
                new Medicine { Name = "Voveran SR", GenericName = "Diclofenac", BatchNo = "VOV001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 85, SalePrice = 78, PurchasePrice = 55, StockQty = 200, HSNCode = "3004", GSTPercent = 12, Description = "Sustained release pain relief", Manufacturer = "Novartis", Composition = "Diclofenac Sodium 100mg SR", Dosage = "1 tablet daily", PackSize = "Strip of 10", CategoryId = 1, RequiresPrescription = true },
                
                // Antibiotics (Category 2)
                new Medicine { Name = "Azithromycin 500mg", GenericName = "Azithromycin", BatchNo = "AZM001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 120, SalePrice = 110, PurchasePrice = 75, StockQty = 150, HSNCode = "3004", GSTPercent = 12, Description = "Broad spectrum antibiotic", Manufacturer = "Cipla", Composition = "Azithromycin 500mg", Dosage = "1 tablet daily for 3 days", PackSize = "Strip of 3", CategoryId = 2, RequiresPrescription = true, IsFeatured = true },
                new Medicine { Name = "Amoxicillin 500mg", GenericName = "Amoxicillin", BatchNo = "AMX001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 85, SalePrice = 78, PurchasePrice = 52, StockQty = 180, HSNCode = "3004", GSTPercent = 12, Description = "Penicillin-type antibiotic", Manufacturer = "Ranbaxy", Composition = "Amoxicillin 500mg", Dosage = "1 capsule thrice daily", PackSize = "Strip of 10", CategoryId = 2, RequiresPrescription = true },
                new Medicine { Name = "Augmentin 625 Duo", GenericName = "Amoxicillin + Clavulanic Acid", BatchNo = "AUG001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 280, SalePrice = 255, PurchasePrice = 180, StockQty = 100, HSNCode = "3004", GSTPercent = 12, Description = "Combination antibiotic", Manufacturer = "GSK", Composition = "Amoxicillin 500mg + Clavulanic Acid 125mg", Dosage = "1 tablet twice daily", PackSize = "Strip of 6", CategoryId = 2, RequiresPrescription = true },
                new Medicine { Name = "Ciprofloxacin 500mg", GenericName = "Ciprofloxacin", BatchNo = "CIP001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(2), MRP = 95, SalePrice = 88, PurchasePrice = 60, StockQty = 160, HSNCode = "3004", GSTPercent = 12, Description = "Fluoroquinolone antibiotic", Manufacturer = "Sun Pharma", Composition = "Ciprofloxacin 500mg", Dosage = "1 tablet twice daily", PackSize = "Strip of 10", CategoryId = 2, RequiresPrescription = true },
                new Medicine { Name = "Metronidazole 400mg", GenericName = "Metronidazole", BatchNo = "MTR001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 45, SalePrice = 42, PurchasePrice = 28, StockQty = 220, HSNCode = "3004", GSTPercent = 12, Description = "Antibiotic and antiprotozoal", Manufacturer = "Alkem", Composition = "Metronidazole 400mg", Dosage = "1 tablet thrice daily", PackSize = "Strip of 10", CategoryId = 2, RequiresPrescription = true },
                new Medicine { Name = "Cefixime 200mg", GenericName = "Cefixime", BatchNo = "CEF001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 145, SalePrice = 132, PurchasePrice = 95, StockQty = 120, HSNCode = "3004", GSTPercent = 12, Description = "Cephalosporin antibiotic", Manufacturer = "Lupin", Composition = "Cefixime 200mg", Dosage = "1 tablet twice daily", PackSize = "Strip of 10", CategoryId = 2, RequiresPrescription = true },
                
                // Vitamins & Supplements (Category 3)
                new Medicine { Name = "Vitamin C 500mg", GenericName = "Ascorbic Acid", BatchNo = "VTC001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 18, SalePrice = 15, PurchasePrice = 10, StockQty = 600, HSNCode = "3004", GSTPercent = 12, Description = "Immunity booster", Manufacturer = "Abbott", Composition = "Ascorbic Acid 500mg", Dosage = "1 tablet daily", PackSize = "Strip of 10", CategoryId = 3, IsFeatured = true },
                new Medicine { Name = "Vitamin D3 60000 IU", GenericName = "Cholecalciferol", BatchNo = "VTD001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 120, SalePrice = 108, PurchasePrice = 75, StockQty = 250, HSNCode = "3004", GSTPercent = 12, Description = "For bone health and immunity", Manufacturer = "USV", Composition = "Cholecalciferol 60000 IU", Dosage = "1 sachet weekly", PackSize = "Pack of 4", CategoryId = 3 },
                new Medicine { Name = "B-Complex Forte", GenericName = "Vitamin B Complex", BatchNo = "BCX001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 65, SalePrice = 58, PurchasePrice = 40, StockQty = 350, HSNCode = "3004", GSTPercent = 12, Description = "For nerve health and energy", Manufacturer = "Abbott", Composition = "B1, B2, B6, B12, Niacin, Folic Acid", Dosage = "1 tablet daily", PackSize = "Strip of 20", CategoryId = 3 },
                new Medicine { Name = "Calcium + Vitamin D3", GenericName = "Calcium Carbonate + D3", BatchNo = "CLD001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 180, SalePrice = 165, PurchasePrice = 115, StockQty = 200, HSNCode = "3004", GSTPercent = 12, Description = "For strong bones", Manufacturer = "Pfizer", Composition = "Calcium 500mg + Vitamin D3 250 IU", Dosage = "1 tablet twice daily", PackSize = "Bottle of 30", CategoryId = 3 },
                new Medicine { Name = "Iron + Folic Acid", GenericName = "Ferrous Sulphate + Folic Acid", BatchNo = "IFA001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(2), MRP = 55, SalePrice = 48, PurchasePrice = 32, StockQty = 280, HSNCode = "3004", GSTPercent = 12, Description = "For anemia prevention", Manufacturer = "Alkem", Composition = "Ferrous Sulphate 100mg + Folic Acid 0.5mg", Dosage = "1 tablet daily", PackSize = "Strip of 30", CategoryId = 3 },
                new Medicine { Name = "Zinc Tablets", GenericName = "Zinc Sulphate", BatchNo = "ZNC001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 45, SalePrice = 40, PurchasePrice = 28, StockQty = 320, HSNCode = "3004", GSTPercent = 12, Description = "For immunity and skin health", Manufacturer = "Mankind", Composition = "Zinc Sulphate 20mg", Dosage = "1 tablet daily", PackSize = "Strip of 10", CategoryId = 3 },
                new Medicine { Name = "Omega-3 Fish Oil", GenericName = "Fish Oil", BatchNo = "OMG001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(1).AddMonths(6), MRP = 450, SalePrice = 410, PurchasePrice = 290, StockQty = 150, HSNCode = "3004", GSTPercent = 12, Description = "For heart and brain health", Manufacturer = "Healthkart", Composition = "EPA 180mg + DHA 120mg", Dosage = "1-2 capsules daily", PackSize = "Bottle of 60", CategoryId = 3, IsFeatured = true },
                new Medicine { Name = "Multivitamin Tablets", GenericName = "Multivitamin", BatchNo = "MVT001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 295, SalePrice = 270, PurchasePrice = 190, StockQty = 180, HSNCode = "3004", GSTPercent = 12, Description = "Complete daily nutrition", Manufacturer = "Centrum", Composition = "Vitamins A, C, D, E, K, B-complex, Minerals", Dosage = "1 tablet daily", PackSize = "Bottle of 30", CategoryId = 3 },
                
                // Digestive Health (Category 4)
                new Medicine { Name = "Pantoprazole 40mg", GenericName = "Pantoprazole", BatchNo = "PNT001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 120, SalePrice = 108, PurchasePrice = 75, StockQty = 250, HSNCode = "3004", GSTPercent = 12, Description = "For acidity and ulcers", Manufacturer = "Sun Pharma", Composition = "Pantoprazole 40mg", Dosage = "1 tablet before breakfast", PackSize = "Strip of 15", CategoryId = 4, IsFeatured = true },
                new Medicine { Name = "Omeprazole 20mg", GenericName = "Omeprazole", BatchNo = "OMP001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 85, SalePrice = 78, PurchasePrice = 52, StockQty = 280, HSNCode = "3004", GSTPercent = 12, Description = "Proton pump inhibitor", Manufacturer = "Cipla", Composition = "Omeprazole 20mg", Dosage = "1 capsule daily before meal", PackSize = "Strip of 10", CategoryId = 4 },
                new Medicine { Name = "Domperidone 10mg", GenericName = "Domperidone", BatchNo = "DMP001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 55, SalePrice = 48, PurchasePrice = 32, StockQty = 320, HSNCode = "3004", GSTPercent = 12, Description = "For nausea and vomiting", Manufacturer = "Dr. Reddy's", Composition = "Domperidone 10mg", Dosage = "1 tablet thrice daily", PackSize = "Strip of 10", CategoryId = 4 },
                new Medicine { Name = "Ranitidine 150mg", GenericName = "Ranitidine", BatchNo = "RNT001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(2), MRP = 45, SalePrice = 40, PurchasePrice = 28, StockQty = 350, HSNCode = "3004", GSTPercent = 12, Description = "H2 blocker for acidity", Manufacturer = "Zydus", Composition = "Ranitidine 150mg", Dosage = "1 tablet twice daily", PackSize = "Strip of 10", CategoryId = 4 },
                new Medicine { Name = "Loperamide 2mg", GenericName = "Loperamide", BatchNo = "LPR001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 35, SalePrice = 32, PurchasePrice = 22, StockQty = 220, HSNCode = "3004", GSTPercent = 12, Description = "For diarrhea relief", Manufacturer = "Mankind", Composition = "Loperamide 2mg", Dosage = "2 capsules initially, then 1 after each loose stool", PackSize = "Strip of 4", CategoryId = 4 },
                new Medicine { Name = "Digene Gel", GenericName = "Antacid Gel", BatchNo = "DGN001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(1).AddMonths(6), MRP = 95, SalePrice = 88, PurchasePrice = 60, StockQty = 180, HSNCode = "3004", GSTPercent = 12, Description = "Fast acting antacid gel", Manufacturer = "Abbott", Composition = "Magnesium Hydroxide + Aluminium Hydroxide + Simethicone", Dosage = "2 teaspoons after meals", PackSize = "Bottle of 200ml", CategoryId = 4 },
                
                // Cold & Cough (Category 5)
                new Medicine { Name = "Cetirizine 10mg", GenericName = "Cetirizine", BatchNo = "CTZ001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 35, SalePrice = 30, PurchasePrice = 20, StockQty = 400, HSNCode = "3004", GSTPercent = 12, Description = "For allergies and cold", Manufacturer = "Dr. Reddy's", Composition = "Cetirizine 10mg", Dosage = "1 tablet daily at bedtime", PackSize = "Strip of 10", CategoryId = 5, IsFeatured = true },
                new Medicine { Name = "Montair LC", GenericName = "Montelukast + Levocetirizine", BatchNo = "MLC001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 185, SalePrice = 168, PurchasePrice = 120, StockQty = 180, HSNCode = "3004", GSTPercent = 12, Description = "For allergic rhinitis and asthma", Manufacturer = "Cipla", Composition = "Montelukast 10mg + Levocetirizine 5mg", Dosage = "1 tablet at bedtime", PackSize = "Strip of 10", CategoryId = 5, RequiresPrescription = true },
                new Medicine { Name = "Sinarest Tablet", GenericName = "Paracetamol + Phenylephrine + Chlorpheniramine", BatchNo = "SNR001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 45, SalePrice = 42, PurchasePrice = 28, StockQty = 320, HSNCode = "3004", GSTPercent = 12, Description = "For cold and sinus relief", Manufacturer = "Centaur", Composition = "Paracetamol 500mg + Phenylephrine 10mg + Chlorpheniramine 2mg", Dosage = "1 tablet thrice daily", PackSize = "Strip of 10", CategoryId = 5 },
                new Medicine { Name = "Benadryl Cough Syrup", GenericName = "Diphenhydramine", BatchNo = "BND001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(1).AddMonths(6), MRP = 110, SalePrice = 99, PurchasePrice = 70, StockQty = 200, HSNCode = "3004", GSTPercent = 12, Description = "Cough suppressant", Manufacturer = "Johnson & Johnson", Composition = "Diphenhydramine 14.08mg/5ml", Dosage = "10ml thrice daily", PackSize = "Bottle of 100ml", CategoryId = 5 },
                new Medicine { Name = "Ascoril LS Syrup", GenericName = "Levosalbutamol + Ambroxol + Guaifenesin", BatchNo = "ASC001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(1).AddMonths(6), MRP = 125, SalePrice = 115, PurchasePrice = 80, StockQty = 180, HSNCode = "3004", GSTPercent = 12, Description = "For productive cough", Manufacturer = "Glenmark", Composition = "Levosalbutamol 1mg + Ambroxol 30mg + Guaifenesin 50mg per 5ml", Dosage = "10ml thrice daily", PackSize = "Bottle of 100ml", CategoryId = 5 },
                new Medicine { Name = "Vicks VapoRub", GenericName = "Camphor + Menthol + Eucalyptus Oil", BatchNo = "VCK001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(3), MRP = 85, SalePrice = 78, PurchasePrice = 55, StockQty = 250, HSNCode = "3004", GSTPercent = 12, Description = "Topical cough suppressant", Manufacturer = "P&G", Composition = "Camphor 5.3% + Menthol 2.8% + Eucalyptus Oil 1.2%", Dosage = "Apply on chest and throat", PackSize = "Jar of 50g", CategoryId = 5 },
                
                // Diabetes Care (Category 6)
                new Medicine { Name = "Metformin 500mg", GenericName = "Metformin", BatchNo = "MET001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 45, SalePrice = 40, PurchasePrice = 28, StockQty = 300, HSNCode = "3004", GSTPercent = 12, Description = "First-line diabetes medication", Manufacturer = "USV", Composition = "Metformin 500mg", Dosage = "1 tablet twice daily with meals", PackSize = "Strip of 20", CategoryId = 6, RequiresPrescription = true, IsFeatured = true },
                new Medicine { Name = "Glimepiride 2mg", GenericName = "Glimepiride", BatchNo = "GLM001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 85, SalePrice = 78, PurchasePrice = 52, StockQty = 200, HSNCode = "3004", GSTPercent = 12, Description = "Sulfonylurea for diabetes", Manufacturer = "Sanofi", Composition = "Glimepiride 2mg", Dosage = "1 tablet daily before breakfast", PackSize = "Strip of 10", CategoryId = 6, RequiresPrescription = true },
                new Medicine { Name = "Januvia 100mg", GenericName = "Sitagliptin", BatchNo = "JNV001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 550, SalePrice = 495, PurchasePrice = 350, StockQty = 80, HSNCode = "3004", GSTPercent = 12, Description = "DPP-4 inhibitor for diabetes", Manufacturer = "MSD", Composition = "Sitagliptin 100mg", Dosage = "1 tablet daily", PackSize = "Strip of 7", CategoryId = 6, RequiresPrescription = true },
                new Medicine { Name = "Insulin Glargine", GenericName = "Insulin Glargine", BatchNo = "INS001", ManufactureDate = baseDate.AddMonths(-2), ExpiryDate = baseDate.AddYears(1), MRP = 1250, SalePrice = 1150, PurchasePrice = 850, StockQty = 50, HSNCode = "3004", GSTPercent = 5, Description = "Long-acting insulin", Manufacturer = "Sanofi", Composition = "Insulin Glargine 100 IU/ml", Dosage = "As prescribed", PackSize = "Pen of 3ml", CategoryId = 6, RequiresPrescription = true },
                new Medicine { Name = "Glucose Monitor Strips", GenericName = "Blood Glucose Test Strips", BatchNo = "GMS001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(1).AddMonths(6), MRP = 650, SalePrice = 595, PurchasePrice = 420, StockQty = 100, HSNCode = "3822", GSTPercent = 12, Description = "For blood glucose monitoring", Manufacturer = "Accu-Chek", Composition = "Glucose Oxidase Electrode", Dosage = "Use as needed", PackSize = "Box of 50", CategoryId = 6 },
                
                // Heart Care (Category 7)
                new Medicine { Name = "Aspirin 75mg", GenericName = "Aspirin", BatchNo = "ASP001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 35, SalePrice = 32, PurchasePrice = 22, StockQty = 350, HSNCode = "3004", GSTPercent = 12, Description = "Blood thinner for heart protection", Manufacturer = "Bayer", Composition = "Aspirin 75mg", Dosage = "1 tablet daily", PackSize = "Strip of 14", CategoryId = 7, RequiresPrescription = true },
                new Medicine { Name = "Atorvastatin 10mg", GenericName = "Atorvastatin", BatchNo = "ATV001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 145, SalePrice = 132, PurchasePrice = 92, StockQty = 200, HSNCode = "3004", GSTPercent = 12, Description = "Cholesterol lowering medication", Manufacturer = "Pfizer", Composition = "Atorvastatin 10mg", Dosage = "1 tablet daily at bedtime", PackSize = "Strip of 10", CategoryId = 7, RequiresPrescription = true, IsFeatured = true },
                new Medicine { Name = "Amlodipine 5mg", GenericName = "Amlodipine", BatchNo = "AML001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 65, SalePrice = 58, PurchasePrice = 40, StockQty = 250, HSNCode = "3004", GSTPercent = 12, Description = "Calcium channel blocker for BP", Manufacturer = "Cipla", Composition = "Amlodipine 5mg", Dosage = "1 tablet daily", PackSize = "Strip of 10", CategoryId = 7, RequiresPrescription = true },
                new Medicine { Name = "Telmisartan 40mg", GenericName = "Telmisartan", BatchNo = "TLM001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 125, SalePrice = 115, PurchasePrice = 80, StockQty = 180, HSNCode = "3004", GSTPercent = 12, Description = "ARB for hypertension", Manufacturer = "Sun Pharma", Composition = "Telmisartan 40mg", Dosage = "1 tablet daily", PackSize = "Strip of 10", CategoryId = 7, RequiresPrescription = true },
                new Medicine { Name = "Clopidogrel 75mg", GenericName = "Clopidogrel", BatchNo = "CLP001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 165, SalePrice = 150, PurchasePrice = 105, StockQty = 160, HSNCode = "3004", GSTPercent = 12, Description = "Antiplatelet medication", Manufacturer = "Sanofi", Composition = "Clopidogrel 75mg", Dosage = "1 tablet daily", PackSize = "Strip of 10", CategoryId = 7, RequiresPrescription = true },
                
                // Skin Care (Category 8)
                new Medicine { Name = "Betadine Ointment", GenericName = "Povidone Iodine", BatchNo = "BTD001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(2), MRP = 85, SalePrice = 78, PurchasePrice = 55, StockQty = 200, HSNCode = "3004", GSTPercent = 12, Description = "Antiseptic ointment", Manufacturer = "Win-Medicare", Composition = "Povidone Iodine 5%", Dosage = "Apply to affected area 1-3 times daily", PackSize = "Tube of 20g", CategoryId = 8 },
                new Medicine { Name = "Clotrimazole Cream", GenericName = "Clotrimazole", BatchNo = "CLT001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 65, SalePrice = 58, PurchasePrice = 40, StockQty = 180, HSNCode = "3004", GSTPercent = 12, Description = "Antifungal cream", Manufacturer = "Glenmark", Composition = "Clotrimazole 1%", Dosage = "Apply twice daily for 2-4 weeks", PackSize = "Tube of 15g", CategoryId = 8 },
                new Medicine { Name = "Hydrocortisone Cream", GenericName = "Hydrocortisone", BatchNo = "HDC001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 75, SalePrice = 68, PurchasePrice = 48, StockQty = 150, HSNCode = "3004", GSTPercent = 12, Description = "Mild steroid for skin inflammation", Manufacturer = "Cipla", Composition = "Hydrocortisone Acetate 1%", Dosage = "Apply sparingly 1-2 times daily", PackSize = "Tube of 15g", CategoryId = 8 },
                new Medicine { Name = "Sunscreen SPF 50", GenericName = "Sunscreen", BatchNo = "SUN001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(2), MRP = 350, SalePrice = 315, PurchasePrice = 220, StockQty = 120, HSNCode = "3304", GSTPercent = 18, Description = "High protection sunscreen", Manufacturer = "La Shield", Composition = "Zinc Oxide + Titanium Dioxide", Dosage = "Apply 15 minutes before sun exposure", PackSize = "Tube of 60g", CategoryId = 8, IsFeatured = true },
                new Medicine { Name = "Moisturizing Lotion", GenericName = "Moisturizer", BatchNo = "MOS001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 195, SalePrice = 175, PurchasePrice = 125, StockQty = 140, HSNCode = "3304", GSTPercent = 18, Description = "Daily moisturizing lotion", Manufacturer = "Cetaphil", Composition = "Glycerin + Dimethicone", Dosage = "Apply as needed", PackSize = "Bottle of 200ml", CategoryId = 8 },
                
                // Eye & Ear Care (Category 9)
                new Medicine { Name = "Refresh Tears Eye Drops", GenericName = "Carboxymethylcellulose", BatchNo = "RFT001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(1), MRP = 120, SalePrice = 108, PurchasePrice = 75, StockQty = 180, HSNCode = "3004", GSTPercent = 12, Description = "Lubricating eye drops", Manufacturer = "Allergan", Composition = "Carboxymethylcellulose 0.5%", Dosage = "1-2 drops as needed", PackSize = "Bottle of 10ml", CategoryId = 9 },
                new Medicine { Name = "Ciprofloxacin Eye Drops", GenericName = "Ciprofloxacin", BatchNo = "CEY001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(1), MRP = 65, SalePrice = 58, PurchasePrice = 40, StockQty = 150, HSNCode = "3004", GSTPercent = 12, Description = "Antibiotic eye drops", Manufacturer = "Sun Pharma", Composition = "Ciprofloxacin 0.3%", Dosage = "1-2 drops every 4-6 hours", PackSize = "Bottle of 5ml", CategoryId = 9, RequiresPrescription = true },
                new Medicine { Name = "Ear Wax Drops", GenericName = "Docusate Sodium", BatchNo = "EWD001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 85, SalePrice = 78, PurchasePrice = 55, StockQty = 120, HSNCode = "3004", GSTPercent = 12, Description = "Ear wax softener", Manufacturer = "FDC", Composition = "Docusate Sodium 5%", Dosage = "2-3 drops in affected ear twice daily", PackSize = "Bottle of 10ml", CategoryId = 9 },
                new Medicine { Name = "Gentamicin Ear Drops", GenericName = "Gentamicin", BatchNo = "GED001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(1).AddMonths(6), MRP = 55, SalePrice = 48, PurchasePrice = 32, StockQty = 140, HSNCode = "3004", GSTPercent = 12, Description = "Antibiotic ear drops", Manufacturer = "Alkem", Composition = "Gentamicin 0.3%", Dosage = "2-3 drops thrice daily", PackSize = "Bottle of 5ml", CategoryId = 9, RequiresPrescription = true },
                
                // First Aid (Category 10)
                new Medicine { Name = "Dettol Antiseptic Liquid", GenericName = "Chloroxylenol", BatchNo = "DTL001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(3), MRP = 85, SalePrice = 78, PurchasePrice = 55, StockQty = 200, HSNCode = "3808", GSTPercent = 18, Description = "Antiseptic for cuts and wounds", Manufacturer = "Reckitt", Composition = "Chloroxylenol 4.8%", Dosage = "Dilute and apply", PackSize = "Bottle of 100ml", CategoryId = 10 },
                new Medicine { Name = "Bandage Roll", GenericName = "Cotton Bandage", BatchNo = "BDG001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(5), MRP = 35, SalePrice = 30, PurchasePrice = 20, StockQty = 300, HSNCode = "3005", GSTPercent = 12, Description = "Cotton bandage for dressing", Manufacturer = "Surgicon", Composition = "100% Cotton", Dosage = "Use as required", PackSize = "Roll of 10cm x 4m", CategoryId = 10 },
                new Medicine { Name = "Band-Aid Strips", GenericName = "Adhesive Bandage", BatchNo = "BAD001", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(3), MRP = 55, SalePrice = 48, PurchasePrice = 32, StockQty = 250, HSNCode = "3005", GSTPercent = 12, Description = "Adhesive bandages for minor cuts", Manufacturer = "Johnson & Johnson", Composition = "Adhesive Strip with Pad", Dosage = "Apply to clean wound", PackSize = "Box of 20", CategoryId = 10, IsFeatured = true },
                new Medicine { Name = "Cotton Wool", GenericName = "Absorbent Cotton", BatchNo = "CTW001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(5), MRP = 45, SalePrice = 40, PurchasePrice = 28, StockQty = 280, HSNCode = "5601", GSTPercent = 12, Description = "Medical grade cotton wool", Manufacturer = "Winner", Composition = "100% Pure Cotton", Dosage = "Use as required", PackSize = "Pack of 50g", CategoryId = 10 },
                new Medicine { Name = "ORS Sachets", GenericName = "Oral Rehydration Salts", BatchNo = "ORS001", ManufactureDate = baseDate.AddMonths(-4), ExpiryDate = baseDate.AddYears(2), MRP = 25, SalePrice = 22, PurchasePrice = 15, StockQty = 400, HSNCode = "3004", GSTPercent = 12, Description = "For dehydration treatment", Manufacturer = "FDC", Composition = "Sodium Chloride, Potassium Chloride, Glucose, Sodium Citrate", Dosage = "Dissolve in 1 liter water", PackSize = "Pack of 5", CategoryId = 10 },
                new Medicine { Name = "Thermometer Digital", GenericName = "Digital Thermometer", BatchNo = "THM001", ManufactureDate = baseDate.AddMonths(-3), ExpiryDate = baseDate.AddYears(5), MRP = 150, SalePrice = 135, PurchasePrice = 95, StockQty = 100, HSNCode = "9025", GSTPercent = 18, Description = "Digital fever thermometer", Manufacturer = "Dr. Morepen", Composition = "Electronic Temperature Sensor", Dosage = "Place under tongue or armpit", PackSize = "1 Unit", CategoryId = 10 }
            };

            // Add near expiry medicines for testing
            medicines.Add(new Medicine { Name = "Expiring Soon Medicine A", GenericName = "Test Generic", BatchNo = "EXP001", ManufactureDate = baseDate.AddYears(-2), ExpiryDate = baseDate.AddDays(45), MRP = 50, SalePrice = 45, PurchasePrice = 30, StockQty = 100, HSNCode = "3004", GSTPercent = 12, Description = "Test near expiry item", Manufacturer = "Test Pharma", Composition = "Test", Dosage = "Test", PackSize = "Strip of 10", CategoryId = 1 });
            medicines.Add(new Medicine { Name = "Expiring Soon Medicine B", GenericName = "Test Generic", BatchNo = "EXP002", ManufactureDate = baseDate.AddYears(-2), ExpiryDate = baseDate.AddDays(60), MRP = 60, SalePrice = 55, PurchasePrice = 35, StockQty = 80, HSNCode = "3004", GSTPercent = 12, Description = "Test near expiry item", Manufacturer = "Test Pharma", Composition = "Test", Dosage = "Test", PackSize = "Strip of 10", CategoryId = 2 });

            // Add low stock medicines for testing
            medicines.Add(new Medicine { Name = "Low Stock Medicine A", GenericName = "Test Generic", BatchNo = "LOW001", ManufactureDate = baseDate.AddMonths(-6), ExpiryDate = baseDate.AddYears(2), MRP = 70, SalePrice = 65, PurchasePrice = 45, StockQty = 5, HSNCode = "3004", GSTPercent = 12, Description = "Test low stock item", Manufacturer = "Test Pharma", Composition = "Test", Dosage = "Test", PackSize = "Strip of 10", CategoryId = 3 });
            medicines.Add(new Medicine { Name = "Low Stock Medicine B", GenericName = "Test Generic", BatchNo = "LOW002", ManufactureDate = baseDate.AddMonths(-5), ExpiryDate = baseDate.AddYears(2), MRP = 80, SalePrice = 75, PurchasePrice = 50, StockQty = 8, HSNCode = "3004", GSTPercent = 12, Description = "Test low stock item", Manufacturer = "Test Pharma", Composition = "Test", Dosage = "Test", PackSize = "Strip of 10", CategoryId = 4 });

            await context.Medicines.AddRangeAsync(medicines);
            await context.SaveChangesAsync();
        }

        private static async Task SeedSuppliersAsync(ApplicationDbContext context)
        {
            if (await context.Suppliers.AnyAsync()) return;

            var suppliers = new List<Supplier>
            {
                new Supplier
                {
                    Name = "Cipla Distributors",
                    ContactPerson = "Rajesh Kumar",
                    Phone = "9876543001",
                    Email = "rajesh@cipladist.com",
                    Address = "123 Pharma Lane, Andheri East",
                    City = "Mumbai",
                    State = "Maharashtra",
                    PinCode = "400069",
                    GSTIN = "27AABCC1234D1ZV",
                    OpeningBalance = 0,
                    CurrentBalance = 0
                },
                new Supplier
                {
                    Name = "Sun Pharma Wholesale",
                    ContactPerson = "Amit Sharma",
                    Phone = "9876543002",
                    Email = "amit@sunpharmawholesale.com",
                    Address = "456 Medicine Road, Malad West",
                    City = "Mumbai",
                    State = "Maharashtra",
                    PinCode = "400064",
                    GSTIN = "27AABCS5678E2ZW",
                    OpeningBalance = 0,
                    CurrentBalance = 0
                },
                new Supplier
                {
                    Name = "Abbott Healthcare Pvt Ltd",
                    ContactPerson = "Priya Singh",
                    Phone = "9876543003",
                    Email = "priya@abbotthealthcare.com",
                    Address = "789 Health Avenue, Gurgaon",
                    City = "Gurgaon",
                    State = "Haryana",
                    PinCode = "122001",
                    GSTIN = "06AABCA9012F3ZX",
                    OpeningBalance = 0,
                    CurrentBalance = 0
                },
                new Supplier
                {
                    Name = "Ranbaxy Pharmaceuticals",
                    ContactPerson = "Vikram Patel",
                    Phone = "9876543004",
                    Email = "vikram@ranbaxy.com",
                    Address = "321 Drug Street, Hyderabad",
                    City = "Hyderabad",
                    State = "Telangana",
                    PinCode = "500001",
                    GSTIN = "36AABCR3456G4ZY",
                    OpeningBalance = 0,
                    CurrentBalance = 0
                },
                new Supplier
                {
                    Name = "GSK Distributors",
                    ContactPerson = "Neha Gupta",
                    Phone = "9876543005",
                    Email = "neha@gskdist.com",
                    Address = "654 Wellness Road, Bengaluru",
                    City = "Bengaluru",
                    State = "Karnataka",
                    PinCode = "560001",
                    GSTIN = "29AABCG7890H5ZZ",
                    OpeningBalance = 0,
                    CurrentBalance = 0
                }
            };

            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();
        }
    }
}
