using EventPoll;
using EventPoll.Dtos;
using EventPoll.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

#region Configs

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication().AddJwtBearer(c =>
{
    c.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});
builder.Services.AddAuthorization(options => options.AddPolicy("admin", policy => policy.RequireClaim(ClaimTypes.Role, "admin")));

builder.Services.AddValidatorsFromAssemblyContaining<UserLoginDto.Validator>();

builder.Services.AddEndpointsApiExplorer();

var connectionString = builder.Configuration.GetConnectionString("EventPollContext");
if (string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<MainDb>(options => options.UseInMemoryDatabase("items"));
}
else
{
    builder.Services.AddDbContext<MainDb>(options => options.UseSqlServer(connectionString));
}

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventPoll API",
        Description = "Gestionnaire de participations aux événements.",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JSON Web Token based security",
    });
    c.OperationFilter<SecurityRequirementsOperationFilter>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    using var db = services.GetRequiredService<MainDb>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

#endregion Configs

#region Routes

#region Auth

app.MapPost("/auth/login", async (MainDb db, UserLoginDto dto, IValidator<UserLoginDto> validator) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    var user = await db.FindUser(dto.Username, dto.Password);

    if (user == null)
    {
        return Results.Unauthorized();
    }

    var issuer = builder.Configuration["Jwt:Issuer"];
    var audience = builder.Configuration["Jwt:Audience"];
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    };
    if (!string.IsNullOrEmpty(user.Role))
    {
        claims.Add(new Claim(ClaimTypes.Role, user.Role));
    }

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        signingCredentials: credentials
    );

    var tokenHandler = new JwtSecurityTokenHandler();
    var stringToken = tokenHandler.WriteToken(token);

    return Results.Ok(new UserLoginTokenDto(stringToken));
})
    .WithDescription("Authentifie un utilisateur en retournant un token.")
    .Produces<UserLoginTokenDto>()
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status401Unauthorized)
    .WithOpenApi();

app.MapPost("auth/signup", async (MainDb db, UserSignupDto dto, IValidator<UserSignupDto> validator) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    if (await db.ExistUser(dto.Username))
    {
        return Results.Conflict();
    }

    var user = new User
    {
        Username = dto.Username,
        Password = dto.Password
    };

    await db.Users.AddAsync(user);
    await db.SaveChangesAsync();
    return Results.Created($"/users/{user.Id}", new UserDto(user));
})
    .WithDescription("Inscrit un nouvel utilisateurs à l'application.")
    .Produces<UserDto>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status409Conflict)
    .WithOpenApi();

#endregion Auth

#region Users

app.MapGet("/users", [Authorize] async (MainDb db) => Results.Ok(await db.Users.Select(u => new UserDto(u)).ToListAsync()))
    .WithDescription("Liste tous les utilisateurs inscrits.")
    .Produces<IEnumerable<UserDto>>()
    .WithOpenApi();

app.MapGet("/users/{id}", [Authorize] async (int id, MainDb db) =>
{
    var user = await db.Users.FindAsync(id);
    return user is null ? Results.NotFound() : Results.Ok(new UserDto(user));
})
    .WithDescription("Détaille un utilisateur.")
    .Produces<UserDto>()
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

app.MapGet("/users/me", [Authorize] async (ClaimsPrincipal user, MainDb db) =>
{
    if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    var dbUser = await db.Users.FindAsync(userId);
    return dbUser is null ? Results.NotFound() : Results.Ok(new UserDto(dbUser));
})
    .WithDescription("Détaille l'utilisateur actuellement connecté.")
    .Produces<UserDto>()
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

#endregion Users

#region Polls

app.MapGet("/polls", async (MainDb db) => Results.Ok(await db.GetPolls().Select(p => new PollDto(p)).ToListAsync()))
    .WithDescription("Liste tous les sondages d'événements.")
    .Produces<IEnumerable<PollDto>>()
    .WithOpenApi();

app.MapPost("/polls", [Authorize(Policy = "admin")] async (PollCreateDto dto, IValidator<PollCreateDto> validator, ClaimsPrincipal user, MainDb db) =>
{
    if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    var poll = new Poll
    {
        Name = dto.Name,
        Description = dto.Description,
        EventDate = dto.EventDate,
        UserId = userId
    };

    await db.Polls.AddAsync(poll);
    await db.SaveChangesAsync();

    poll = await db.FindPoll(poll.Id, includeVotes: true);
    return Results.Created($"/polls/{poll!.Id}", new PollWithVotesDto(poll));
})
    .WithDescription("Crée un nouveau sondage d'événement.")
    .Produces<PollWithVotesDto>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .WithOpenApi();

app.MapGet("/polls/{id}", async (int id, MainDb db) =>
{
    var poll = await db.FindPoll(id, includeVotes: true);
    return poll is null ? Results.NotFound() : Results.Ok(new PollWithVotesDto(poll));
})
    .WithDescription("Détaille un sondage d'événement.")
    .Produces<PollWithVotesDto>()
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

app.MapPut("/polls/{id}", [Authorize(Policy = "admin")] async (int id, PollUpdateDto dto, IValidator<PollUpdateDto> validator, MainDb db) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    var poll = await db.FindPoll(id, includeVotes: true);
    if (poll is null)
    {
        return Results.NotFound();
    }

    poll.Name = dto.Name ?? poll.Name;
    poll.Description = dto.Description ?? poll.Description;
    poll.EventDate = dto.EventDate ?? poll.EventDate;
    await db.SaveChangesAsync();
    return Results.Ok(new PollWithVotesDto(poll));
})
    .WithDescription("Modifie un sondage d'événement.")
    .Produces<PollWithVotesDto>()
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

