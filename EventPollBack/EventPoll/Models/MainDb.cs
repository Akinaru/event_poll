using Microsoft.EntityFrameworkCore;

namespace EventPoll.Models
{
    public class MainDb : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Poll> Polls { get; set; }
        public DbSet<Vote> Votes { get; set; }

        public MainDb(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(new User { Id = 1, Username = "admin", Password = "admin", Role = "admin" });
            modelBuilder.Entity<User>().HasData(new User { Id = 2, Username = "user1", Password = "user1" });
            modelBuilder.Entity<User>().HasData(new User { Id = 3, Username = "user2", Password = "user2" });

            modelBuilder.Entity<Poll>().HasData(new Poll { Id = 1, UserId = 1, Name = "Un premier événement", Description = "Ceci est un premier événement de test.", EventDate = new DateTime(2023, 4, 1) });

            modelBuilder.Entity<Vote>().HasData(new Vote { PollId = 1, UserId = 2, Status = true, Created = DateTime.Now.AddDays(-2) });
        }

        public Task<User?> FindUser(string username, string password)
        {
            return Users.FirstOrDefaultAsync(u => u.Username.Equals(username, StringComparison.CurrentCultureIgnoreCase) && u.Password.Equals(password));
        }

        public Task<bool> ExistUser(string username)
        {
            return Users.AnyAsync(u => u.Username.Equals(username, StringComparison.CurrentCultureIgnoreCase));
        }

        public Task<Poll?> FindPoll(int id, bool includeVotes = false)
        {
            return GetPolls(includeVotes).FirstOrDefaultAsync(p => p.Id == id);
        }

        public IQueryable<Poll> GetPolls(bool includeVotes = false)
        {
            var query = Polls.Include(nameof(Poll.User));

            if (includeVotes)
            {
                query = query.Include(nameof(Poll.Votes)).Include($"{nameof(Poll.Votes)}.{nameof(Vote.User)}");
            }

            return query;
        }

        public Task<Vote?> FindVote(int pollId, int userId)
        {
            return Votes.Include(nameof(Vote.User)).FirstOrDefaultAsync(p => p.PollId == pollId && p.UserId == userId);
        }
    }
}
