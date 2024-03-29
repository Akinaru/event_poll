using EventPoll.Models;
using FluentValidation;

namespace EventPoll.Dtos
{
    public record VoteDto(int PollId, bool Status, DateTime Created, UserDto User)
    {
        public VoteDto(Vote vote)
            : this(vote.PollId, vote.Status, vote.Created, new UserDto(vote.User!)) { }
    }

    public record VoteCreateDto(bool Status)
    {
        public class Validator : AbstractValidator<VoteCreateDto>
        {
            public Validator()
            {
                RuleFor(x => x.Status).NotNull();
            }
        }
    }

    public record VoteUpdateDto(bool Status)
    {
        public class Validator : AbstractValidator<VoteUpdateDto>
        {
            public Validator()
            {
                RuleFor(x => x.Status).NotNull();
            }
        }
    }
}
