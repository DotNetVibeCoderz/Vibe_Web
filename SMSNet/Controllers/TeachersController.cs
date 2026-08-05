using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Controllers;

/// <summary>
/// Teacher records over REST. Reads are staff-only; writes are admin-only.
/// This endpoint previously carried no authorization at all.
/// </summary>
[ApiController]
[Route("api/teachers")]
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Guru)]
public class TeachersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TeachersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Teacher>>> GetAll()
    {
        return await _context.Teachers.AsNoTracking().ToListAsync();
    }
}
