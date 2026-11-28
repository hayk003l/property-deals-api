using Microsoft.EntityFrameworkCore;
using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.ValueObjects;

namespace PropertyDeals.Infrastructure.Persistence.Seed;

public static class DevDataSeeder
{
    // TODO Phase 4: replace with a real hash from the password hasher so seeded users can log in.
    private const string PlaceholderPasswordHash = "seed-placeholder-hash";
    private const int RandomSeed = 20261128;
    private const int PropertyCount = 45;

    private static readonly (DealStatusEnum Status, int Count)[] DealPlan =
    [
        (DealStatusEnum.Draft, 6),
        (DealStatusEnum.Active, 8),
        (DealStatusEnum.UnderContract, 7),
        (DealStatusEnum.Closing, 5),
        (DealStatusEnum.Closed, 7),
        (DealStatusEnum.Cancelled, 6)
    ];

    private const int RelistedAfterCancellationCount = 2;

    private static readonly string[] AgentNames =
    [
        "Olivia Carter", "Liam Brooks", "Sophia Reed", "Noah Bennett",
        "Emma Hayes", "Lucas Foster", "Mia Sullivan", "Ethan Price"
    ];

    private static readonly string[] ClientNames =
    [
        "James Walker", "Ava Mitchell", "Benjamin Turner", "Charlotte Parker", "Henry Collins",
        "Amelia Stewart", "Alexander Morris", "Harper Rogers", "Daniel Cook", "Evelyn Morgan",
        "Matthew Bell", "Abigail Murphy", "Samuel Bailey", "Emily Rivera", "David Cooper",
        "Elizabeth Richardson", "Joseph Cox", "Sofia Howard", "Jackson Ward", "Avery Torres",
        "Sebastian Peterson", "Ella Gray", "Jack Ramirez", "Scarlett James", "Owen Watson",
        "Grace Brooks", "Wyatt Kelly", "Chloe Sanders", "Julian Myers", "Lily Long"
    ];

    private static readonly string[] StreetNames =
    [
        "Maple Avenue", "Oak Street", "Cedar Lane", "Park Road", "Hillcrest Drive", "Lakeview Boulevard",
        "Elm Street", "Riverside Drive", "Sunset Avenue", "Willow Way", "Highland Road", "Chestnut Street"
    ];

    private static readonly Location[] Locations =
    [
        new("Austin", "TX", "78701", "US", "USD", 1.00m),
        new("Denver", "CO", "80202", "US", "USD", 1.05m),
        new("Seattle", "WA", "98101", "US", "USD", 1.40m),
        new("Miami", "FL", "33131", "US", "USD", 1.20m),
        new("Chicago", "IL", "60601", "US", "USD", 0.95m),
        new("Boston", "MA", "02108", "US", "USD", 1.45m),
        new("San Diego", "CA", "92101", "US", "USD", 1.60m),
        new("Nashville", "TN", "37203", "US", "USD", 0.90m),
        new("London", null, "SW1A 2AA", "GB", "GBP", 1.50m),
        new("Manchester", null, "M1 1AE", "GB", "GBP", 0.70m),
        new("Berlin", null, "10115", "DE", "EUR", 1.00m),
        new("Munich", null, "80331", "DE", "EUR", 1.30m)
    ];

    private static readonly string[] PhotoFileNames = ["Front Exterior.jpg", "Kitchen.jpg", "Living Room.png", "Backyard.jpeg"];

