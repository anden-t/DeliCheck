using DeliCheck.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliCheck
{
    /// <summary>
    /// База данных
    /// </summary>
    public class DatabaseContext : DbContext
    {
        /// <summary>
        /// Таблица токенов сессий
        /// </summary>
        public DbSet<SessionTokenModel> SessionTokens { get; set; }
        /// <summary>
        /// Таблица зарегистированных друзей
        /// </summary>
        public DbSet<FriendModel> Friends { get; set; }
        /// <summary>
        /// Таблица незарегистрированных друзей
        /// </summary>
        public DbSet<OfflineFriendModel> OfflineFriends { get; set; }
        /// <summary>
        /// Таблица данных ВК для входа
        /// </summary>
        public DbSet<VkAuthorizationData> VkAuth { get; set; }
        /// <summary>
        /// Таблица пользователей
        /// </summary>
        public DbSet<UserModel> Users { get; set; }
        /// <summary>
        /// Таблица чеков
        /// </summary>
        public DbSet<InvoiceModel> Invoices { get; set; }
        /// <summary>
        /// Таблица позиций чеков
        /// </summary>
        public DbSet<InvoiceItemModel> InvoicesItems { get; set; }
        /// <summary>
        /// Таблица счетов пользователей
        /// </summary>
        public DbSet<BillModel> Bills { get; set; }
        /// <summary>
        /// Таблица позиций счетов пользователей
        /// </summary>
        public DbSet<BillItemModel> BillsItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename=database.db");
        }
    }
}
