namespace SGE.Core.Exceptions;

/// <summary>
/// Base exception class for all SGE-specific exceptions.
/// </summary>
public abstract class SgeException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    protected SgeException(string message, string errorCode, int statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Represents an exception that is thrown when a business rule is violated within the system.
/// </summary>
public class BusinessRuleException : SgeException
{
    public BusinessRuleException(string message, string errorCode = "BUSINESS_RULE_VIOLATION")
        : base(message, errorCode, 400)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when an employee with a specified ID cannot be found.
/// </summary>
public class EmployeeNotFoundException : SgeException
{
    public EmployeeNotFoundException(int employeeId)
        : base($"Employé avec l'ID {employeeId} introuvable.", "EMPLOYEE_NOT_FOUND", 404)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when a specific department cannot be found.
/// </summary>
public class DepartmentNotFoundException : SgeException
{
    public DepartmentNotFoundException(int departmentId)
        : base($"Département avec l'ID {departmentId} introuvable.", "DEPARTMENT_NOT_FOUND", 404)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when a leave request cannot be found in the system.
/// </summary>
public class LeaveRequestNotFoundException : SgeException
{
    public LeaveRequestNotFoundException(int leaveRequestId)
        : base($"Demande de congé avec l'ID {leaveRequestId} introuvable.", "LEAVE_REQUEST_NOT_FOUND", 404)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when there are insufficient leave days available for a request.
/// </summary>
public class InsufficientLeaveDaysException : SgeException
{
    public InsufficientLeaveDaysException(int requiredDays, int availableDays)
        : base($"Jours de congé insuffisants. Demandé: {requiredDays}, Disponible: {availableDays}", "INSUFFICIENT_LEAVE_DAYS", 400)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when a leave request conflicts with another existing leave request.
/// </summary>
public class ConflictingLeaveRequestException : SgeException
{
    public ConflictingLeaveRequestException(DateTime startDate, DateTime endDate)
        : base($"Conflit de congé détecté pour la période du {startDate:dd/MM/yyyy} au {endDate:dd/MM/yyyy}", "CONFLICTING_LEAVE_REQUEST", 409)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when an invalid status transition occurs for a leave request.
/// </summary>
public class InvalidLeaveStatusTransitionException : SgeException
{
    public InvalidLeaveStatusTransitionException(string currentStatus, string newStatus)
        : base($"Transition de statut invalide de '{currentStatus}' vers '{newStatus}'", "INVALID_STATUS_TRANSITION", 400)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when one or more validation errors occur in the system.
/// </summary>
public class ValidationException : SgeException
{
    public Dictionary<string, List<string>> Errors { get; }

    public ValidationException(Dictionary<string, List<string>> errors)
        : base("Une ou plusieurs erreurs de validation sont survenues.", "VALIDATION_ERROR", 400)
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : this(new Dictionary<string, List<string>> { { propertyName, new List<string> { errorMessage } } })
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when an issue related to attendance operations occurs.
/// </summary>
public class AttendanceException : SgeException
{
    public AttendanceException(string message, string errorCode = "ATTENDANCE_ERROR")
        : base(message, errorCode, 400)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when an attempt is made to clock in an employee who is already clocked in.
/// </summary>
public class AlreadyClockedInException : SgeException
{
    public AlreadyClockedInException(int employeeId)
        : base($"L'employé {employeeId} est déjà pointé.", "ALREADY_CLOCKED_IN", 409)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when an employee has not clocked in as required.
/// </summary>
public class NotClockedInException : SgeException
{
    public NotClockedInException(int employeeId)
        : base($"L'employé {employeeId} n'a pas pointé à l'arrivée.", "NOT_CLOCKED_IN", 400)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when an attempt is made to create or register a department
/// with a name that already exists within the system.
/// </summary>
public class DuplicateDepartmentNameException : SgeException
{
    public DuplicateDepartmentNameException(string departmentName)
        : base($"Le nom du département '{departmentName}' existe déjà.", "DEPARTMENT_NAME_EXISTS", 409)
    {
    }
}

/// <summary>
/// Represents an exception that is thrown when invalid data is provided for an employee within the system.
/// </summary>
public class InvalidEmployeeDataException : SgeException
{
    public InvalidEmployeeDataException(string message)
        : base(message, "INVALID_EMPLOYEE_DATA", 400)
    {
    }
}
