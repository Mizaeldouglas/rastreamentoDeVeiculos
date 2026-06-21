using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Rastreador.Api.Data;
using Rastreador.Api.Models;
using Rastreador.Api.Services;

namespace Rastreador.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _tokenService;

    public AuthController(AppDbContext db, UserManager<ApplicationUser> userManager, JwtTokenService tokenService)
    {
        _db = db;
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Nome da empresa, e-mail e senha são obrigatórios.");

        var company = new Company { Name = dto.CompanyName.Trim() };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            CompanyId = company.Id
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            _db.Companies.Remove(company);
            await _db.SaveChangesAsync();
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        var (token, expiresAt) = _tokenService.GenerateToken(user, company);
        return Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email!,
            CompanyId = company.Id,
            CompanyName = company.Name
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("E-mail ou senha inválidos.");

        var company = await _db.Companies.FindAsync(user.CompanyId);
        if (company is null) return Unauthorized();

        var (token, expiresAt) = _tokenService.GenerateToken(user, company);
        return Ok(new AuthResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email!,
            CompanyId = company.Id,
            CompanyName = company.Name
        });
    }
}
