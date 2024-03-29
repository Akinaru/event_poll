using EventPoll.Models;
using FluentValidation;

namespace EventPoll.Dtos
{
    public record UserDto(int Id, string Username, string? Role)
    {
        public UserDto(User user)
            : this(user.Id, user.Username!, user.Role) { }
    }

    public record UserLoginTokenDto(string Token);

    public record UserSignupDto(string Username, string Password)
    {
        public class Validator : AbstractValidator<UserSignupDto>
        {
            public Validator()
            {
                RuleFor(x => x.Username).NotEmpty();
                RuleFor(x => x.Password).NotEmpty();
            }
        }
    }

    public record UserLoginDto(string Username, string Password)
    {
        public class Validator : AbstractValidator<UserLoginDto>
        {
            public Validator()
            {
                RuleFor(x => x.Username).NotEmpty();
                RuleFor(x => x.Password).NotEmpty();
            }
        }
    }
}