    public static async Task SeedAsync(AppDbContext dbContext, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var random = new Random(RandomSeed);
        var history = new List<HistoryEntry>();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var users = CreateUsers(utcNow);
        dbContext.Users.AddRange(users.All);
        await dbContext.SaveChangesAsync(cancellationToken);

        var properties = CreateProperties(users.Agents, random, utcNow);
        dbContext.Properties.AddRange(properties);
        await dbContext.SaveChangesAsync(cancellationToken);

        var timelines = CreateDeals(properties, users, random, utcNow, history);
        dbContext.Deals.AddRange(timelines.Select(t => t.Deal));
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Documents.AddRange(CreateDocuments(timelines, random, utcNow, history));
        dbContext.ActivityLogs.AddRange(history
            .OrderBy(h => h.OccurredOnUtc)
            .Select(h => ActivityLog.Create(h.Deal.Id, h.Action, h.UserId, h.Details, h.OccurredOnUtc)));
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static SeedUsers CreateUsers(DateTime utcNow)
    {
        var admin = User.Create(Email.Create("admin@example.com"), PlaceholderPasswordHash, RolesEnum.Admin, utcNow);
        var agents = AgentNames.Select(name => CreateUser(name, RolesEnum.Agent, utcNow)).ToList();
        var clients = ClientNames.Select(name => CreateUser(name, RolesEnum.Client, utcNow)).ToList();

        return new SeedUsers(admin, agents, clients);
    }

    private static User CreateUser(string fullName, RolesEnum role, DateTime utcNow)
    {
        var email = Email.Create($"{fullName.Replace(' ', '.')}@example.com");
        return User.Create(email, PlaceholderPasswordHash, role, utcNow);
    }

    private static List<Property> CreateProperties(IReadOnlyList<User> agents, Random random, DateTime utcNow)
    {
        var propertyTypes = Enum.GetValues<PropertyTypeEnum>();
        var properties = new List<Property>(PropertyCount);

        for (var i = 0; i < PropertyCount; i++)
        {
            var location = Locations[i % Locations.Length];
            var type = propertyTypes[random.Next(propertyTypes.Length)];
            var (bedrooms, basePrice) = DescribeType(type, random);

            var address = Address.Create(
                $"{random.Next(1, 2400)} {StreetNames[random.Next(StreetNames.Length)]}",
                location.City,
                location.State,
                location.ZipCode,
                location.Country);

            var amount = basePrice * location.PriceFactor * (0.85m + (decimal)random.NextDouble() * 0.4m) + bedrooms * 15_000m;
            var price = Money.Create(decimal.Round(amount / 1_000m) * 1_000m, location.Currency);

            var agent = agents[random.Next(agents.Count)];
            properties.Add(Property.Create(address, type, price, bedrooms, agent.Id, utcNow));
        }

        return properties;
    }

    private static (int Bedrooms, decimal BasePrice) DescribeType(PropertyTypeEnum type, Random random) => type switch
    {
        PropertyTypeEnum.Apartment => (random.Next(1, 4), 250_000m),
        PropertyTypeEnum.Condo => (random.Next(1, 4), 320_000m),
        PropertyTypeEnum.Townhouse => (random.Next(2, 5), 420_000m),
        PropertyTypeEnum.House => (random.Next(3, 6), 550_000m),
        PropertyTypeEnum.MultiFamily => (random.Next(4, 9), 850_000m),
        PropertyTypeEnum.Commercial => (0, 1_200_000m),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static List<DealTimeline> CreateDeals(
        IReadOnlyList<Property> properties,
        SeedUsers users,
        Random random,
        DateTime utcNow,
        List<HistoryEntry> history)
    {
        var agentsById = users.Agents.ToDictionary(a => a.Id);
        var targets = DealPlan.SelectMany(p => Enumerable.Repeat(p.Status, p.Count)).ToList();
        var timelines = new List<DealTimeline>();

        for (var i = 0; i < targets.Count; i++)
        {
            var property = properties[i];
            timelines.Add(CreateDeal(property, agentsById[property.ListedById], targets[i], users, random, utcNow, history));
        }

        var relistable = timelines
            .Where(t => t.StatusChangedOnUtc.TryGetValue(DealStatusEnum.Cancelled, out var cancelledAt) && cancelledAt <= utcNow.AddDays(-12))
            .Take(RelistedAfterCancellationCount)
            .ToList();

        foreach (var previous in relistable)
        {
            var relistedOn = previous.StatusChangedOnUtc[DealStatusEnum.Cancelled].AddDays(2);
            timelines.Add(CreateDeal(previous.Property, previous.ListingAgent, DealStatusEnum.Active, users, random, utcNow, history, relistedOn));
        }

        return timelines;
    }

    private static DealTimeline CreateDeal(
        Property property,
        User listingAgent,
        DealStatusEnum target,
        SeedUsers users,
        Random random,
        DateTime utcNow,
        List<HistoryEntry> history,
        DateTime? createdOnUtc = null)
    {
        switch (target)
        {
            case DealStatusEnum.Draft:
            {
                var created = utcNow.AddDays(-random.Next(0, 6)).AddHours(-random.Next(1, 20));
                var timeline = DealTimeline.Start(property, listingAgent, created, history);
                if (random.Next(100) < 70)
                {
                    AddSellers(timeline, users, random, created.AddHours(1));
                }

                return timeline;
            }

            case DealStatusEnum.Active:
            {
                var created = createdOnUtc ?? utcNow.AddDays(-random.Next(10, 45));
                var activated = created.AddDays(2);
                var timeline = DealTimeline.Start(property, listingAgent, created, history);
                AddSellers(timeline, users, random, created.AddHours(1));
                timeline.MoveTo(DealStatusEnum.Active, activated);

                if (random.Next(100) < 50)
                {
                    AddBuyerSide(timeline, users, random, activated.AddDays(random.Next(2, 8)));
                }

                return timeline;
            }

            case DealStatusEnum.UnderContract:
            {
                var underContract = utcNow.AddDays(-random.Next(1, 21)).AddHours(-random.Next(1, 8));
                var activated = underContract.AddDays(-random.Next(7, 26));
                var created = activated.AddDays(-2);
                var closingDate = DateOnly.FromDateTime(underContract).AddDays(random.Next(30, 46));

                var timeline = DealTimeline.Start(property, listingAgent, created, history);
                AddSellers(timeline, users, random, created.AddHours(1));
                timeline.MoveTo(DealStatusEnum.Active, activated);
                AddBuyerSide(timeline, users, random, underContract.AddDays(-2));
                timeline.MoveTo(DealStatusEnum.UnderContract, underContract, closingDate);

                var rescheduledAt = underContract.AddDays(3);
                if (rescheduledAt <= utcNow && random.Next(100) < 40)
                {
                    timeline.RescheduleClosing(closingDate.AddDays(random.Next(5, 15)), rescheduledAt);
                }

                return timeline;
            }

            case DealStatusEnum.Closing:
            {
                var closingDate = DateOnly.FromDateTime(utcNow).AddDays(random.Next(3, 11));
                var underContract = closingDate.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc).AddDays(-35);
                var closingStarted = utcNow.AddDays(-random.Next(1, 4));
                var activated = underContract.AddDays(-random.Next(7, 21));
                var created = activated.AddDays(-2);

                var timeline = DealTimeline.Start(property, listingAgent, created, history);
                AddSellers(timeline, users, random, created.AddHours(1));
                timeline.MoveTo(DealStatusEnum.Active, activated);
                AddBuyerSide(timeline, users, random, underContract.AddDays(-2));
                timeline.MoveTo(DealStatusEnum.UnderContract, underContract, closingDate);
                timeline.MoveTo(DealStatusEnum.Closing, closingStarted);

                return timeline;
            }

            case DealStatusEnum.Closed:
            {
                var created = utcNow.AddDays(-random.Next(100, 260));
                var activated = created.AddDays(2);
                var buyersJoined = activated.AddDays(random.Next(5, 21));
                var underContract = buyersJoined.AddDays(2);
                var closingDate = DateOnly.FromDateTime(underContract).AddDays(random.Next(30, 46));
                var closed = closingDate.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);

                var timeline = DealTimeline.Start(property, listingAgent, created, history);
                AddSellers(timeline, users, random, created.AddHours(1));
                timeline.MoveTo(DealStatusEnum.Active, activated);
                AddBuyerSide(timeline, users, random, buyersJoined);
                timeline.MoveTo(DealStatusEnum.UnderContract, underContract, closingDate);
                timeline.MoveTo(DealStatusEnum.Closing, closed.AddDays(-7));
                timeline.MoveTo(DealStatusEnum.Closed, closed);

                return timeline;
            }

            case DealStatusEnum.Cancelled:
            {
                var created = utcNow.AddDays(-random.Next(30, 150));
                var activated = created.AddDays(2);
                var timeline = DealTimeline.Start(property, listingAgent, created, history);
                AddSellers(timeline, users, random, created.AddHours(1));
                timeline.MoveTo(DealStatusEnum.Active, activated);

                if (random.Next(100) < 50)
                {
                    timeline.MoveTo(DealStatusEnum.Cancelled, activated.AddDays(random.Next(5, 21)));
                    return timeline;
                }

                var underContract = activated.AddDays(random.Next(5, 16));
                AddBuyerSide(timeline, users, random, underContract.AddDays(-1));
                timeline.MoveTo(DealStatusEnum.UnderContract, underContract, DateOnly.FromDateTime(underContract).AddDays(random.Next(30, 46)));
                timeline.MoveTo(DealStatusEnum.Cancelled, underContract.AddDays(random.Next(3, 13)));

                return timeline;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, null);
        }
    }

