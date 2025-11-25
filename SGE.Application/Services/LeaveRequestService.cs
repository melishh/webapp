using AutoMapper;
using SGE.Application.DTOs.LeaveRequests;
using SGE.Application.Interfaces.Repositories;
using SGE.Application.Interfaces.Services;
using SGE.Core.Entities;
using SGE.Core.Enums;
using SGE.Core.Exceptions;

namespace SGE.Application.Services;

public class LeaveRequestService(
   IEmployeeRepository employeeRepository,
   ILeaveRequestRepository leaveRequestRepository,
   IMapper mapper)
   : ILeaveRequestService
{
   /// <summary>
   /// Creates a new leave request in the system asynchronously.
   /// </summary>
   /// <param name="dto">
   /// The data required to create a new leave request, including employee ID, leave type, start date, end date, and reason.
   /// </param>
   /// <param name="cancellationToken">
   /// A token to monitor for cancellation requests.
   /// </param>
   /// <returns>
   /// The details of the created leave request wrapped in a LeaveRequestDto.
   /// </returns>
   public async Task<LeaveRequestDto> CreateAsync(LeaveRequestCreateDto dto,
       CancellationToken cancellationToken = default)
   {
       var employee = await employeeRepository.GetByIdAsync(dto.EmployeeId, cancellationToken);
       if (employee is null)
           throw new EmployeeNotFoundException(dto.EmployeeId);

       if (dto.EndDate < dto.StartDate)
           throw new ValidationException("EndDate", "La date de fin doit être supérieure à la date de début.");

       if (dto.StartDate < DateTime.Today)
           throw new ValidationException("StartDate", "La date de début doit être supérieure ou égale à la date de jour.");

       var daysRequested = CalculateBusinessDays(dto.StartDate, dto.EndDate);

       var hasConflict = await HasConflictingLeaveAsync(dto.EmployeeId, dto.StartDate, dto.EndDate, cancellationToken: cancellationToken);
       if (hasConflict)
           throw new ConflictingLeaveRequestException(dto.StartDate, dto.EndDate);

       var startDateUtc = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
       var endDateUtc = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);

       var entity = mapper.Map<LeaveRequest>(dto);
       entity.StartDate = startDateUtc;
       entity.EndDate = endDateUtc;
       entity.DaysRequested = daysRequested;
       entity.Status = LeaveStatus.Pending;
       entity.CreatedAt = DateTime.UtcNow;
       entity.UpdatedAt = DateTime.UtcNow;

       await leaveRequestRepository.AddAsync(entity, cancellationToken);
       return mapper.Map<LeaveRequestDto>(entity);
   }

   /// <summary>
   /// Retrieves the details of a leave request by its unique identifier asynchronously.
   /// </summary>
   /// <param name="id">
   /// The unique identifier of the leave request to be retrieved.
   /// </param>
   /// <param name="cancellationToken">
   /// A token to monitor for cancellation requests.
   /// </param>
   /// <returns>
   /// The details of the leave request wrapped in a LeaveRequestDto.
   /// </returns>
   /// <exception cref="LeaveRequestNotFoundException">Thrown when the leave request with the specified ID is not found.</exception>
   public async Task<LeaveRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
   {
       var leaveRequest = await leaveRequestRepository.GetByIdAsync(id, cancellationToken);
       if (leaveRequest == null)
           throw new LeaveRequestNotFoundException(id);

       return mapper.Map<LeaveRequestDto>(leaveRequest);
   }

   /// <summary>
   /// Retrieves the leave requests associated with a specific employee asynchronously.
   /// </summary>
   /// <param name="employeeId">
   /// The unique identifier of the employee whose leave requests are to be retrieved.
   /// </param>
   /// <param name="cancellationToken">
   /// A token to monitor for cancellation requests.
   /// </param>
   /// <returns>
   /// A collection of leave request details wrapped in LeaveRequestDto objects.
   /// </returns>
   public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByEmployeeAsync(int employeeId,
       CancellationToken cancellationToken = default)
   {
       var leaveRequests = await leaveRequestRepository.GetByEmployeeAsync(employeeId, cancellationToken);
       return mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
   }

   /// <summary>
   /// Retrieves a collection of leave requests based on the specified status asynchronously.
   /// </summary>
   /// <param name="status">
   /// The status of the leave requests to filter by, such as Pending, Approved, Rejected, or Cancelled.
   /// </param>
   /// <param name="cancellationToken">
   /// A token to monitor for cancellation requests.
   /// </param>
   /// <returns>
   /// A collection of leave requests matching the specified status, wrapped in LeaveRequestDto objects.
   /// </returns>
   public async Task<IEnumerable<LeaveRequestDto>> GetLeaveRequestsByStatusAsync(LeaveStatus status,
       CancellationToken cancellationToken = default)
   {
       var allLeaveRequests = await leaveRequestRepository.GetAllAsync(cancellationToken);
       var filteredLeaveRequests = allLeaveRequests.Where(lr => lr.Status == status);
       return mapper.Map<IEnumerable<LeaveRequestDto>>(filteredLeaveRequests);
   }

   /// <summary>
   /// Retrieves all leave requests with a status of pending asynchronously.
   /// </summary>
   /// <returns>
   /// A collection of leave requests that are currently pending, wrapped in LeaveRequestDto objects.
   /// </returns>
   public async Task<IEnumerable<LeaveRequestDto>> GetPendingLeaveRequestsAsync()
   {
       return await GetLeaveRequestsByStatusAsync(LeaveStatus.Pending);
   }

   /// <summary>
   /// Updates the status of an existing leave request asynchronously.
   /// </summary>
   /// <param name="id">
   /// The unique identifier of the leave request to be updated.
   /// </param>
   /// <param name="dto">
   /// An object containing the updated status and optional manager comments for the leave request.
   /// </param>
   /// <param name="cancellationToken">
   /// A token to monitor for cancellation requests.
   /// </param>
   /// <returns>
   /// A boolean value indicating whether the operation was successful.
   /// </returns>
   /// <exception cref="LeaveRequestNotFoundException">Thrown when the leave request with the specified ID is not found.</exception>
   public async Task<bool> UpdateStatusAsync(int id, LeaveRequestUpdateDto dto,
       CancellationToken cancellationToken = default)
   {
       var leaveRequest = await leaveRequestRepository.GetByIdAsync(id, cancellationToken);
       if (leaveRequest == null)
           throw new LeaveRequestNotFoundException(id);

       leaveRequest.Status = dto.Status;
       leaveRequest.ManagerComments = dto.ManagerComments;
       leaveRequest.UpdatedAt = DateTime.UtcNow;

       await leaveRequestRepository.UpdateAsync(leaveRequest, cancellationToken);
       return true;
   }

   /// <summary>
   /// Retrieves the remaining leave days for a specific employee in a given year asynchronously.
   /// </summary>
   /// <param name="employeeId">
   /// The unique identifier of the employee for whom the remaining leave days are being retrieved.
   /// </param>
   /// <param name="year">
   /// The year for which the remaining leave days are being calculated.
   /// </param>
   /// <param name="cancellationToken">
   /// A token to monitor for cancellation requests.
   /// </param>
   /// <returns>
   /// The total number of remaining leave days for the specified employee and year.
   /// </returns>
   public async Task<int> GetRemainingLeaveDaysAsync(int employeeId, int year,
       CancellationToken cancellationToken = default)
   {
       if (!await employeeRepository.ExistsAsync(employeeId, cancellationToken))
           throw new EmployeeNotFoundException(employeeId);

       // Supposons que chaque employé a droit à 25 jours de congés annuels
       const int annualLeaveDays = 25;

       var leaveRequests = await leaveRequestRepository.GetByEmployeeAsync(employeeId, cancellationToken);
       var approvedLeaves = leaveRequests
           .Where(lr => lr.Status == LeaveStatus.Approved &&
                       lr.StartDate.Year == year &&
                       lr.LeaveType == LeaveType.Annual)
           .Sum(lr => lr.DaysRequested);

       return annualLeaveDays - approvedLeaves;
   }

   /// <summary>
   /// Checks if there are any conflicting leave requests for an employee within the specified date range.
   /// </summary>
   /// <param name="employeeId">
   /// The ID of the employee for whom the check is being performed.
   /// </param>
   /// <param name="startDate">
   /// The start date of the leave period to verify for conflicts.
   /// </param>
   /// <param name="endDate">
   /// The end date of the leave period to verify for conflicts.
   /// </param>
   /// <param name="excludeRequestId">
   /// An optional leave request ID to exclude from the conflict check.
   /// </param>
   /// <param name="cancellationToken">
   /// A token to monitor for cancellation requests during the operation.
   /// </param>
   /// <returns>
   /// A boolean value indicating whether any conflicting leave requests exist.
   /// </returns>
   public async Task<bool> HasConflictingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate,
       int? excludeRequestId = null, CancellationToken cancellationToken = default)
   {
       var leaveRequests = await leaveRequestRepository.GetByEmployeeAsync(employeeId, cancellationToken);

       var conflictingLeaves = leaveRequests
           .Where(lr => lr.Status != LeaveStatus.Rejected &&
                       lr.Status != LeaveStatus.Cancelled &&
                       (excludeRequestId == null || lr.Id != excludeRequestId) &&
                       ((lr.StartDate <= endDate && lr.EndDate >= startDate)))
           .ToList();

       return conflictingLeaves.Any();
   }

   /// <summary>
   /// Retrieves all leave requests from the system asynchronously.
   /// </summary>
   /// <param name="cancellationToken">
   /// A token to monitor for cancellation requests.
   /// </param>
   /// <returns>
   /// A collection of all leave requests wrapped in LeaveRequestDto objects.
   /// </returns>
   public async Task<IEnumerable<LeaveRequestDto>> GetAllAsync(CancellationToken cancellationToken = default)
   {
       var leaveRequests = await leaveRequestRepository.GetAllAsync(cancellationToken);
       return mapper.Map<IEnumerable<LeaveRequestDto>>(leaveRequests);
   }

   private int CalculateBusinessDays(DateTime startDate, DateTime endDate)
   {
       int businessDays = 0;
       DateTime current = startDate;

       while (current <= endDate)
       {
           if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
               businessDays++;

           current = current.AddDays(1);
       }

       return businessDays;
   }

}
