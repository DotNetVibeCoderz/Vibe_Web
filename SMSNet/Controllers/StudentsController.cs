using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public StudentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetAll()
    {
        return await _context.Students.AsNoTracking().ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Student>> GetById(int id)
    {
        var student = await _context.Students.FindAsync(id);
        return student == null ? NotFound() : student;
    }

    [HttpPost]
    public async Task<ActionResult<Student>> Create(Student student)
    {
        _context.Students.Add(student);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Student student)
    {
        if (id != student.Id)
        {
            return BadRequest();
        }

        _context.Entry(student).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _context.Students.FindAsync(id);
        if (student == null)
        {
            return NotFound();
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
