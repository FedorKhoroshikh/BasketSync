using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
        public DbSet<ListItem> ListItems => Set<ListItem>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
        public DbSet<User> Users => Set<User>();
        public DbSet<DiscountCard> DiscountCards => Set<DiscountCard>();
        public DbSet<CardIdentifier> CardIdentifiers => Set<CardIdentifier>();

        /// <summary>
        /// Entities validation
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Unique fields setup
            modelBuilder.Entity<Unit>().HasIndex(u => u.Name).IsUnique();
            modelBuilder.Entity<Category>().HasIndex(c => c.Name).IsUnique();
            modelBuilder.Entity<Item>().HasIndex(i => i.Name).IsUnique();
            modelBuilder.Entity<ShoppingList>().HasIndex(s => s.Name).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Name).IsUnique();
            
            // Required fields setup
            modelBuilder.Entity<Unit>().Property(u => u.Name).IsRequired();
            modelBuilder.Entity<Category>().Property(c => c.Name).IsRequired();
            modelBuilder.Entity<Item>().Property(i => i.Name).IsRequired();
            modelBuilder.Entity<ShoppingList>().Property(s => s.Name).IsRequired();
            modelBuilder.Entity<User>().Property(u => u.Name).IsRequired();
            modelBuilder.Entity<User>().Property(u => u.PwdHash).IsRequired();
            modelBuilder.Entity<ListItem>().Property(l => l.Quantity).IsRequired();
            modelBuilder.Entity<ListItem>().Property(l => l.IsChecked).IsRequired();
            
            // Tables relationship setup
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Unit)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ListItem>()
                .HasOne(li => li.Item)
                .WithMany(i => i.ListItems)
                .HasForeignKey(li => li.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ListItem>()
                .HasOne(li => li.List)
                .WithMany(l => l.ListItems)
                .HasForeignKey(li => li.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            
            modelBuilder.Entity<ShoppingList>()
                .HasOne(s => s.User)
                .WithMany(u => u.Lists)
                .HasForeignKey(s => s.UserId);

            // DiscountCard
            modelBuilder.Entity<DiscountCard>().Property(d => d.Name).IsRequired();
            modelBuilder.Entity<DiscountCard>()
                .HasOne(d => d.User)
                .WithMany(u => u.DiscountCards)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // CardIdentifier
            modelBuilder.Entity<CardIdentifier>().Property(ci => ci.Type).IsRequired();
            modelBuilder.Entity<CardIdentifier>()
                .HasOne(ci => ci.DiscountCard)
                .WithMany(d => d.Identifiers)
                .HasForeignKey(ci => ci.DiscountCardId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
