using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Controllers;

[ApiController]
[Route("api/teachers")]
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