    private static void AddSellers(DealTimeline timeline, SeedUsers users, Random random, DateTime at)
    {
        var count = random.Next(100) < 25 ? 2 : 1;
        foreach (var seller in PickClients(timeline, users, random, count))
        {
            timeline.AddParticipant(seller, ParticipantRoleEnum.Seller, at);
        }
    }

    private static void AddBuyerSide(DealTimeline timeline, SeedUsers users, Random random, DateTime at)
    {
        var buyerAgents = users.Agents.Where(a => !timeline.Deal.IsParticipant(a.Id)).ToList();
        timeline.AddParticipant(buyerAgents[random.Next(buyerAgents.Count)], ParticipantRoleEnum.BuyerAgent, at);

        var count = random.Next(100) < 30 ? 2 : 1;
        foreach (var buyer in PickClients(timeline, users, random, count))
        {
            timeline.AddParticipant(buyer, ParticipantRoleEnum.Buyer, at.AddMinutes(30));
        }
    }

    private static IEnumerable<User> PickClients(DealTimeline timeline, SeedUsers users, Random random, int count) =>
        users.Clients
            .Where(c => !timeline.Deal.IsParticipant(c.Id))
            .OrderBy(_ => random.Next())
            .Take(count)
            .ToList();

    private static List<Document> CreateDocuments(
        IEnumerable<DealTimeline> timelines,
        Random random,
        DateTime utcNow,
        List<HistoryEntry> history)
    {
        var documents = new List<Document>();

        foreach (var timeline in timelines)
        {
            var listingAgentId = timeline.ListingAgent.Id;
            var uploadsUntil = timeline.StatusChangedOnUtc.GetValueOrDefault(DealStatusEnum.Cancelled, DateTime.MaxValue);

            if (timeline.StatusChangedOnUtc.TryGetValue(DealStatusEnum.Active, out var activated))
            {
                Upload("Listing Agreement.pdf", listingAgentId, activated);

                var photoCount = random.Next(2, PhotoFileNames.Length + 1);
                foreach (var photo in PhotoFileNames.Take(photoCount))
                {
                    Upload(photo, listingAgentId, activated.AddHours(2));
                }

                if (random.Next(100) < 20)
                {
                    var outdated = Upload("Old Floor Plan.jpg", listingAgentId, activated.AddHours(3));
                    Delete(outdated, listingAgentId, activated.AddDays(1));
                }
            }
            else if (random.Next(100) < 50)
            {
                documents.Add(Document.Create(timeline.Deal.Id, "Seller Disclosure.docx", listingAgentId, utcNow));
            }

            if (timeline.StatusChangedOnUtc.TryGetValue(DealStatusEnum.UnderContract, out var underContract)
                && timeline.BuyerAgent is not null)
            {
                var buyerAgentId = timeline.BuyerAgent.Id;
                Upload("Purchase Agreement.pdf", buyerAgentId, underContract);

                if (underContract.AddDays(7) <= uploadsUntil)
                {
                    Upload("Inspection Report.pdf", buyerAgentId, underContract.AddDays(7));
                }

                if (underContract.AddDays(12) <= uploadsUntil)
                {
                    Upload("Appraisal Report.pdf", listingAgentId, underContract.AddDays(12));
                }
            }

            if (timeline.StatusChangedOnUtc.TryGetValue(DealStatusEnum.Closing, out var closing))
            {
                Upload("Closing Disclosure.pdf", listingAgentId, closing);
            }

            if (timeline.StatusChangedOnUtc.TryGetValue(DealStatusEnum.Closed, out var closed))
            {
                Upload("Deed Transfer.pdf", listingAgentId, closed);
            }

            Document Upload(string fileName, int uploadedBy, DateTime at)
            {
                var document = Document.Create(timeline.Deal.Id, fileName, uploadedBy, at);
                documents.Add(document);

                if (at <= utcNow)
                {
                    document.MarkUploaded(at);
                    history.Add(new HistoryEntry(timeline.Deal, ActivityActionEnum.DocumentUploaded, uploadedBy, $"Uploaded \"{fileName}\".", at));
                }

                return document;
            }

            void Delete(Document document, int deletedBy, DateTime at)
            {
                document.Delete(deletedBy, at);
                history.Add(new HistoryEntry(timeline.Deal, ActivityActionEnum.DocumentDeleted, deletedBy, $"Deleted \"{document.FileName}\".", at));
            }
        }

        return documents;
    }

