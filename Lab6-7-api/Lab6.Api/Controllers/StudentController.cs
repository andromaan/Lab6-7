using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Lab4.Data;
[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly StudentRepository _repository;
    private readonly IValidator<CreateStudentRequest> _createValidator;
    private readonly IValidator<UpdateStudentRequest> _updateValidator;
    
    public StudentController(
        StudentRepository repository,
        IValidator<CreateStudentRequest> createValidator,
        IValidator<UpdateStudentRequest> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var students = await _repository.GetAllAsync();
        return Ok(students);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null)
            return NotFound();
            
        return Ok(student);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null)
            return NotFound();
            
        await _repository.DeleteAsync(id);
        return NoContent();
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateStudentRequest request)
    {
        var validationResult = await _createValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { Errors = errors });
        }
        var student = new Student
        {
            FullName = request.FullName,
            Email = request.Email,
            EnrollmentDate = request.EnrollmentDate
        };
        
        try 
        {
            await _repository.AddAsync(student);
            return Created("", student);
        } 
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString());
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateStudentRequest request)
    {
        var validationResult = await _updateValidator.ValidateAsync(request);   
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { Errors = errors });
        }
        var student = await _repository.GetByIdAsync(request.Id);
        if (student != null)
        {
            student.FullName = request.FullName;
            student.Email = request.Email;
            await _repository.UpdateAsync(student);
            return Ok(student);
        }
        return NotFound();
    }
}
