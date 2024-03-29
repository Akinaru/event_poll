using EventPoll.Models;
using FluentValidation;

namespace EventPoll.Dtos
{
    public record PollDto(int Id, string Name, string Description, string? ImageName, DateTime EventDate, UserDto User)
    {
        public PollDto(Poll poll)
            : this(poll.Id, poll.Name, poll.Description, poll.ImageName, poll.EventDate, new UserDto(poll.User!)) { }
    }

    public record PollWithVotesDto(int Id, string Name, string Description, string? ImageName, DateTime EventDate, UserDto User, IEnumerable<VoteDto> Votes) : PollDto(Id, Name, Description, ImageName, EventDate, User)
    {
        public PollWithVotesDto(Poll poll)
           : this(poll.Id, poll.Name, poll.Description, poll.ImageName, poll.EventDate, new UserDto(poll.User!), poll.Votes?.Select(v => new VoteDto(v)) ?? Array.Empty<VoteDto>()) { }
    }

    public record PollCreateDto(string Name, string Description, DateTime EventDate)
    {
        public class Validator : AbstractValidator<PollCreateDto>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty();
                RuleFor(x => x.Description).NotEmpty();
                RuleFor(x => x.EventDate).NotNull().GreaterThan(DateTime.Today);
            }
        }
    }

    public record PollUpdateDto(string? Name, string? Description, DateTime? EventDate)
    {
        public class Validator : AbstractValidator<PollUpdateDto>
        {
            public Validator()
            {
                RuleFor(x => x.EventDate).GreaterThan(DateTime.Today);
            }
        }
    }
}
