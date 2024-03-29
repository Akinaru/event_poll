using Microsoft.EntityFrameworkCore;

namespace EventPoll.Models
{
    [PrimaryKey(nameof(PollId), nameof(UserId))]
    public class Vote
    {
        public int PollId { get; set; }
        public int UserId { get; set; }
        public bool Status { get; set; }
        public DateTime Created { get; set; }

        public User? User { get; set; }
    }
}
