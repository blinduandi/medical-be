using medical_be.Shared.DTOs;
using medical_be.Shared.Interfaces;
using medical_be.Shared.Events;
using medical_be.Models;
using medical_be.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace medical_be.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        RoleManager<Role> roleManager,
        IJwtService jwtService,
        ApplicationDbContext context,
        IMapper mapper,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _context = context;
        _mapper = mapper;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> AuthenticateAsync(LoginDto loginDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Authentication failed for email: {Email}", loginDto.Email);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
            {
                await _auditService.LogActivityAsync("LOGIN_FAILED", Guid.Parse(user.Id), 
                    $"Failed login attempt for {loginDto.Email}");
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is deactivated");
            }

            var userDto = await GetUserDtoAsync(user);
            var token = await _jwtService.GenerateTokenAsync(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Save refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            await _auditService.LogActivityAsync("LOGIN_SUCCESS", Guid.Parse(user.Id), 
                $"Successful login for {loginDto.Email}");

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for email: {Email}", loginDto.Email);
            throw;
        }
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var user = _mapper.Map<User>(createUserDto);
            user.UserName = createUserDto.Email;
            user.EmailConfirmed = true;

            var result = await _userManager.CreateAsync(user, createUserDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            // Assign roles
            if (createUserDto.Roles.Any())
            {
                foreach (var roleName in createUserDto.Roles)
                {
                    if (await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _userManager.AddToRoleAsync(user, roleName);
                    }
                }
            }
            else
            {
                // Default role
                await _userManager.AddToRoleAsync(user, "Patient");
            }

            var userDto = await GetUserDtoAsync(user);

            // Publish event
            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = Guid.Parse(user.Id),
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = createUserDto.Roles
            };

            await _auditService.LogActivityAsync("USER_CREATED", Guid.Parse(user.Id), 
                $"User created: {user.Email}");

            _logger.LogInformation("User created successfully: {Email}", user.Email);
            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email: {Email}", createUserDto.Email);
            throw;
        }
    }

    public async Task<UserDto> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        return await GetUserDtoAsync(user);
    }

    public async Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        _mapper.Map(updateUserDto, user);
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        await _auditService.LogActivityAsync("USER_UPDATED", userId, 
            $"User updated: {user.Email}");

        return await GetUserDtoAsync(user);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        return await _jwtService.ValidateTokenAsync(token);
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            await _auditService.LogActivityAsync("USER_DELETED", userId, 
                $"User deactivated: {user.Email}");
        }

        return result.Succeeded;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .ToListAsync();

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            userDtos.Add(await GetUserDtoAsync(user));
        }

        return userDtos;
    }

    public async Task<List<UserDto>> GetUsersByRoleAsync(string role)
    {
        var usersInRole = await _userManager.GetUsersInRoleAsync(role);
        var activeUsers = usersInRole.Where(u => u.IsActive);

        var userDtos = new List<UserDto>();
        foreach (var user in activeUsers)
        {
            userDtos.Add(await GetUserDtoAsync(user));
        }

        return userDtos;
    }

    private async Task<UserDto> GetUserDtoAsync(User user)
    {
        var userDto = _mapper.Map<UserDto>(user);
        var roles = await _userManager.GetRolesAsync(user);
        userDto.Roles = roles.ToList();
        return userDto;
    }
}
