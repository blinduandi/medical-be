using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using System.Diagnostics;

namespace medical_be.Services;

public class DataSeedingService : IDataSeedingService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DataSeedingService> _logger;
    private readonly Random _random;

    // Medical data arrays for realistic seeding
    private readonly string[] _bloodTypes = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
    private readonly string[] _firstNamesMale = { "John", "James", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Christopher", "Charles", "Daniel", "Matthew", "Anthony", "Mark", "Donald", "Steven", "Paul", "Andrew", "Joshua", "Kenneth", "Kevin", "Brian", "George", "Timothy", "Ronald", "Jason", "Edward", "Jeffrey", "Ryan", "Jacob", "Gary", "Nicholas", "Eric", "Jonathan", "Stephen", "Larry", "Justin", "Scott", "Brandon", "Benjamin", "Samuel", "Gregory", "Alexander", "Patrick", "Frank", "Raymond", "Jack", "Dennis", "Jerry", "Tyler", "Aaron", "Jose", "Henry", "Adam", "Douglas", "Nathan", "Peter", "Zachary", "Kyle", "Walter", "Harold", "Carl", "Arthur", "Gerald", "Roger", "Keith", "Jeremy", "Lawrence", "Sean", "Christian", "Ethan", "Austin", "Juan", "Ralph", "Wayne", "Roy", "Eugene", "Louis", "Philip", "Bobby", "Mason", "Eugene", "Martin", "Albert", "Noah", "Wayne", "Ralph", "Louis", "Philip", "Bobby", "Johnny", "Mason" };
    private readonly string[] _firstNamesFemale = { "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen", "Nancy", "Lisa", "Betty", "Helen", "Sandra", "Donna", "Carol", "Ruth", "Sharon", "Michelle", "Laura", "Sarah", "Kimberly", "Deborah", "Dorothy", "Lisa", "Nancy", "Karen", "Betty", "Helen", "Sandra", "Donna", "Carol", "Ruth", "Sharon", "Michelle", "Laura", "Sarah", "Kimberly", "Deborah", "Dorothy", "Amy", "Angela", "Ashley", "Brenda", "Emma", "Olivia", "Cynthia", "Marie", "Janet", "Catherine", "Frances", "Christine", "Samantha", "Debra", "Rachel", "Carolyn", "Janet", "Virginia", "Maria", "Heather", "Diane", "Julie", "Joyce", "Victoria", "Kelly", "Christina", "Joan", "Evelyn", "Lauren", "Judith", "Megan", "Cheryl", "Andrea", "Hannah", "Jacqueline", "Martha", "Gloria", "Teresa", "Sara", "Janice", "Marie", "Julia", "Heather", "Diane", "Ruth", "Julie", "Joyce", "Virginia", "Victoria", "Kelly", "Christina", "Joan", "Evelyn", "Lauren", "Judith", "Megan", "Cheryl", "Andrea", "Hannah", "Amanda", "Stephanie", "Carolyn", "Christine", "Marie", "Janet", "Catherine", "Frances", "Christine", "Samantha" };
    private readonly string[] _lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores", "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell", "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker", "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy", "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson", "Bailey", "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson", "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza", "Ruiz", "Hughes", "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers", "Long", "Ross", "Foster", "Jimenez" };

    private readonly string[] _allergens = { "Peanuts", "Tree nuts", "Milk", "Eggs", "Wheat", "Soy", "Fish", "Shellfish", "Sesame", "Latex", "Dust mites", "Pollen", "Pet dander", "Mold", "Insect stings", "Medications", "Food additives", "Nickel", "Fragrance", "Formaldehyde" };
    private readonly string[] _vaccines = { "COVID-19", "Influenza", "Hepatitis B", "Tetanus", "Measles", "Mumps", "Rubella", "Polio", "Pneumococcal", "HPV", "Meningitis", "Chickenpox", "Shingles", "Hepatitis A", "Rabies", "Yellow Fever", "Typhoid", "Japanese Encephalitis", "Cholera", "Anthrax" };
    
    private readonly string[] _labTests = { "Complete Blood Count", "Basic Metabolic Panel", "Lipid Panel", "Liver Function Tests", "Thyroid Function", "Hemoglobin A1C", "Glucose", "Creatinine", "Blood Urea Nitrogen", "Cholesterol", "Triglycerides", "HDL Cholesterol", "LDL Cholesterol", "C-Reactive Protein", "Vitamin D", "Vitamin B12", "Iron", "Ferritin", "PSA", "TSH", "Free T4", "Insulin", "Cortisol", "Testosterone", "Estrogen", "Progesterone", "Prolactin", "Growth Hormone", "Parathyroid Hormone", "Calcium", "Phosphorus", "Magnesium", "Potassium", "Sodium", "Chloride", "CO2", "Albumin", "Total Protein", "Bilirubin", "AST", "ALT", "Alkaline Phosphatase", "GGT", "Troponin", "BNP", "D-Dimer", "PT/INR", "PTT", "Fibrinogen" };
    
    private readonly Dictionary<string, (decimal min, decimal max, string unit)> _labRanges = new()
    {
        { "Glucose", (70, 200, "mg/dL") },
        { "Cholesterol", (150, 350, "mg/dL") },
        { "Triglycerides", (50, 400, "mg/dL") },
        { "HDL Cholesterol", (30, 80, "mg/dL") },
        { "LDL Cholesterol", (50, 200, "mg/dL") },
        { "Creatinine", (0.6m, 1.5m, "mg/dL") },
        { "Blood Urea Nitrogen", (7, 25, "mg/dL") },
        { "Hemoglobin A1C", (4.0m, 12.0m, "%") },
        { "TSH", (0.5m, 5.0m, "mIU/L") },
        { "Vitamin D", (10, 80, "ng/mL") },
        { "Iron", (50, 200, "mcg/dL") },
        { "C-Reactive Protein", (0.1m, 10.0m, "mg/L") },
        { "Complete Blood Count", (4.0m, 12.0m, "x10^3/uL") },
        { "Liver Function Tests", (10, 50, "U/L") },
        { "Basic Metabolic Panel", (95, 105, "mEq/L") }
    };

    private readonly string[] _diagnosisCategories = { "Cancer", "Cardiovascular", "Diabetes", "Respiratory", "Neurological", "Musculoskeletal", "Gastrointestinal", "Endocrine", "Psychiatric", "Infectious Disease", "Autoimmune", "Kidney Disease", "Skin Disorders", "Blood Disorders", "Genetic Disorders" };
    private readonly Dictionary<string, string[]> _diagnosisByCategory = new()
    {
        { "Cancer", new[] { "Breast Cancer", "Lung Cancer", "Prostate Cancer", "Colorectal Cancer", "Skin Cancer", "Lymphoma", "Leukemia", "Liver Cancer", "Pancreatic Cancer", "Bladder Cancer" } },
        { "Cardiovascular", new[] { "Hypertension", "Coronary Artery Disease", "Heart Failure", "Atrial Fibrillation", "Myocardial Infarction", "Stroke", "Peripheral Artery Disease", "Deep Vein Thrombosis", "Pulmonary Embolism", "Cardiomyopathy" } },
        { "Diabetes", new[] { "Type 1 Diabetes", "Type 2 Diabetes", "Gestational Diabetes", "Prediabetes", "Diabetic Nephropathy", "Diabetic Retinopathy", "Diabetic Neuropathy", "Diabetic Ketoacidosis", "Hypoglycemia", "Metabolic Syndrome" } },
        { "Respiratory", new[] { "Asthma", "COPD", "Pneumonia", "Bronchitis", "Sleep Apnea", "Pulmonary Fibrosis", "Lung Cancer", "Tuberculosis", "Emphysema", "Pleural Effusion" } },
        { "Neurological", new[] { "Alzheimer's Disease", "Parkinson's Disease", "Multiple Sclerosis", "Epilepsy", "Migraine", "Stroke", "Dementia", "Neuropathy", "Brain Tumor", "Spinal Cord Injury" } },
        { "Musculoskeletal", new[] { "Arthritis", "Osteoporosis", "Fibromyalgia", "Back Pain", "Osteoarthritis", "Rheumatoid Arthritis", "Gout", "Tendonitis", "Bursitis", "Fractures" } },
        { "Gastrointestinal", new[] { "Gastroesophageal Reflux", "Irritable Bowel Syndrome", "Crohn's Disease", "Ulcerative Colitis", "Peptic Ulcer", "Hepatitis", "Cirrhosis", "Gallstones", "Pancreatitis", "Celiac Disease" } },
        { "Endocrine", new[] { "Hypothyroidism", "Hyperthyroidism", "Diabetes", "Adrenal Insufficiency", "Cushing's Syndrome", "PCOS", "Osteoporosis", "Growth Hormone Deficiency", "Hyperparathyroidism", "Pituitary Disorders" } },
        { "Psychiatric", new[] { "Depression", "Anxiety", "Bipolar Disorder", "Schizophrenia", "PTSD", "ADHD", "OCD", "Eating Disorders", "Substance Abuse", "Personality Disorders" } },
        { "Infectious Disease", new[] { "COVID-19", "Influenza", "Pneumonia", "Urinary Tract Infection", "Skin Infections", "Hepatitis", "HIV", "Tuberculosis", "Sepsis", "Meningitis" } }
    };

    public DataSeedingService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        ILogger<DataSeedingService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _random = new Random(12345); // Fixed seed for reproducible data
    }

    public async Task<bool> IsDataAlreadySeededAsync()
    {
        var userCount = await _context.Users.CountAsync();
        return userCount >= 1000; // Consider seeded if we have at least 1000 users
    }

    public async Task<DataSeedingResultDto> SeedLargeDatasetAsync(int userCount = 10000)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();
        var result = new DataSeedingResultDto
        {
            Success = false,
            Message = "Data seeding started",
            CompletedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting to seed {UserCount} users with comprehensive medical data...", userCount);

            var isAlreadySeeded = await IsDataAlreadySeededAsync();
            if (isAlreadySeeded)
            {
                warnings.Add("Dataset already exists - operation skipped");
                result.Message = "Data already seeded";
                result.Success = true;
                result.ProcessingTime = stopwatch.Elapsed;
                result.Warnings = warnings;
                _logger.LogInformation("Data already seeded. Skipping...");
                return result;
            }

            // Determine offset to avoid duplicate user emails on repeated seeding runs
            var existingGeneratedUsers = await _context.Users
                .AsNoTracking()
                .CountAsync(u => (u.Email ?? string.Empty).StartsWith("user") && (u.Email ?? string.Empty).EndsWith("@medical.com"));

            // Start with creating users in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)userCount / batchSize);
            int totalUsersCreated = 0, totalVisitsCreated = 0, totalAllergiesCreated = 0;
            int totalVaccinationsCreated = 0, totalLabResultsCreated = 0, totalDiagnosesCreated = 0;
            int totalAppointmentsCreated = 0, totalRatingsCreated = 0;

            for (int batch = 0; batch < totalBatches; batch++)
            {
                var currentBatchSize = Math.Min(batchSize, userCount - (batch * batchSize));
                _logger.LogInformation("Processing batch {Batch}/{TotalBatches} - Creating {BatchSize} users...", 
                    batch + 1, totalBatches, currentBatchSize);

                var batchResult = await SeedUserBatch(currentBatchSize, existingGeneratedUsers + batch * batchSize);
                totalUsersCreated += batchResult.UsersCreated;
                totalAppointmentsCreated += batchResult.AppointmentsCreated;
                totalRatingsCreated += batchResult.RatingsCreated;
                totalVisitsCreated += batchResult.VisitsCreated;
                totalAllergiesCreated += batchResult.AllergiesCreated;
                totalVaccinationsCreated += batchResult.VaccinationsCreated;
                totalLabResultsCreated += batchResult.LabResultsCreated;
                totalDiagnosesCreated += batchResult.DiagnosesCreated;

                // Small delay between batches to prevent overwhelming the database
                await Task.Delay(1000);
            }

            result.UsersCreated = totalUsersCreated;
            result.AppointmentsCreated = totalAppointmentsCreated;
            result.RatingsCreated = totalRatingsCreated;
            result.VisitsCreated = totalVisitsCreated;
            result.AllergiesCreated = totalAllergiesCreated;
            result.VaccinationsCreated = totalVaccinationsCreated;
            result.LabResultsCreated = totalLabResultsCreated;
            result.DiagnosesCreated = totalDiagnosesCreated;
            result.Success = true;
            result.Message = $"Successfully seeded {totalUsersCreated} users with comprehensive medical data";
            result.ProcessingTime = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
            result.Warnings = warnings;

            _logger.LogInformation("Successfully seeded {UserCount} users with comprehensive medical data in {Time}!", 
                totalUsersCreated, stopwatch.Elapsed);
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Error during seeding: {ex.Message}";
            result.ProcessingTime = stopwatch.Elapsed;
            result.Warnings = warnings;
            _logger.LogError(ex, "Error seeding large dataset");
            return result;
        }
    }

    private async Task<BatchResult> SeedUserBatch(int batchSize, int startIndex)
    {
        var users = new List<User>();
        var visitRecords = new List<VisitRecord>();
        var allergies = new List<Allergy>();
        var vaccinations = new List<Vaccination>();
        var labResults = new List<LabResult>();
        var diagnoses = new List<Diagnosis>();
        var appointments = new List<Appointment>();
        var ratings = new List<Rating>();

        // Determine doctors vs patients ratio (20% doctors, 80% patients)
        var doctorCount = (int)(batchSize * 0.2);
        var patientCount = batchSize - doctorCount;
        
        var doctors = new List<User>();
        var patients = new List<User>();

        // Create doctors first
        for (int i = 0; i < doctorCount; i++)
        {
            var userIndex = startIndex + i;
            var doctor = CreateRandomDoctor(userIndex);
            doctors.Add(doctor);
            users.Add(doctor);
        }

        // Create patients
        for (int i = 0; i < patientCount; i++)
        {
            var userIndex = startIndex + doctorCount + i;
            var patient = CreateRandomPatient(userIndex);
            patients.Add(patient);
            users.Add(patient);
        }

        // Add users to context and assign roles
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
        
        // Assign roles
        var doctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "DOCTOR");
        var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "PATIENT");
        
        if (doctorRole != null)
        {
            foreach (var doctor in doctors)
            {
                await _userManager.AddToRoleAsync(doctor, "Doctor");
            }
        }
        
        if (patientRole != null)
        {
            foreach (var patient in patients)
            {
                await _userManager.AddToRoleAsync(patient, "Patient");
            }
        }

        // Create appointments between patients and doctors
        if (doctors.Any() && patients.Any())
        {
            foreach (var patient in patients)
            {
                var appointmentCount = _random.Next(2, 8); // 2-7 appointments per patient
                for (int a = 0; a < appointmentCount; a++)
                {
                    var doctor = doctors[_random.Next(doctors.Count)];
                    appointments.Add(CreateRandomAppointment(patient.Id, doctor.Id));
                }
            }
        }

        // Create ratings for doctors
        foreach (var doctor in doctors)
        {
            var ratingCount = _random.Next(5, 25); // 5-24 ratings per doctor
            var ratedPatients = new HashSet<string>();
            
            for (int r = 0; r < ratingCount && ratedPatients.Count < patients.Count; r++)
            {
                var patient = patients[_random.Next(patients.Count)];
                if (!ratedPatients.Contains(patient.Id))
                {
                    ratedPatients.Add(patient.Id);
                    ratings.Add(CreateRandomRating(patient.Id, doctor.Id));
                }
            }
        }

        // Now create medical data for each patient
        foreach (var patient in patients)
        {
            // Select a random doctor for medical records
            var assignedDoctor = doctors.Any() ? doctors[_random.Next(doctors.Count)].Id : patient.Id;
            
            // Create visit records (2-15 visits per patient)
            var visitCount = _random.Next(2, 16);
            for (int v = 0; v < visitCount; v++)
            {
                visitRecords.Add(CreateRandomVisitRecord(patient.Id, assignedDoctor));
            }

            // Create allergies (0-5 allergies per patient)
            var allergyCount = _random.Next(0, 6);
            var usedAllergens = new HashSet<string>();
            for (int a = 0; a < allergyCount; a++)
            {
                var allergy = CreateRandomAllergy(patient.Id, usedAllergens, assignedDoctor);
                if (allergy != null)
                    allergies.Add(allergy);
            }

            // Create vaccinations (3-12 vaccinations per patient)
            var vaccinationCount = _random.Next(3, 13);
            var usedVaccines = new HashSet<string>();
            for (int v = 0; v < vaccinationCount; v++)
            {
                var vaccination = CreateRandomVaccination(patient.Id, usedVaccines, assignedDoctor);
                if (vaccination != null)
                    vaccinations.Add(vaccination);
            }

            // Create lab results (5-30 lab results per patient)
            var labCount = _random.Next(5, 31);
            for (int l = 0; l < labCount; l++)
            {
                labResults.Add(CreateRandomLabResult(patient.Id));
            }

            // Create diagnoses (1-6 diagnoses per patient)
            var diagnosisCount = _random.Next(1, 7);
            for (int d = 0; d < diagnosisCount; d++)
            {
                diagnoses.Add(CreateRandomDiagnosis(patient.Id, assignedDoctor));
            }
        }

        // Add all medical data in batches to avoid memory issues
        if (appointments.Any())
        {
            _context.Appointments.AddRange(appointments);
            await _context.SaveChangesAsync();
        }

        if (ratings.Any())
        {
            _context.Ratings.AddRange(ratings);
            await _context.SaveChangesAsync();
        }

        if (visitRecords.Any())
        {
            _context.VisitRecords.AddRange(visitRecords);
            await _context.SaveChangesAsync();
        }

        if (allergies.Any())
        {
            _context.Allergies.AddRange(allergies);
            await _context.SaveChangesAsync();
        }

        if (vaccinations.Any())
        {
            _context.Vaccinations.AddRange(vaccinations);
            await _context.SaveChangesAsync();
        }

        if (labResults.Any())
        {
            _context.LabResults.AddRange(labResults);
            await _context.SaveChangesAsync();
        }

        if (diagnoses.Any())
        {
            _context.Diagnoses.AddRange(diagnoses);
            await _context.SaveChangesAsync();
        }

        return new BatchResult
        {
            UsersCreated = users.Count,
            DoctorsCreated = doctors.Count,
            PatientsCreated = patients.Count,
            AppointmentsCreated = appointments.Count,
            RatingsCreated = ratings.Count,
            VisitsCreated = visitRecords.Count,
            AllergiesCreated = allergies.Count,
            VaccinationsCreated = vaccinations.Count,
            LabResultsCreated = labResults.Count,
            DiagnosesCreated = diagnoses.Count
        };
    }

    private User CreateRandomDoctor(int index)
    {
        var gender = _random.Next(2) == 0 ? Gender.Male : Gender.Female;
        var firstName = gender == Gender.Male 
            ? _firstNamesMale[_random.Next(_firstNamesMale.Length)]
            : _firstNamesFemale[_random.Next(_firstNamesFemale.Length)];
        var lastName = _lastNames[_random.Next(_lastNames.Length)];
        
        var birthDate = DateTime.Now.AddYears(-_random.Next(30, 65))
                                   .AddDays(-_random.Next(0, 365));

        var specialties = new[] { DoctorSpecialty.Cardiology, DoctorSpecialty.Neurology, DoctorSpecialty.Pediatrics, 
                                  DoctorSpecialty.Orthopedics, DoctorSpecialty.Dermatology, DoctorSpecialty.Oncology, 
                                  DoctorSpecialty.Psychiatry, DoctorSpecialty.Surgery, DoctorSpecialty.Radiology, 
                                  DoctorSpecialty.Endocrinology, DoctorSpecialty.Gastroenterology, DoctorSpecialty.Pulmonology,
                                  DoctorSpecialty.GeneralPractice, DoctorSpecialty.Gynecology };

        return new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"doctor{index:D6}@medical.com",
            Email = $"doctor{index:D6}@medical.com",
            NormalizedEmail = $"DOCTOR{index:D6}@MEDICAL.COM",
            NormalizedUserName = $"DOCTOR{index:D6}@MEDICAL.COM",
            EmailConfirmed = true,
            IDNP = GenerateIDNP(),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = GeneratePhoneNumber(),
            DateOfBirth = birthDate,
            Gender = gender,
            BloodType = _bloodTypes[_random.Next(_bloodTypes.Length)],
            Address = GenerateRandomAddress(),
            Specialty = specialties[_random.Next(specialties.Length)],
            Experience = $"{_random.Next(2, 35)} years",
            ClinicId = $"CLINIC_{_random.Next(1, 11)}",
            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(365, 1825)),
            IsActive = true,
            IsEmailVerified = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
    }

    private User CreateRandomPatient(int index)
    {
        var gender = _random.Next(2) == 0 ? Gender.Male : Gender.Female;
        var firstName = gender == Gender.Male 
            ? _firstNamesMale[_random.Next(_firstNamesMale.Length)]
            : _firstNamesFemale[_random.Next(_firstNamesFemale.Length)];
        var lastName = _lastNames[_random.Next(_lastNames.Length)];
        
        var birthDate = DateTime.Now.AddYears(-_random.Next(1, 95))
                                   .AddDays(-_random.Next(0, 365));

        return new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"patient{index:D6}@medical.com",
            Email = $"patient{index:D6}@medical.com",
            NormalizedEmail = $"PATIENT{index:D6}@MEDICAL.COM",
            NormalizedUserName = $"PATIENT{index:D6}@MEDICAL.COM",
            EmailConfirmed = true,
            IDNP = GenerateIDNP(),
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = GeneratePhoneNumber(),
            DateOfBirth = birthDate,
            Gender = gender,
            BloodType = _bloodTypes[_random.Next(_bloodTypes.Length)],
            Address = GenerateRandomAddress(),
            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 730)),
            IsActive = _random.Next(100) < 98, // 98% are active
            IsEmailVerified = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
    }

    private VisitRecord CreateRandomVisitRecord(string patientId, string doctorId)
    {
        var visitTypes = Enum.GetValues<VisitType>();
        var visitDate = DateTime.UtcNow.AddDays(-_random.Next(0, 730)); // Last 2 years

        return new VisitRecord
        {
            PatientId = patientId,
            DoctorId = doctorId,
            VisitDate = visitDate,
            VisitType = visitTypes[_random.Next(visitTypes.Length)],
            Symptoms = GenerateRandomSymptoms(),
            Diagnosis = GenerateRandomDiagnosis(),
            Treatment = GenerateRandomTreatment(),
            Prescription = GenerateRandomPrescription(),
            Notes = GenerateRandomNotes(),
            CreatedAt = visitDate.AddMinutes(_random.Next(0, 60))
        };
    }

    private Appointment CreateRandomAppointment(string patientId, string doctorId)
    {
        var futureDate = _random.Next(2) == 0; // 50% future, 50% past
        DateTime appointmentDate;
        AppointmentStatus status;

        if (futureDate)
        {
            appointmentDate = DateTime.UtcNow.AddDays(_random.Next(1, 60));
            status = _random.Next(10) < 2 ? AppointmentStatus.Cancelled : AppointmentStatus.Scheduled;
        }
        else
        {
            appointmentDate = DateTime.UtcNow.AddDays(-_random.Next(1, 365));
            var rand = _random.Next(10);
            status = rand < 7 ? AppointmentStatus.Completed : 
                    rand < 9 ? AppointmentStatus.Cancelled : AppointmentStatus.NoShow;
        }

        var hour = _random.Next(8, 17); // 8 AM to 5 PM
        appointmentDate = appointmentDate.Date.AddHours(hour).AddMinutes(_random.Next(0, 4) * 15); // 15-min intervals

        return new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = appointmentDate,
            Duration = TimeSpan.FromMinutes(_random.Next(2, 5) * 15), // 30, 45, or 60 minutes
            Status = status,
            Reason = GenerateRandomComplaint(),
            Notes = _random.Next(10) < 3 ? GenerateRandomNotes() : null,
            CreatedAt = appointmentDate.AddDays(-_random.Next(1, 30))
        };
    }

    private Rating CreateRandomRating(string patientId, string doctorId)
    {
        return new Rating
        {
            PatientId = patientId,
            DoctorId = doctorId,
            RatingNr = _random.Next(3, 6), // 3-5 stars (mostly positive)
            RatingCommentary = GenerateRandomRatingComment(),
            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 365))
        };
    }

    private Allergy? CreateRandomAllergy(string patientId, HashSet<string> usedAllergens, string recordedById)
    {
        var availableAllergens = _allergens.Where(a => !usedAllergens.Contains(a)).ToList();
        if (!availableAllergens.Any()) return null;

        var allergen = availableAllergens[_random.Next(availableAllergens.Count)];
        usedAllergens.Add(allergen);

        var severities = Enum.GetValues<AllergySeverity>();
        var reactions = new[] { "Rash", "Hives", "Swelling", "Difficulty breathing", "Anaphylaxis", "Nausea", "Vomiting", "Diarrhea", "Runny nose", "Watery eyes", "Cough", "Wheezing" };

        return new Allergy
        {
            PatientId = patientId,
            RecordedById = recordedById,
            AllergenName = allergen,
            Severity = severities[_random.Next(severities.Length)],
            Reaction = reactions[_random.Next(reactions.Length)],
            DiagnosedDate = DateTime.UtcNow.AddDays(-_random.Next(0, 1825)), // Last 5 years
            Notes = _random.Next(10) < 3 ? "Additional allergy notes here" : null,
            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 1825))
        };
    }

    private Vaccination? CreateRandomVaccination(string patientId, HashSet<string> usedVaccines, string administeredById)
    {
        var availableVaccines = _vaccines.Where(v => !usedVaccines.Contains(v)).ToList();
        if (!availableVaccines.Any()) return null;

        var vaccine = availableVaccines[_random.Next(availableVaccines.Count)];
        usedVaccines.Add(vaccine);

        return new Vaccination
        {
            PatientId = patientId,
            AdministeredById = administeredById,
            VaccineName = vaccine,
            DateAdministered = DateTime.UtcNow.AddDays(-_random.Next(0, 1825)),
            BatchNumber = $"BATCH{_random.Next(10000, 99999)}",
            Manufacturer = new[] { "Pfizer", "Moderna", "Johnson & Johnson", "AstraZeneca", "Novavax" }[_random.Next(5)],
            Notes = _random.Next(10) < 2 ? "Vaccination completed without complications" : null,
            CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(0, 1825))
        };
    }

    private LabResult CreateRandomLabResult(string patientId)
    {
        var testName = _labTests[_random.Next(_labTests.Length)];
        var testDate = DateTime.UtcNow.AddDays(-_random.Next(0, 365)); // Last year
        
        decimal value;
        string unit;
        string status;

        if (_labRanges.ContainsKey(testName))
        {
            var range = _labRanges[testName];
            // Generate values that are sometimes normal, sometimes abnormal
            var isAbnormal = _random.Next(100) < 30; // 30% abnormal
            
            if (isAbnormal)
            {
                // Generate abnormal values
                var isHigh = _random.Next(2) == 0;
                if (isHigh)
                {
                    value = range.max + (_random.Next(1, 50) / 100m * range.max);
                    status = _random.Next(10) < 2 ? "CRITICAL" : "HIGH";
                }
                else
                {
                    value = Math.Max(0, range.min - (_random.Next(1, 30) / 100m * range.min));
                    status = _random.Next(10) < 2 ? "CRITICAL" : "LOW";
                }
            }
            else
            {
                // Generate normal values
                value = range.min + (_random.Next(0, 100) / 100m * (range.max - range.min));
                status = "NORMAL";
            }
            
            unit = range.unit;
        }
        else
        {
            // Default values for tests not in our range dictionary
            value = _random.Next(1, 100) + (_random.Next(0, 100) / 100m);
            unit = "units";
            status = new[] { "NORMAL", "HIGH", "LOW" }[_random.Next(3)];
        }

        return new LabResult
        {
            PatientId = patientId,
            TestName = testName,
            TestCode = $"LAB{_random.Next(1000, 9999)}",
            Value = value,
            Unit = unit,
            ReferenceMin = _labRanges.ContainsKey(testName) ? _labRanges[testName].min : null,
            ReferenceMax = _labRanges.ContainsKey(testName) ? _labRanges[testName].max : null,
            Status = status,
            TestDate = testDate,
            LabName = new[] { "Central Lab", "Regional Medical Lab", "City Hospital Lab", "University Lab", "Private Lab Services" }[_random.Next(5)],
            Notes = _random.Next(10) < 2 ? "Lab result notes" : null,
            CreatedAt = testDate.AddHours(_random.Next(1, 24))
        };
    }

    private Diagnosis CreateRandomDiagnosis(string patientId, string doctorId)
    {
        var category = _diagnosisCategories[_random.Next(_diagnosisCategories.Length)];
        var diagnosisName = _diagnosisByCategory.ContainsKey(category) 
            ? _diagnosisByCategory[category][_random.Next(_diagnosisByCategory[category].Length)]
            : "General Medical Condition";

        var diagnosedDate = DateTime.UtcNow.AddDays(-_random.Next(0, 1825)); // Last 5 years
        var isActive = diagnosedDate > DateTime.UtcNow.AddYears(-2) && _random.Next(100) < 70; // 70% of recent diagnoses are still active

        return new Diagnosis
        {
            PatientId = patientId,
            DiagnosisCode = $"{category[0]}{_random.Next(10, 99)}.{_random.Next(0, 9)}",
            DiagnosisName = diagnosisName,
            Description = $"Patient diagnosed with {diagnosisName.ToLower()}",
            Severity = new[] { "Mild", "Moderate", "Severe" }[_random.Next(3)],
            Category = category,
            DiagnosedDate = diagnosedDate,
            DoctorId = doctorId,
            IsActive = isActive,
            ResolvedDate = !isActive ? diagnosedDate.AddDays(_random.Next(30, 365)) : null,
            Notes = _random.Next(10) < 3 ? "Additional diagnosis notes" : null,
            CreatedAt = diagnosedDate.AddMinutes(_random.Next(0, 60))
        };
    }

    private string GenerateRandomPrescription()
    {
        var medications = new[]
        {
            "Lisinopril 10mg once daily",
            "Metformin 500mg twice daily",
            "Atorvastatin 20mg at bedtime",
            "Omeprazole 20mg before breakfast",
            "Levothyroxine 50mcg once daily",
            "Amlodipine 5mg once daily",
            "Metoprolol 25mg twice daily",
            "Losartan 50mg once daily",
            "Gabapentin 300mg three times daily",
            "Sertraline 50mg once daily"
        };
        
        return medications[_random.Next(medications.Length)];
    }

    private string GenerateRandomRatingComment()
    {
        var comments = new[]
        {
            "Excellent doctor, very professional and caring",
            "Great experience, highly recommend",
            "Very knowledgeable and patient",
            "Took time to explain everything clearly",
            "Friendly staff and good bedside manner",
            "Professional and thorough examination",
            "Good communication and follow-up",
            "Helped me understand my condition better",
            "Quick appointment, efficient service",
            "Compassionate and understanding",
            "Experienced and trustworthy",
            "Would definitely visit again"
        };
        
        return _random.Next(10) < 8 ? comments[_random.Next(comments.Length)] : null;
    }

    // Helper methods for generating random data
    private string GenerateIDNP()
    {
        // Generate a 13-digit number (no leading zeros). Use exclusive upper bound.
        // Range: [1_000_000_000_000, 10_000_000_000_000)
        var idnp = _random.NextInt64(1_000_000_000_000, 10_000_000_000_000).ToString();
        return idnp;
    }

    private string GeneratePhoneNumber()
    {
        return $"+373{_random.Next(60000000, 99999999)}";
    }

    private string GenerateRandomAddress()
    {
        var streets = new[] { "Main St", "Oak Ave", "Elm St", "Pine Rd", "Cedar Ln", "Maple Dr", "First St", "Second St", "Park Ave", "Washington St" };
        var cities = new[] { "Chisinau", "Balti", "Tiraspol", "Bender", "Rybnitsa", "Soroca", "Orhei", "Ungheni", "Cahul", "Comrat" };
        
        return $"{_random.Next(1, 999)} {streets[_random.Next(streets.Length)]}, {cities[_random.Next(cities.Length)]}";
    }

    private string GenerateRandomComplaint()
    {
        var complaints = new[] { "Chest pain", "Shortness of breath", "Abdominal pain", "Headache", "Fatigue", "Fever", "Cough", "Nausea", "Dizziness", "Back pain", "Joint pain", "Skin rash", "Insomnia", "Anxiety", "Depression" };
        return complaints[_random.Next(complaints.Length)];
    }

    private string GenerateRandomDiagnosis()
    {
        var diagnoses = new[] { "Hypertension", "Diabetes Type 2", "Upper respiratory infection", "Migraine", "Gastritis", "Anxiety disorder", "Musculoskeletal pain", "Allergic reaction", "Viral infection", "Bacterial infection" };
        return diagnoses[_random.Next(diagnoses.Length)];
    }

    private string GenerateRandomTreatment()
    {
        var treatments = new[] { "Medication prescribed", "Rest and observation", "Physical therapy", "Lifestyle modifications", "Follow-up in 2 weeks", "Specialist referral", "Laboratory tests ordered", "Imaging studies ordered", "Symptom management", "Dietary counseling" };
        return treatments[_random.Next(treatments.Length)];
    }

    private string GenerateRandomNotes()
    {
        var notes = new[] { "Patient responding well to treatment", "Continue current medication", "Monitor symptoms", "Patient education provided", "No adverse reactions noted", "Follow-up as needed", "Stable condition", "Symptoms improving", "Additional evaluation needed", "Routine follow-up" };
        return _random.Next(10) < 7 ? notes[_random.Next(notes.Length)] : "";
    }

    private string GenerateRandomSymptoms()
    {
        var symptoms = new[] 
        { 
            "Headache", "Fever", "Cough", "Fatigue", "Nausea", "Dizziness", "Chest pain", 
            "Shortness of breath", "Abdominal pain", "Back pain", "Joint pain", "Muscle aches",
            "Rash", "Insomnia", "Loss of appetite", "Weight loss", "Weight gain", "Swelling",
            "Bleeding", "Bruising", "Vision problems", "Hearing problems", "Memory issues"
        };
        
        var symptomCount = _random.Next(1, 4); // 1-3 symptoms
        var selectedSymptoms = new List<string>();
        
        for (int i = 0; i < symptomCount; i++)
        {
            var symptom = symptoms[_random.Next(symptoms.Length)];
            if (!selectedSymptoms.Contains(symptom))
            {
                selectedSymptoms.Add(symptom);
            }
        }
        
        return string.Join(", ", selectedSymptoms);
    }

    public async Task<DataSeedingStatusDto> GetSeedingStatusAsync()
    {
        var userCount = await _context.Users.CountAsync();
        var visitCount = await _context.VisitRecords.CountAsync();
        var allergyCount = await _context.Allergies.CountAsync();
        var vaccinationCount = await _context.Vaccinations.CountAsync();
        var labResultCount = await _context.LabResults.CountAsync();
        var diagnosisCount = await _context.Diagnoses.CountAsync();

        return new DataSeedingStatusDto
        {
            TotalUsers = userCount,
            TotalVisits = visitCount,
            TotalAllergies = allergyCount,
            TotalVaccinations = vaccinationCount,
            TotalLabResults = labResultCount,
            TotalDiagnoses = diagnosisCount,
            IsLargeDatasetSeeded = userCount >= 1000,
            LastSeedingDate = DateTime.UtcNow,
            AverageDataPerUser = userCount > 0 ? (double)(visitCount + allergyCount + vaccinationCount + labResultCount + diagnosisCount) / userCount : 0,
            Status = userCount >= 1000 ? "Large dataset seeded" : userCount > 0 ? "Small dataset available" : "No data seeded"
        };
    }

    public async Task ClearSeedDataAsync()
    {
        try
        {
            _logger.LogWarning("Clearing all seeded medical data...");

            // Clear in reverse order of dependencies
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Diagnoses");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM LabResults");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Vaccinations");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Allergies");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM VisitRecords");
            
            // Delete users that were seeded (identifiable by email pattern)
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers WHERE Email LIKE 'user%@medical.com'");

            _logger.LogInformation("All seeded data cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing seeded data");
            throw;
        }
    }

    public async Task<DataSeedingResultDto> SeedPatientsForDoctorAsync(string doctorId, int patientCount = 20)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new DataSeedingResultDto
        {
            Success = false,
            Message = "Seeding patients for doctor",
            CompletedAt = DateTime.UtcNow
        };

        try
        {
            // Verify doctor exists
            var doctor = await _context.Users.FindAsync(doctorId);
            if (doctor == null)
            {
                result.Message = "Doctor not found";
                return result;
            }

            _logger.LogInformation("Seeding {Count} patients for doctor {DoctorName}...", 
                patientCount, $"{doctor.FirstName} {doctor.LastName}");

            var patients = new List<User>();
            var appointments = new List<Appointment>();
            var ratings = new List<Rating>();

            // Get existing patient count to avoid duplicates
            var existingPatients = await _context.Users
                .AsNoTracking()
                .CountAsync(u => (u.Email ?? string.Empty).StartsWith("patient") && (u.Email ?? string.Empty).EndsWith("@medical.com"));

            // Create patients
            for (int i = 0; i < patientCount; i++)
            {
                var patient = CreateRandomPatient(existingPatients + i);
                patients.Add(patient);
            }

            // Add patients to database
            _context.Users.AddRange(patients);
            await _context.SaveChangesAsync();

            // Assign patient role
            var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "PATIENT");
            if (patientRole != null)
            {
                foreach (var patient in patients)
                {
                    await _userManager.AddToRoleAsync(patient, "Patient");
                }
            }

            // Create appointments for each patient with this doctor
            foreach (var patient in patients)
            {
                var appointmentCount = _random.Next(3, 8); // 3-7 appointments per patient
                for (int a = 0; a < appointmentCount; a++)
                {
                    appointments.Add(CreateRandomAppointment(patient.Id, doctorId));
                }
            }

            // Create ratings from patients for this doctor
            foreach (var patient in patients)
            {
                if (_random.Next(100) < 70) // 70% chance patient rates the doctor
                {
                    ratings.Add(CreateRandomRating(patient.Id, doctorId));
                }
            }

            // Save appointments and ratings
            if (appointments.Any())
            {
                _context.Appointments.AddRange(appointments);
                await _context.SaveChangesAsync();
            }

            if (ratings.Any())
            {
                _context.Ratings.AddRange(ratings);
                await _context.SaveChangesAsync();
            }

            result.Success = true;
            result.PatientsCreated = patients.Count;
            result.AppointmentsCreated = appointments.Count;
            result.RatingsCreated = ratings.Count;
            result.Message = $"Successfully created {patients.Count} patients, {appointments.Count} appointments, and {ratings.Count} ratings for doctor {doctor.FirstName} {doctor.LastName}";
            result.ProcessingTime = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(result.Message);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding patients for doctor");
            result.Message = $"Error: {ex.Message}";
            return result;
        }
    }

    private class BatchResult
    {
        public int UsersCreated { get; set; }
        public int DoctorsCreated { get; set; }
        public int PatientsCreated { get; set; }
        public int AppointmentsCreated { get; set; }
        public int RatingsCreated { get; set; }
        public int VisitsCreated { get; set; }
        public int AllergiesCreated { get; set; }
        public int VaccinationsCreated { get; set; }
        public int LabResultsCreated { get; set; }
        public int DiagnosesCreated { get; set; }
    }
}
