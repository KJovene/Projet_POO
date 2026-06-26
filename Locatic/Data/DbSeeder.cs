using Locatic.Models;

namespace Locatic.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            SeedBrandsAndModels(context);
            SeedCars(context);
            SeedClients(context);
            SeedReservations(context);
        }

        private static void SeedBrandsAndModels(AppDbContext context)
        {
            if (context.CarBrands.Any()) return;

            var brands = new List<CarBrand>
            {
                new() { Name = "Toyota",        Country = "Japon",      CarModels = new() { new() { Name = "Corolla" },   new() { Name = "Yaris" },    new() { Name = "RAV4" } } },
                new() { Name = "Volkswagen",    Country = "Allemagne",  CarModels = new() { new() { Name = "Golf" },      new() { Name = "Polo" },     new() { Name = "Passat" } } },
                new() { Name = "BMW",           Country = "Allemagne",  CarModels = new() { new() { Name = "Série 3" },   new() { Name = "Série 5" },  new() { Name = "X5" } } },
                new() { Name = "Mercedes-Benz", Country = "Allemagne",  CarModels = new() { new() { Name = "Classe C" },  new() { Name = "Classe E" }, new() { Name = "GLC" } } },
                new() { Name = "Peugeot",       Country = "France",     CarModels = new() { new() { Name = "208" },       new() { Name = "308" },      new() { Name = "3008" } } },
                new() { Name = "Renault",       Country = "France",     CarModels = new() { new() { Name = "Clio" },      new() { Name = "Mégane" },   new() { Name = "Kadjar" } } },
                new() { Name = "Ford",          Country = "États-Unis", CarModels = new() { new() { Name = "Focus" },     new() { Name = "Fiesta" },   new() { Name = "Kuga" } } },
                new() { Name = "Honda",         Country = "Japon",      CarModels = new() { new() { Name = "Civic" },     new() { Name = "CR-V" },     new() { Name = "Jazz" } } },
                new() { Name = "Audi",          Country = "Allemagne",  CarModels = new() { new() { Name = "A3" },        new() { Name = "A4" },       new() { Name = "Q5" } } },
                new() { Name = "Citroën",       Country = "France",     CarModels = new() { new() { Name = "C3" },        new() { Name = "C4" },       new() { Name = "C5 Aircross" } } },
            };

            context.CarBrands.AddRange(brands);
            context.SaveChanges();
        }

        private static void SeedCars(AppDbContext context)
        {
            if (context.Cars.Any()) return;

            var models = context.CarModels.ToDictionary(m => m.Name);

            var cars = new List<Car>
            {
                new() { LicensePlate = "FY-945-NT", CarModelId = models["Corolla"].Id,      Year = 2016, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 100 },
                new() { LicensePlate = "AB-123-CD", CarModelId = models["Yaris"].Id,        Year = 2019, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 65  },
                new() { LicensePlate = "BC-234-DE", CarModelId = models["RAV4"].Id,         Year = 2021, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 120 },
                new() { LicensePlate = "CD-345-EF", CarModelId = models["Golf"].Id,         Year = 2018, FuelType = Fuel.Diesel,    NumberOfSeats = 5, DailyPrice = 80  },
                new() { LicensePlate = "DE-456-FG", CarModelId = models["Polo"].Id,         Year = 2020, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 60  },
                new() { LicensePlate = "EF-567-GH", CarModelId = models["Passat"].Id,       Year = 2017, FuelType = Fuel.Diesel,    NumberOfSeats = 5, DailyPrice = 90  },
                new() { LicensePlate = "FG-678-HI", CarModelId = models["Série 3"].Id,      Year = 2019, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 130 },
                new() { LicensePlate = "GH-789-IJ", CarModelId = models["Série 5"].Id,      Year = 2020, FuelType = Fuel.Diesel,    NumberOfSeats = 5, DailyPrice = 160 },
                new() { LicensePlate = "HI-890-JK", CarModelId = models["X5"].Id,           Year = 2022, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 200 },
                new() { LicensePlate = "IJ-901-KL", CarModelId = models["Classe C"].Id,     Year = 2018, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 140 },
                new() { LicensePlate = "JK-012-LM", CarModelId = models["Classe E"].Id,     Year = 2021, FuelType = Fuel.Diesel,    NumberOfSeats = 5, DailyPrice = 170 },
                new() { LicensePlate = "KL-123-MN", CarModelId = models["GLC"].Id,          Year = 2020, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 190 },
                new() { LicensePlate = "LM-234-NO", CarModelId = models["208"].Id,          Year = 2021, FuelType = Fuel.Electric,  NumberOfSeats = 5, DailyPrice = 70  },
                new() { LicensePlate = "MN-345-OP", CarModelId = models["308"].Id,          Year = 2019, FuelType = Fuel.Diesel,    NumberOfSeats = 5, DailyPrice = 80  },
                new() { LicensePlate = "NO-456-PQ", CarModelId = models["3008"].Id,         Year = 2022, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 110 },
                new() { LicensePlate = "OP-567-QR", CarModelId = models["Clio"].Id,         Year = 2020, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 55  },
                new() { LicensePlate = "PQ-678-RS", CarModelId = models["Mégane"].Id,       Year = 2018, FuelType = Fuel.Diesel,    NumberOfSeats = 5, DailyPrice = 75  },
                new() { LicensePlate = "QR-789-ST", CarModelId = models["Kadjar"].Id,       Year = 2021, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 95  },
                new() { LicensePlate = "RS-890-TU", CarModelId = models["Focus"].Id,        Year = 2017, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 70  },
                new() { LicensePlate = "ST-901-UV", CarModelId = models["Fiesta"].Id,       Year = 2019, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 55  },
                new() { LicensePlate = "TU-012-VW", CarModelId = models["Kuga"].Id,         Year = 2022, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 105 },
                new() { LicensePlate = "UV-123-WX", CarModelId = models["Civic"].Id,        Year = 2020, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 80  },
                new() { LicensePlate = "VW-234-XY", CarModelId = models["CR-V"].Id,         Year = 2021, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 115 },
                new() { LicensePlate = "WX-345-YZ", CarModelId = models["Jazz"].Id,         Year = 2018, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 65  },
                new() { LicensePlate = "XY-456-ZA", CarModelId = models["A3"].Id,           Year = 2019, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 110 },
                new() { LicensePlate = "YZ-567-AB", CarModelId = models["A4"].Id,           Year = 2020, FuelType = Fuel.Diesel,    NumberOfSeats = 5, DailyPrice = 130 },
                new() { LicensePlate = "ZA-678-BC", CarModelId = models["Q5"].Id,           Year = 2022, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 165 },
                new() { LicensePlate = "AB-789-CD", CarModelId = models["C3"].Id,           Year = 2021, FuelType = Fuel.Petrol,    NumberOfSeats = 5, DailyPrice = 55  },
                new() { LicensePlate = "BC-890-DE", CarModelId = models["C4"].Id,           Year = 2020, FuelType = Fuel.Electric,  NumberOfSeats = 5, DailyPrice = 75  },
                new() { LicensePlate = "CD-901-EF", CarModelId = models["C5 Aircross"].Id,  Year = 2022, FuelType = Fuel.Hybrid,    NumberOfSeats = 5, DailyPrice = 105 },
            };

            context.Cars.AddRange(cars);
            context.SaveChanges();
        }

        private static void SeedClients(AppDbContext context)
        {
            if (context.Clients.Any()) return;

            var clients = new List<Client>
            {
                new() { FirstName = "Lucas",  LastName = "Martin",   Email = "lucas.martin@locatic.fr",   PhoneNumber = "06 12 34 56 78" },
                new() { FirstName = "Emma",   LastName = "Bernard",  Email = "emma.bernard@locatic.fr",  PhoneNumber = "06 23 45 67 89" },
                new() { FirstName = "Hugo",   LastName = "Dubois",   Email = "hugo.dubois@locatic.fr",   PhoneNumber = "06 34 56 78 90" },
                new() { FirstName = "Chloe",  LastName = "Thomas",   Email = "chloe.thomas@locatic.fr",  PhoneNumber = "06 45 67 89 01" },
                new() { FirstName = "Nathan", LastName = "Robert",   Email = "nathan.robert@locatic.fr", PhoneNumber = "06 56 78 90 12" },
                new() { FirstName = "Lea",    LastName = "Petit",    Email = "lea.petit@locatic.fr",     PhoneNumber = "06 67 89 01 23" },
                new() { FirstName = "Jules",  LastName = "Richard",  Email = "jules.richard@locatic.fr", PhoneNumber = "06 78 90 12 34" },
                new() { FirstName = "Ines",   LastName = "Durand",   Email = "ines.durand@locatic.fr",   PhoneNumber = "06 89 01 23 45" },
            };

            context.Clients.AddRange(clients);
            context.SaveChanges();
        }

        private static void SeedReservations(AppDbContext context)
        {
            if (context.Reservations.Any()) return;

            var clients = context.Clients.ToDictionary(client => client.Email);
            var cars = context.Cars.ToDictionary(car => car.LicensePlate);

            var reservations = new List<Reservation>
            {
                new() { ClientId = clients["lucas.martin@locatic.fr"].Id,   CarId = cars["FY-945-NT"].Id, StartDate = new DateTime(2026, 7, 1),  EndDate = new DateTime(2026, 7, 5) },
                new() { ClientId = clients["emma.bernard@locatic.fr"].Id,  CarId = cars["AB-123-CD"].Id, StartDate = new DateTime(2026, 7, 3),  EndDate = new DateTime(2026, 7, 6) },
                new() { ClientId = clients["hugo.dubois@locatic.fr"].Id,   CarId = cars["CD-345-EF"].Id, StartDate = new DateTime(2026, 7, 7),  EndDate = new DateTime(2026, 7, 10) },
                new() { ClientId = clients["chloe.thomas@locatic.fr"].Id,  CarId = cars["FG-678-HI"].Id, StartDate = new DateTime(2026, 7, 8),  EndDate = new DateTime(2026, 7, 12) },
                new() { ClientId = clients["nathan.robert@locatic.fr"].Id, CarId = cars["LM-234-NO"].Id, StartDate = new DateTime(2026, 7, 10), EndDate = new DateTime(2026, 7, 13) },
                new() { ClientId = clients["lea.petit@locatic.fr"].Id,     CarId = cars["OP-567-QR"].Id, StartDate = new DateTime(2026, 7, 14), EndDate = new DateTime(2026, 7, 18) },
                new() { ClientId = clients["jules.richard@locatic.fr"].Id, CarId = cars["UV-123-WX"].Id, StartDate = new DateTime(2026, 7, 15), EndDate = new DateTime(2026, 7, 20) },
                new() { ClientId = clients["ines.durand@locatic.fr"].Id,   CarId = cars["BC-890-DE"].Id, StartDate = new DateTime(2026, 7, 21), EndDate = new DateTime(2026, 7, 24) },
            };

            context.Reservations.AddRange(reservations);
            context.SaveChanges();
        }
    }
}