    private sealed record Location(string City, string? State, string ZipCode, string Country, string Currency, decimal PriceFactor);

    private sealed record HistoryEntry(Deal Deal, ActivityActionEnum Action, int UserId, string Details, DateTime OccurredOnUtc);

    private sealed record SeedUsers(User Admin, IReadOnlyList<User> Agents, IReadOnlyList<User> Clients)
    {
        public IEnumerable<User> All => [Admin, .. Agents, .. Clients];
    }

    private sealed class DealTimeline
    {
        private readonly List<HistoryEntry> _history;

        private DealTimeline(Deal deal, Property property, User listingAgent, List<HistoryEntry> history)
        {
            Deal = deal;
            Property = property;
            ListingAgent = listingAgent;
            _history = history;
        }

        public Deal Deal { get; }
        public Property Property { get; }
        public User ListingAgent { get; }
        public User? BuyerAgent { get; private set; }
        public Dictionary<DealStatusEnum, DateTime> StatusChangedOnUtc { get; } = [];

        public static DealTimeline Start(Property property, User listingAgent, DateTime createdOnUtc, List<HistoryEntry> history)
        {
            var deal = Deal.Create(property.Id, listingAgent.Id, listingAgent.Role, createdOnUtc);
            var timeline = new DealTimeline(deal, property, listingAgent, history);

            timeline.Log(ActivityActionEnum.DealCreated, $"Deal created for {property.Address}.", createdOnUtc);
            return timeline;
        }

