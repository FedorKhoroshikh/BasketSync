using Application.Commands;
using Application.Handlers;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BasketSyncTests;

[TestFixture]
public class UserHandlersTests : TestBase
{
    private static IPasswordHasher TestHasher() => new PasswordHasher();

    [Test]
    public async Task GetProfile_ReturnsUserData()
    {
        var db = NewDb();
        var hasher = TestHasher();
        var user = new User("Alice", hasher.Hash("pass"));
        user.SetEmail("alice@mail.com");
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new GetUserProfileHandler(_uow);
        var dto = await handler.Handle(new GetUserProfileQuery(user.Id), _ct);

        Assert.That(dto.Name, Is.EqualTo("Alice"));
        Assert.That(dto.Email, Is.EqualTo("alice@mail.com"));
        Assert.That(dto.HasPassword, Is.True);
        Assert.That(dto.HasGoogle, Is.False);
    }

    [Test]
    public async Task GetProfile_GoogleUser_HasGoogleTrue()
    {
        var db = NewDb();
        var user = Seed.GoogleUser("Bob", "bob@gmail.com", "g-sub-1");
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new GetUserProfileHandler(_uow);
        var dto = await handler.Handle(new GetUserProfileQuery(user.Id), _ct);

        Assert.That(dto.HasPassword, Is.False);
        Assert.That(dto.HasGoogle, Is.True);
    }

    [Test]
    public void GetProfile_NotFound_Throws()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);
        var handler = new GetUserProfileHandler(_uow);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetUserProfileQuery(999), _ct));
    }

    [Test]
    public async Task UpdateName_Success()
    {
        var db = NewDb();
        var user = Seed.TestUser("OldName");
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new UpdateUserNameHandler(_uow);
        var dto = await handler.Handle(new UpdateUserNameCommand(user.Id, "NewName"), _ct);

        Assert.That(dto.Name, Is.EqualTo("NewName"));
        var updated = await db.Users.FirstAsync(_ct);
        Assert.That(updated.Name, Is.EqualTo("NewName"));
    }

    [Test]
    public void UpdateName_Empty_Throws()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);
        var handler = new UpdateUserNameHandler(_uow);

        Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new UpdateUserNameCommand(1, "  "), _ct));
    }

    [Test]
    public async Task UpdateEmail_AddAndRemove()
    {
        var db = NewDb();
        var user = Seed.TestUser("Carol");
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        // Add email
        _uow = new UnitOfWork(db);
        var handler = new UpdateUserEmailHandler(_uow);
        var dto = await handler.Handle(new UpdateUserEmailCommand(user.Id, "carol@mail.com"), _ct);

        Assert.That(dto.Email, Is.EqualTo("carol@mail.com"));

        // Remove email
        _uow = new UnitOfWork(db);
        handler = new UpdateUserEmailHandler(_uow);
        dto = await handler.Handle(new UpdateUserEmailCommand(user.Id, null), _ct);

        Assert.That(dto.Email, Is.Null);
    }

    [Test]
    public async Task ChangePassword_Success()
    {
        var db = NewDb();
        var hasher = TestHasher();
        var user = new User("Dave", hasher.Hash("oldpass"));
        db.Add(user);
        await db.SaveChangesAsync(_ct);

        _uow = new UnitOfWork(db);
        var handler = new ChangePasswordHandler(_uow, hasher);
        var dto = await handler.Handle(new ChangePasswordCommand(user.Id, "newpass123"), _ct);

        Assert.That(dto.HasPassword, Is.True);

        // Verify new password works
        var updated = await db.Users.FirstAsync(_ct);
        Assert.That(hasher.Verify("newpass123", updated.PwdHash!), Is.True);
    }

    [Test]
    public void ChangePassword_TooShort_Throws()
    {
        var db = NewDb();
        _uow = new UnitOfWork(db);
        var handler = new ChangePasswordHandler(_uow, TestHasher());

        Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new ChangePasswordCommand(1, "ab"), _ct));
    }
}
