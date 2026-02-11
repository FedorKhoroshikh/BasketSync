using Application.Commands;
using Application.Handlers;
using Application.Interfaces;
using Application.Queries;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BasketSyncTests;

[TestFixture]
public class DiscountCardHandlersTests : TestBase
{
    private sealed class FakeFileStorage : IFileStorageService
    {
        public List<string> Deleted { get; } = [];
        public Task<string> SaveAsync(Stream stream, string fileName, CancellationToken ct)
            => Task.FromResult($"uploads/cards/{Guid.NewGuid()}_{fileName}");
        public void Delete(string relativePath) => Deleted.Add(relativePath);
    }

    [Test]
    public async Task CreateCard_Success_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        var handler = new CreateDiscountCardHandler(_uow, Mapper);
        var dto = await handler.Handle(new CreateDiscountCardCommand(user.Id, "Пятёрочка", "5% скидка"), _ct);

        Assert.That(dto.Name, Is.EqualTo("Пятёрочка"));
        Assert.That(dto.Comment, Is.EqualTo("5% скидка"));
        Assert.That(dto.IsActive, Is.True);
        Assert.That(await db.DiscountCards.CountAsync(_ct), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateCard_ReturnsDto_WithCorrectFields()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        var handler = new CreateDiscountCardHandler(_uow, Mapper);
        var dto = await handler.Handle(new CreateDiscountCardCommand(user.Id, "Магнит", null), _ct);

        Assert.That(dto.Id, Is.GreaterThan(0));
        Assert.That(dto.UserId, Is.EqualTo(user.Id));
        Assert.That(dto.Comment, Is.Null);
    }

    [Test]
    public async Task GetUserCards_ReturnsOnlyUserCards()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user1 = Seed.TestUser("Alice");
        var user2 = Seed.TestUser("Bob");
        db.AddRange(user1, user2);
        await db.SaveChangesAsync(_ct);

        db.AddRange(
            new DiscountCard(user1, "Card1", "comment1"),
            new DiscountCard(user1, "Card2", "comment2"),
            new DiscountCard(user2, "Card3"));
        await db.SaveChangesAsync(_ct);

