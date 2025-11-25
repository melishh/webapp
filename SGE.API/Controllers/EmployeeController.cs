using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Employees;
using SGE.Application.Interfaces.Services;
using ExcelDataReader;
using System.Data;

namespace SGE.API.Controllers;

/// <summary>
/// API controller responsible for managing employee-related operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmployeesController(IEmployeeService employeeService) : ControllerBase
{
    /// <summary>
    /// Retrieves all employees.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An asynchronous task that returns an action result containing an enumerable collection of EmployeeDto objects.
    /// </returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll(CancellationToken cancellationToken)
    {
        var employees = await employeeService.GetAllAsync(cancellationToken);
        return Ok(employees);
    }

    /// <summary>
    /// Retrieves an employee by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An asynchronous task that returns an action result containing the employee data transfer object (EmployeeDto) if found; otherwise, a not found result.
    /// </returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var employee = await employeeService.GetByIdAsync(id, cancellationToken);
        return Ok(employee);
    }

    /// <summary>
    /// Retrieves an employee by their email address.
    /// </summary>
    /// <param name="email">The email address of the employee to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An asynchronous task that returns an action result containing the employee's data transfer object (EmployeeDto) if found, otherwise a NotFound result.
    /// </returns>
    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<EmployeeDto>> GetByEmail(string email, CancellationToken cancellationToken)
    {
        var employee = await employeeService.GetByEmailAsync(email, cancellationToken);
        if (employee == null) return NotFound();
        return Ok(employee);
    }

    /// <summary>
    /// Retrieves employees associated with a specific department.
    /// </summary>
    /// <param name="departmentId">The identifier of the department to retrieve employees for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An asynchronous task that returns an action result containing an enumerable collection of EmployeeDto objects.
    /// </returns>
    [HttpGet("department/{departmentId:int}")]
    public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetByDepartment(int departmentId, CancellationToken cancellationToken)
    {
        var employees = await employeeService.GetByDepartmentAsync(departmentId, cancellationToken);
        return Ok(employees);
    }

    /// <summary>
    /// Creates a new employee record.
    /// </summary>
    /// <param name="dto">The data transfer object containing the details of the employee to create.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An asynchronous task that returns an action result containing the created EmployeeDto object.
    /// </returns>
    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> Create(EmployeeCreateDto dto, CancellationToken cancellationToken)
    {
        var created = await employeeService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing employee with the provided details.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to be updated.</param>
    /// <param name="dto">The data transfer object containing the updated employee details.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An action result indicating the outcome of the update operation. Returns <c>NoContent</c> if the update is successful or <c>NotFound</c> if the employee is not found.
    /// </returns>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EmployeeUpdateDto dto, CancellationToken cancellationToken)
    {
        var success = await employeeService.UpdateAsync(id, dto, cancellationToken);
        if (!success)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Deletes an employee by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the employee to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An asynchronous task that returns an action result. Returns NoContent when the deletion is successful, or NotFound if the employee does not exist.
    /// </returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var success = await employeeService.DeleteAsync(id, cancellationToken);
        if (!success)
            return NotFound();
        return NoContent();
    }


    [HttpPost("import")]
    public async Task<ActionResult<EmployeeImportResultDto>> ImportFromExcel(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Aucun fichier fourni" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "Le fichier ne doit pas dépasser 10 MB" });

        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { error = "Le fichier doit être au format Excel (.xlsx ou .xls)" });

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        ExcelReaderConfiguration config;
        try
        {
            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = false
                }
            });

            if (dataSet.Tables.Count == 0)
                return BadRequest(new { error = "Le fichier Excel est vide" });

            var table = dataSet.Tables[0];

            if (table.Rows.Count < 2)
                return BadRequest(new { error = "Le fichier doit contenir au moins une ligne de données (en plus de l'en-tête)" });

            var expectedHeaders = new[] { "FirstName", "LastName", "Email", "PhoneNumber", "Address", "Position", "Salary", "HireDate", "DepartmentId" };
            var headers = new string[table.Columns.Count];

            for (int col = 0; col < table.Columns.Count && col < expectedHeaders.Length; col++)
            {
                headers[col] = table.Rows[0][col]?.ToString()?.Trim() ?? "";
            }

            var headerErrors = new List<string>();
            for (int i = 0; i < expectedHeaders.Length; i++)
            {
                if (i >= headers.Length || !string.Equals(headers[i], expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                {
                    headerErrors.Add($"Colonne {i + 1}: attendu '{expectedHeaders[i]}', trouvé '{(i < headers.Length ? headers[i] : "manquant")}'");
                }
            }

            if (headerErrors.Any())
            {
                return BadRequest(new
                {
                    error = "En-têtes du fichier Excel incorrects",
                    details = headerErrors
                });
            }

            var importResult = new EmployeeImportResultDto
            {
                TotalRows = table.Rows.Count - 1,
                SuccessCount = 0,
                ErrorCount = 0,
                Errors = new List<string>()
            };

            var emailsInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 1; i < table.Rows.Count; i++)
            {
                var lineNumber = i + 1;
                var errors = new List<string>();

                try
                {
                    var row = table.Rows[i];

                    bool isEmptyRow = true;
                    for (int col = 0; col < table.Columns.Count; col++)
                    {
                        if (!string.IsNullOrWhiteSpace(row[col]?.ToString()))
                        {
                            isEmptyRow = false;
                            break;
                        }
                    }

                    if (isEmptyRow)
                    {
                        importResult.TotalRows--;
                        continue;
                    }

                    var firstName = row[0]?.ToString()?.Trim();
                    var lastName = row[1]?.ToString()?.Trim();
                    var email = row[2]?.ToString()?.Trim();
                    var phoneNumber = row[3]?.ToString()?.Trim();
                    var address = row[4]?.ToString()?.Trim();
                    var position = row[5]?.ToString()?.Trim();
                    var salaryStr = row[6]?.ToString()?.Trim();
                    var hireDateStr = row[7]?.ToString()?.Trim();
                    var departmentIdStr = row[8]?.ToString()?.Trim();

                    if (string.IsNullOrWhiteSpace(firstName))
                        errors.Add("Le prénom est obligatoire");
                    else if (firstName.Length < 2 || firstName.Length > 50)
                        errors.Add("Le prénom doit contenir entre 2 et 50 caractères");
                    else if (!System.Text.RegularExpressions.Regex.IsMatch(firstName, @"^[a-zA-ZÀ-ÿ\s\-']+$"))
                        errors.Add("Le prénom contient des caractères invalides");
                    else if (firstName.Contains("  "))
                        errors.Add("Le prénom contient des espaces multiples");

                    if (string.IsNullOrWhiteSpace(lastName))
                        errors.Add("Le nom est obligatoire");
                    else if (lastName.Length < 2 || lastName.Length > 50)
                        errors.Add("Le nom doit contenir entre 2 et 50 caractères");
                    else if (!System.Text.RegularExpressions.Regex.IsMatch(lastName, @"^[a-zA-ZÀ-ÿ\s\-']+$"))
                        errors.Add("Le nom contient des caractères invalides");
                    else if (lastName.Contains("  "))
                        errors.Add("Le nom contient des espaces multiples");

                    if (string.IsNullOrWhiteSpace(email))
                        errors.Add("L'email est obligatoire");
                    else
                    {
                        email = email.ToLowerInvariant();
                        if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$"))
                            errors.Add("L'email n'est pas valide");
                        else if (email.Length > 100)
                            errors.Add("L'email est trop long (max 100 caractères)");
                        else if (emailsInFile.Contains(email))
                            errors.Add($"L'email '{email}' apparaît plusieurs fois dans le fichier");
                        else
                        {
                            var existingEmployee = await employeeService.GetByEmailAsync(email, cancellationToken);
                            if (existingEmployee != null)
                                errors.Add($"L'email '{email}' existe déjà dans la base de données");
                            else
                                emailsInFile.Add(email);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        var cleanPhone = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d+]", "");
                        if (cleanPhone.Length < 10 || cleanPhone.Length > 15)
                            errors.Add("Le numéro de téléphone doit contenir entre 10 et 15 chiffres");
                        else if (!System.Text.RegularExpressions.Regex.IsMatch(cleanPhone, @"^[\d+]+$"))
                            errors.Add("Le numéro de téléphone contient des caractères invalides");
                    }

                    if (!string.IsNullOrWhiteSpace(address))
                    {
                        if (address.Length > 200)
                            errors.Add("L'adresse est trop longue (max 200 caractères)");
                        if (address.Contains("  "))
                            address = System.Text.RegularExpressions.Regex.Replace(address, @"\s+", " ");
                    }

                    if (!string.IsNullOrWhiteSpace(position))
                    {
                        if (position.Length > 100)
                            errors.Add("Le poste est trop long (max 100 caractères)");
                        if (position.Contains("  "))
                            position = System.Text.RegularExpressions.Regex.Replace(position, @"\s+", " ");
                    }

                    decimal salary = 0;
                    if (string.IsNullOrWhiteSpace(salaryStr))
                        errors.Add("Le salaire est obligatoire");
                    else
                    {
                        var cleanSalary = salaryStr.Replace(" ", "").Replace(",", ".");
                        if (!decimal.TryParse(cleanSalary, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out salary))
                            errors.Add($"Le salaire '{salaryStr}' n'est pas un nombre valide");
                        else if (salary < 0)
                            errors.Add("Le salaire ne peut pas être négatif");
                        else if (salary == 0)
                            errors.Add("Le salaire ne peut pas être zéro");
                        else if (salary > 1000000)
                            errors.Add("Le salaire est trop élevé (max 1 000 000)");
                    }

                    DateTime hireDate = DateTime.MinValue;
                    if (string.IsNullOrWhiteSpace(hireDateStr))
                        errors.Add("La date d'embauche est obligatoire");
                    else
                    {
                        var dateFormats = new[]
                        {
                            "yyyy-MM-dd",
                            "dd/MM/yyyy",
                            "MM/dd/yyyy",
                            "dd-MM-yyyy",
                            "yyyy/MM/dd",
                            "d/M/yyyy",
                            "M/d/yyyy"
                        };

                        if (!DateTime.TryParseExact(hireDateStr, dateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out hireDate))
                        {
                            if (!DateTime.TryParse(hireDateStr, out hireDate))
                            {
                                errors.Add($"La date d'embauche '{hireDateStr}' n'est pas valide (formats acceptés: YYYY-MM-DD, DD/MM/YYYY, MM/DD/YYYY)");
                            }
                        }

                        if (hireDate != DateTime.MinValue)
                        {
                            if (hireDate < new DateTime(1900, 1, 1))
                                errors.Add("La date d'embauche est trop ancienne (min: 01/01/1900)");
                            else if (hireDate > DateTime.Now.AddDays(1))
                                errors.Add("La date d'embauche ne peut pas être dans le futur");
                        }
                    }

                    int departmentId = 0;
                    if (string.IsNullOrWhiteSpace(departmentIdStr))
                        errors.Add("L'ID du département est obligatoire");
                    else if (!int.TryParse(departmentIdStr, out departmentId))
                        errors.Add($"L'ID du département '{departmentIdStr}' n'est pas un nombre valide");
                    else if (departmentId <= 0)
                        errors.Add("L'ID du département doit être positif");

                    if (errors.Any())
                    {
                        importResult.ErrorCount++;
                        importResult.Errors.Add($"Ligne {lineNumber}: {string.Join(", ", errors)}");
                        continue;
                    }

                    var dto = new EmployeeCreateDto
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        PhoneNumber = phoneNumber ?? "",
                        Address = address ?? "",
                        Position = position ?? "",
                        Salary = salary,
                        HireDate = DateTime.SpecifyKind(hireDate, DateTimeKind.Utc),
                        DepartmentId = departmentId
                    };

                    await employeeService.CreateAsync(dto, cancellationToken);
                    importResult.SuccessCount++;
                }
                catch (ApplicationException ex)
                {
                    importResult.ErrorCount++;
                    importResult.Errors.Add($"Ligne {lineNumber}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    importResult.ErrorCount++;

                    var innerException = ex;
                    while (innerException.InnerException != null)
                        innerException = innerException.InnerException;

                    importResult.Errors.Add($"Ligne {lineNumber}: {innerException.Message}");
                }
            }

            return Ok(importResult);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Erreur lors de la lecture du fichier: {ex.Message}" });
        }
    }


}
