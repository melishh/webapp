using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.LeaveRequests;
using SGE.Application.Interfaces.Services;
using SGE.Core.Enums;

namespace SGE.API.Controllers;

/// <summary>
/// Controller for managing employee leave requests.
/// Provides endpoints for creating, retrieving, updating, and managing leave requests.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController(ILeaveRequestService leaveRequestService) : ControllerBase
{
    /// <summary>
    /// Creates a new leave request.
    /// </summary>
    /// <param name="dto">The data transfer object containing leave request details.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the created leave request details.</returns>
    [HttpPost]
    [ProducesResponseType(201, Type = typeof(LeaveRequestDto))]
    [ProducesResponseType(400)]
    public async Task<ActionResult<LeaveRequestDto>> Create([FromBody] LeaveRequestCreateDto dto, CancellationToken cancellationToken)
    {
        var leaveRequest = await leaveRequestService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = leaveRequest.Id }, leaveRequest);
    }

    /// <summary>
    /// Retrieves all leave requests.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a collection of all leave requests.</returns>
    [HttpGet]
    [ProducesResponseType(200, Type = typeof(IEnumerable<LeaveRequestDto>))]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetAll(CancellationToken cancellationToken)
    {
        var leaveRequests = await leaveRequestService.GetAllAsync(cancellationToken);
        return Ok(leaveRequests);
    }

    /// <summary>
    /// Retrieves a leave request by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the leave request to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the leave request details.</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(200, Type = typeof(LeaveRequestDto))]
    [ProducesResponseType(404)]
    public async Task<ActionResult<LeaveRequestDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var leaveRequest = await leaveRequestService.GetByIdAsync(id, cancellationToken);
        return Ok(leaveRequest);
    }

    /// <summary>
    /// Retrieves all leave requests for a specific employee.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a collection of leave requests for the specified employee.</returns>
    [HttpGet("employee/{employeeId:int}")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<LeaveRequestDto>))]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetByEmployee(int employeeId, CancellationToken cancellationToken)
    {
        var leaveRequests = await leaveRequestService.GetLeaveRequestsByEmployeeAsync(employeeId, cancellationToken);
        return Ok(leaveRequests);
    }

    /// <summary>
    /// Retrieves leave requests filtered by status.
    /// </summary>
    /// <param name="status">The status to filter by (Pending, Approved, Rejected, Cancelled).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a collection of leave requests with the specified status.</returns>
    [HttpGet("status/{status}")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<LeaveRequestDto>))]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetByStatus(LeaveStatus status, CancellationToken cancellationToken)
    {
        var leaveRequests = await leaveRequestService.GetLeaveRequestsByStatusAsync(status, cancellationToken);
        return Ok(leaveRequests);
    }

    /// <summary>
    /// Retrieves all pending leave requests.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns a collection of pending leave requests.</returns>
    [HttpGet("pending")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<LeaveRequestDto>))]
    public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetPending(CancellationToken cancellationToken)
    {
        var leaveRequests = await leaveRequestService.GetPendingLeaveRequestsAsync();
        return Ok(leaveRequests);
    }

    /// <summary>
    /// Updates the status of a leave request.
    /// </summary>
    /// <param name="id">The unique identifier of the leave request to update.</param>
    /// <param name="dto">The data transfer object containing updated status and manager comments.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns no content if successful.</returns>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] LeaveRequestUpdateDto dto, CancellationToken cancellationToken)
    {
        await leaveRequestService.UpdateStatusAsync(id, dto, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Retrieves the remaining leave days for an employee in a specific year.
    /// </summary>
    /// <param name="employeeId">The unique identifier of the employee.</param>
    /// <param name="year">The year for which to calculate remaining leave days.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Returns the number of remaining leave days.</returns>
    [HttpGet("employee/{employeeId:int}/remaining/{year:int}")]
    [ProducesResponseType(200, Type = typeof(int))]
    public async Task<ActionResult<int>> GetRemainingLeaveDays(int employeeId, int year, CancellationToken cancellationToken)
    {
        var remainingDays = await leaveRequestService.GetRemainingLeaveDaysAsync(employeeId, year, cancellationToken);
        return Ok(remainingDays);
    }
}