        var handler = new GetUserCardsHandler(_uow, Mapper);
        var result = await handler.Handle(new GetUserCardsQuery(user1.Id), _ct);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(c => c.UserId == user1.Id), Is.True);
    }

    [Test]
    public async Task DeleteCard_CascadesIdentifiers()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);
        var fakeFs = new FakeFileStorage();

        var user = Seed.TestUser();
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        var card = new DiscountCard(user, "Лента", "7% скидка");
        card.AddIdentifier(IdentifierType.Manual, "1234567890");
        db.Add(card);
        await db.SaveChangesAsync(_ct);

        Assert.That(await db.CardIdentifiers.CountAsync(_ct), Is.EqualTo(1));

        var handler = new DeleteDiscountCardHandler(_uow, fakeFs);
        await handler.Handle(new DeleteDiscountCardCommand(card.Id), _ct);

        Assert.That(await db.DiscountCards.CountAsync(_ct), Is.EqualTo(0));
        Assert.That(await db.CardIdentifiers.CountAsync(_ct), Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteCard_DeletesImageFiles()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);
        var fakeFs = new FakeFileStorage();

        var user = Seed.TestUser();
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        var card = new DiscountCard(user, "TestCard");
        card.AddIdentifier(IdentifierType.QrCode, "qr-value", "uploads/cards/img1.png");
        card.AddIdentifier(IdentifierType.Manual, "manual-value");
        db.Add(card);
        await db.SaveChangesAsync(_ct);

        var handler = new DeleteDiscountCardHandler(_uow, fakeFs);
        await handler.Handle(new DeleteDiscountCardCommand(card.Id), _ct);

        Assert.That(fakeFs.Deleted, Has.Count.EqualTo(1));
        Assert.That(fakeFs.Deleted[0], Is.EqualTo("uploads/cards/img1.png"));
    }

    [Test]
    public async Task AddIdentifier_Success_Test()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        db.Add(user);
        var card = new DiscountCard(user, "Ашан", "3% скидка");
        db.Add(card);
        await db.SaveChangesAsync(_ct);

        var handler = new AddCardIdentifierHandler(_uow, Mapper);
        var dto = await handler.Handle(
            new AddCardIdentifierCommand(card.Id, (int)IdentifierType.Manual, "9999888877"), _ct);

        Assert.That(dto.Value, Is.EqualTo("9999888877"));
        Assert.That(dto.Type, Is.EqualTo((int)IdentifierType.Manual));
        Assert.That(dto.ImagePath, Is.Null);
        Assert.That(await db.CardIdentifiers.CountAsync(_ct), Is.EqualTo(1));
    }

    [Test]
    public async Task AddIdentifier_WithImagePath_Success()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        db.Add(user);
        var card = new DiscountCard(user, "TestCard");
        db.Add(card);
        await db.SaveChangesAsync(_ct);

        var handler = new AddCardIdentifierHandler(_uow, Mapper);
        var dto = await handler.Handle(
            new AddCardIdentifierCommand(card.Id, (int)IdentifierType.QrCode, "qr-data", "uploads/cards/test.png"), _ct);

        Assert.That(dto.ImagePath, Is.EqualTo("uploads/cards/test.png"));
        Assert.That(dto.Type, Is.EqualTo((int)IdentifierType.QrCode));
    }

    [Test]
    public async Task RemoveIdentifier_DeletesImageFile()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);
        var fakeFs = new FakeFileStorage();

        var user = Seed.TestUser();
        db.Add(user);
        var card = new DiscountCard(user, "TestCard");
        card.AddIdentifier(IdentifierType.Image, "photo", "uploads/cards/photo.jpg");
        db.Add(card);
        await db.SaveChangesAsync(_ct);

        var identifierId = (await db.CardIdentifiers.FirstAsync(_ct)).Id;

        var handler = new RemoveCardIdentifierHandler(_uow, fakeFs);
        await handler.Handle(new RemoveCardIdentifierCommand(identifierId), _ct);

        Assert.That(fakeFs.Deleted, Has.Count.EqualTo(1));
        Assert.That(fakeFs.Deleted[0], Is.EqualTo("uploads/cards/photo.jpg"));
        Assert.That(await db.CardIdentifiers.CountAsync(_ct), Is.EqualTo(0));
    }

    [Test]
    public async Task ResolveCard_ByManualNumber_Success()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        db.Add(user);
        var card = new DiscountCard(user, "Перекрёсток", "12% скидка");
        card.AddIdentifier(IdentifierType.Manual, "111222333");
        db.Add(card);
        await db.SaveChangesAsync(_ct);

        var handler = new ResolveCardHandler(_uow, Mapper);
        var dto = await handler.Handle(new ResolveCardCommand("111222333"), _ct);

        Assert.That(dto.Name, Is.EqualTo("Перекрёсток"));
        Assert.That(dto.Comment, Is.EqualTo("12% скидка"));
    }

    [Test]
    public async Task ResolveCard_InactiveCard_ThrowsKeyNotFound()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        db.Add(user);
        var card = new DiscountCard(user, "Old", "old card");
        card.Deactivate();
        card.AddIdentifier(IdentifierType.Manual, "000111");
        db.Add(card);
        await db.SaveChangesAsync(_ct);

        var handler = new ResolveCardHandler(_uow, Mapper);
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new ResolveCardCommand("000111"), _ct));
    }

    [Test]
    public void ResolveCard_UnknownValue_ThrowsKeyNotFound()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var handler = new ResolveCardHandler(_uow, Mapper);
        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new ResolveCardCommand("nonexistent"), _ct));
    }

    [Test]
    public async Task ToggleCard_FlipsIsActive()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);

        var user = Seed.TestUser();
        db.Add(user);
        var card = new DiscountCard(user, "Тест", "1% скидка");
        db.Add(card);
        await db.SaveChangesAsync(_ct);

        Assert.That(card.IsActive, Is.True);

        var handler = new ToggleDiscountCardHandler(_uow);
        await handler.Handle(new ToggleDiscountCardCommand(card.Id), _ct);

        var updated = await db.DiscountCards.FindAsync(card.Id);
        Assert.That(updated!.IsActive, Is.False);

        await handler.Handle(new ToggleDiscountCardCommand(card.Id), _ct);
        await db.Entry(updated).ReloadAsync(_ct);
        Assert.That(updated.IsActive, Is.True);
    }
}
