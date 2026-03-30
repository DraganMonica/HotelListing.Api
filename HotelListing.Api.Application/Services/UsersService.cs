using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Auth;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Config;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HotelListing.Api.Application.Services;

public class UsersService(UserManager<ApplicationUser> userManager,
    HotelListingDbContext hotelListingDbContext,
    IOptions<JwtSettings> jwtOptions,
    IHttpContextAccessor httpContextAccessor,
    ILogger<UsersService> logger) : IUsersService
{
    public async Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto)
    {
        var user = new ApplicationUser()
        {
            Email = registerUserDto.Email,
            FirstName = registerUserDto.FirstName,
            LastName = registerUserDto.LastName,
            UserName = registerUserDto.Email
        };

        var result = await userManager.CreateAsync(user, registerUserDto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new Error(ErrorCodes.BadRequest, e.Description)).ToArray();

            logger.LogWarning("User registration failed login for {Email}: {Errors}", registerUserDto.Email, string.Join(", ", errors));

            return Result<RegisteredUserDto>.BadRequest(errors);
        }

        //assign the role here before register user
        await userManager.AddToRoleAsync(user, registerUserDto.Role);

        if(registerUserDto.Role==RoleNames.HotelAdmin)
        {
            var hotelAdmin = hotelListingDbContext.HotelAdmins.Add(
                new HotelAdmin
                {
                    UserId = user.Id,
                    HotelId = registerUserDto.AssociatedHotelId.GetValueOrDefault()
                });
            await hotelListingDbContext.SaveChangesAsync();
        }
        var registeredUser = new RegisteredUserDto
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Id = user.Id,
            Role=registerUserDto.Role

        };

        return Result<RegisteredUserDto>.Success(registeredUser);
    }

    public async Task<Result<string>> LoginAsync(LoginUserDto loginUserDto)
    {
        var user = await userManager.FindByEmailAsync(loginUserDto.Email);
        if (user is null)
        {
            logger.LogWarning("Failed login attempt for email:{Email}", loginUserDto.Email);
            return Result<string>.Failure(new Error(ErrorCodes.BadRequest, "Invalid Credentials"));
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, loginUserDto.Password);
        if (!isPasswordValid)
        {
            return Result<string>.Failure(new Error(ErrorCodes.BadRequest, "Invalid Credentials"));
        }

        //issue a token
        var token = await GenerateToken(user);

        return Result<string>.Success(token);
    }


    public string UserId=> httpContextAccessor?
                .HttpContext?
                .User?
                //incearca sub
                .FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                //daca nu, NameIdentifier
                ?? httpContextAccessor?
                    .HttpContext?
                    .User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value
                //daca nu, empty
                ?? string.Empty;
    private async Task<string> GenerateToken(ApplicationUser user)
    {
        //set basic user claims
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, user.FullName)
        };

        //set user role claims
        var roles = await userManager.GetRolesAsync(user);
        var roleClaims=roles.Select(x=>new Claim(ClaimTypes.Role,x)).ToList();

        claims=claims.Union(roleClaims).ToList();

        //set jwt key credentials
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key));
        // this key will be hashed
        var credentials=new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        //create an encoded token
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Value.Issuer,
            audience: jwtOptions.Value.Audience,
            claims: claims,
            expires:DateTime.UtcNow.AddMinutes(Convert.ToInt32(jwtOptions.Value.DurationInMinutes)),
            signingCredentials:credentials
            );

        //return token value
        return new JwtSecurityTokenHandler().WriteToken(token);

    }
}