app.MapPost("/polls/{id}/image", [Authorize(Policy = "admin")] async (int id, MainDb db, IWebHostEnvironment env, HttpRequest request) =>
{
    var poll = await db.FindPoll(id);
    if (poll is null)
    {
        return Results.NotFound();
    }

    if (!string.IsNullOrEmpty(poll.ImageName))
    {
        var fileInfo = new FileInfo($"{env.ContentRootPath}/images/{poll.ImageName}");

        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }
    }

    var formFile = request.Form.Files.FirstOrDefault();

    if (formFile is null)
    {
        return Results.BadRequest();
    }

    string extension;
    if (!string.IsNullOrEmpty(formFile.FileName))
    {
        extension = new FileInfo(formFile.FileName).Extension;
    }
    else
    {
        var mimeType = formFile.ContentType.ToLowerInvariant().Split('/');
        if (mimeType.Length != 2 || mimeType[0] != "image")
        {
            return Results.BadRequest();
        }

        extension = "." + mimeType[1];
    }

    if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
    {
        return Results.BadRequest();
    }

    var fileName = Guid.NewGuid().ToString() + extension;

    using (var inputStream = formFile.OpenReadStream())
    using (var outputStream = File.OpenWrite($"{env.ContentRootPath}/images/{fileName}"))
    {
        await inputStream.CopyToAsync(outputStream);
    }

    poll.ImageName = fileName;
    await db.SaveChangesAsync();
    return Results.Ok(new PollDto(poll));
})
    .WithDescription("Ajoute ou modifie l'image d'un sondage d'événement.")
    .Accepts<IFormFile>("multipart/form-data")
    .Produces<PollDto>()
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

app.MapDelete("/polls/{id}/image", [Authorize(Policy = "admin")] async (int id, MainDb db, IWebHostEnvironment env) =>
{
    var poll = await db.Polls.FindAsync(id);
    if (poll is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrEmpty(poll.ImageName))
    {
        return Results.NoContent();
    }

    var fileInfo = new FileInfo($"{env.ContentRootPath}/images/{poll.ImageName}");

    if (fileInfo.Exists)
    {
        fileInfo.Delete();
    }

    poll.ImageName = null;
    await db.SaveChangesAsync();
    return Results.NoContent();
})
    .WithDescription("Supprime l'image d'un sondage d'événement.")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

app.MapDelete("/polls/{id}", [Authorize(Policy = "admin")] async (int id, MainDb db) =>
{
    var poll = await db.Polls.FindAsync(id);
    if (poll is null)
    {
        return Results.NotFound();
    }

    db.Polls.Remove(poll);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
    .WithDescription("Supprime un sondage d'événement.")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

#region Votes

app.MapGet("/polls/{id}/votes", async (int id, MainDb db) =>
{
    var poll = await db.FindPoll(id, includeVotes: true);
    if (poll is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(poll.Votes!.Select(v => new VoteDto(v)));
})
    .WithDescription("Liste tous les votes à un sondage d'événement.")
    .Produces<IEnumerable<VoteDto>>()
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

app.MapPost("/polls/{id}/votes", [Authorize] async (int id, VoteCreateDto dto, IValidator<VoteCreateDto> validator, ClaimsPrincipal user, MainDb db) =>
{
    if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    if (!await db.Polls.AnyAsync(p => p.Id == id))
    {
        return Results.NotFound();
    }

    var vote = await db.FindVote(id, userId);

    if (vote == null)
    {
        vote = new Vote
        {
            PollId = id,
            UserId = userId,
            Status = dto.Status
        };
        db.Votes.Add(vote);
        await db.SaveChangesAsync();
        vote.User = await db.Users.FindAsync(userId);
    }
    else
    {
        vote.Status = dto.Status;
        await db.SaveChangesAsync();
    }

    return Results.Created($"/polls/{id}/votes", new VoteDto(vote!));
})
    .WithDescription("Crée ou modifie un vote à un sondage d'événement.")
    .Produces<VoteDto>(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

app.MapDelete("/polls/{id}/votes", [Authorize(Policy = "admin")] async (int id, ClaimsPrincipal user, MainDb db) =>
{
    if (!int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
    {
        return Results.Unauthorized();
    }

    if (!await db.Polls.AnyAsync(p => p.Id == id))
    {
        return Results.NotFound();
    }

    var vote = await db.Votes.FirstOrDefaultAsync(v => v.PollId == id && v.UserId == userId);

    if (vote is null)
    {
        return Results.NotFound();
    }

    db.Votes.Remove(vote);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
    .WithDescription("Supprime un vote à un sondage d'événement.")
    .Produces(StatusCodes.Status204NoContent)
    .WithOpenApi();

#endregion Votes

#endregion Polls

app.MapGet("/images/{name}", async (string name, IWebHostEnvironment env) =>
{
    var fileInfo = new FileInfo($"{env.ContentRootPath}/images/{name}");

    if (!fileInfo.Exists)
    {
        return Results.NotFound();
    }

    var mimeType = fileInfo.Extension switch
    {
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".jpeg" => "image/jpeg",
        _ => null
    };

    if (string.IsNullOrEmpty(mimeType))
    {
        return Results.NotFound();
    }

    var file = await File.ReadAllBytesAsync(fileInfo.FullName);

    return Results.File(file, mimeType);
})
    .WithDescription("Donne une image (PNG ou JPEG) enregistrée sur le serveur.")
    .Produces<byte[]>(StatusCodes.Status200OK, contentType: "image/png", "image/jpeg")
    .Produces(StatusCodes.Status404NotFound)
    .WithOpenApi();

#endregion Routes

app.Run();