        public void AddParticipant(User user, ParticipantRoleEnum role, DateTime at)
        {
            Deal.AddParticipant(user.Id, user.Role, role, ListingAgent.Id, at);

            if (role == ParticipantRoleEnum.BuyerAgent)
            {
                BuyerAgent = user;
            }

            Log(ActivityActionEnum.ParticipantAdded, $"{user.Email} added as {role}.", at);
        }

        public void MoveTo(DealStatusEnum status, DateTime at, DateOnly? closingDate = null)
        {
            var oldStatus = Deal.Status;
            Deal.ChangeStatus(status, ListingAgent.Id, at, closingDate);
            StatusChangedOnUtc[status] = at;

            var details = closingDate is null
                ? $"{oldStatus} -> {status}."
                : $"{oldStatus} -> {status}, closing on {closingDate.Value:yyyy-MM-dd}.";
            Log(ActivityActionEnum.DealStatusChanged, details, at);
        }

        public void RescheduleClosing(DateOnly newClosingDate, DateTime at)
        {
            var oldClosingDate = Deal.ClosingDate!.Value;
            Deal.RescheduleClosing(newClosingDate, ListingAgent.Id, at);

            Log(ActivityActionEnum.ClosingDateRescheduled, $"Closing moved from {oldClosingDate:yyyy-MM-dd} to {newClosingDate:yyyy-MM-dd}.", at);
        }

        private void Log(ActivityActionEnum action, string details, DateTime at) =>
            _history.Add(new HistoryEntry(Deal, action, ListingAgent.Id, details, at));
    }
}
