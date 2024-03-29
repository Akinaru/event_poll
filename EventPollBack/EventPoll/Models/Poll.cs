namespace EventPoll.Models
{
    public class Poll
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? ImageName { get; set; }
        public DateTime EventDate { get; set; }
        public int UserId { get; set; }

        public User? User { get; set; }
        public virtual ICollection<Vote>? Votes { get; set; }
    }
}
