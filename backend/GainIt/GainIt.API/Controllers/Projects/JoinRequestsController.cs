using GainIt.API.DTOs.Requests.Projects;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Services.Projects.Implementations;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GainIt.API.Controllers.Projects
{
    [ApiController]
    [Route("api/projects/{projectId:guid}")]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class JoinRequestsController : ControllerBase
    {
        private readonly ILogger<JoinRequestsController> r_logger;
        private readonly IJoinRequestService r_JoinRequestService;
        public JoinRequestsController(ILogger<JoinRequestsController> i_Logger, IJoinRequestService i_JoinRequest)
        {
            r_logger = i_Logger;
            r_JoinRequestService = i_JoinRequest;
        }

        private Guid GetUserId()
        {
            var externalId = User.FindFirst("oid")?.Value
                  ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(externalId))
                throw new UnauthorizedAccessException("User ID not found in token.");

            return Guid.Parse(externalId);
        }

        /// <summary>
        /// Create a new join request for current user.
        /// </summary>
        [HttpPost("createrequest")]
        public async Task<ActionResult<JoinRequest>> CreateJoinRequest(Guid projectId, [FromBody] JoinRequestCreateDto dto)
        {
            r_logger.LogInformation("Creating join request: ProjectId={ProjectId}, Role={Role}, HasMessage={HasMessage}", 
                projectId, dto.RequestedRole, !string.IsNullOrEmpty(dto.Message));

            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                var userId = GetUserId();
                r_logger.LogInformation("User authenticated: UserId={UserId}, ProjectId={ProjectId}", userId, projectId);

                var req = await r_JoinRequestService.CreateJoinRequestAsync(projectId, userId, dto.RequestedRole, dto.Message);

                r_logger.LogInformation("Join request created successfully: JoinRequestId={JoinRequestId}, ProjectId={ProjectId}, UserId={UserId}, Role={Role}", 
                    req.JoinRequestId, projectId, userId, dto.RequestedRole);
                
                return CreatedAtAction(nameof(GetJoinRequestById), new { projectId, joinRequestId = req.JoinRequestId }, req);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning(ex, "Project or user not found: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                r_logger.LogWarning(ex, "Invalid operation while creating join request: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Unexpected error creating join request: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while creating the join request." });
            }
        }

        /// <summary>
        /// Get a single join request by ID (admin or requester).
        /// </summary>
        [HttpGet("{joinRequestId:guid}")]
        public async Task<ActionResult<JoinRequestViewModel>> GetJoinRequestById(
            Guid projectId,
            Guid joinRequestId)
        {
            r_logger.LogInformation("Getting join request by ID: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", projectId, joinRequestId);

            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (joinRequestId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid join request ID provided: JoinRequestId={JoinRequestId}", joinRequestId);
                return BadRequest(new { Message = "Join request ID cannot be empty." });
            }

            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    r_logger.LogWarning("User not authenticated: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", projectId, joinRequestId);
                    return Unauthorized();
                }

                r_logger.LogInformation("User authenticated: UserId={UserId}, ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", 
                    userId, projectId, joinRequestId);

                var joinRequest = await r_JoinRequestService.GetJoinRequestByIdAsync(projectId, joinRequestId, userId);
                
                r_logger.LogInformation("Join request retrieved successfully: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, UserId={UserId}", 
                    projectId, joinRequestId, userId);
                
                return Ok(new JoinRequestViewModel(joinRequest));
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning(ex, "Unauthorized access to join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning(ex, "Join request not found: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error getting join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the join request." });
            }
        }

        /// <summary>
        /// Get all join requests for a project (admin only).
        /// </summary>
        [HttpGet("myrequests")]
        public async Task<ActionResult<IEnumerable<JoinRequestViewModel>>> GetJoinRequests(
            Guid projectId,
            [FromQuery] eJoinRequestStatus? status = null)
        {
            r_logger.LogInformation("Getting join requests for project: ProjectId={ProjectId}, Status={Status}", 
                projectId, status?.ToString() ?? "All");

            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    r_logger.LogWarning("User not authenticated: ProjectId={ProjectId}", projectId);
                    return Unauthorized();
                }

                r_logger.LogInformation("User authenticated: UserId={UserId}, ProjectId={ProjectId}", userId, projectId);

                var joinRequests = await r_JoinRequestService.GetJoinRequestsForProjectAsync(projectId, userId, status);
                
                r_logger.LogInformation("Join requests retrieved successfully: ProjectId={ProjectId}, UserId={UserId}, Count={Count}", 
                    projectId, userId, joinRequests.Count);
                
                return Ok(joinRequests.Select(j => new JoinRequestViewModel(j)));
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning(ex, "Unauthorized access to join requests: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error getting join requests: ProjectId={ProjectId}, Error={Error}", projectId, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while retrieving the join requests." });
            }
        }

        /// <summary>
        /// Cancel a join request (requester only).
        /// </summary>
        [HttpPost("{joinRequestId:guid}/cancel")]
        public async Task<ActionResult<JoinRequestViewModel>> CancelJoinRequest(
            Guid projectId,
            Guid joinRequestId,
            [FromBody] JoinRequestCancelDto dto)
        {
            r_logger.LogInformation("Cancelling join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, HasReason={HasReason}", 
                projectId, joinRequestId, !string.IsNullOrEmpty(dto.Reason));

            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (joinRequestId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid join request ID provided: JoinRequestId={JoinRequestId}", joinRequestId);
                return BadRequest(new { Message = "Join request ID cannot be empty." });
            }

            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    r_logger.LogWarning("User not authenticated: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", projectId, joinRequestId);
                    return Unauthorized();
                }

                r_logger.LogInformation("User authenticated: UserId={UserId}, ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", 
                    userId, projectId, joinRequestId);

                var joinRequest = await r_JoinRequestService.CancelJoinRequestAsync(
                    projectId, joinRequestId, userId, dto.Reason);

                r_logger.LogInformation("Join request cancelled successfully: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, UserId={UserId}", 
                    projectId, joinRequestId, userId);

                return Ok(new JoinRequestViewModel(joinRequest));
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning(ex, "Unauthorized cancellation attempt: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning(ex, "Join request not found for cancellation: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                r_logger.LogWarning(ex, "Invalid operation cancelling join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error cancelling join request: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while cancelling the join request." });
            }
        }

        /// <summary>
        /// Decide on a join request (approve/reject).
        /// </summary>
        [HttpPost("{joinRequestId:guid}/decision")]
        public async Task<ActionResult<JoinRequestViewModel>> DecideJoinRequest(
            Guid projectId,
            Guid joinRequestId,
            [FromBody] JoinRequestDecisionDto dto)
        {
            r_logger.LogInformation("Processing join request decision: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, IsApproved={IsApproved}, HasReason={HasReason}", 
                projectId, joinRequestId, dto.IsApproved, !string.IsNullOrEmpty(dto.Reason));

            if (projectId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid project ID provided: ProjectId={ProjectId}", projectId);
                return BadRequest(new { Message = "Project ID cannot be empty." });
            }

            if (joinRequestId == Guid.Empty)
            {
                r_logger.LogWarning("Invalid join request ID provided: JoinRequestId={JoinRequestId}", joinRequestId);
                return BadRequest(new { Message = "Join request ID cannot be empty." });
            }

            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    r_logger.LogWarning("User not authenticated: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}", projectId, joinRequestId);
                    return Unauthorized();
                }

                r_logger.LogInformation("User authenticated: UserId={UserId}, ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, IsApproved={IsApproved}", 
                    userId, projectId, joinRequestId, dto.IsApproved);

                var joinRequest = await r_JoinRequestService.JoinRequestDecisionAsync(
                    projectId, joinRequestId, userId, dto.IsApproved, dto.Reason);

                r_logger.LogInformation("Join request decision processed successfully: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, UserId={UserId}, IsApproved={IsApproved}", 
                    projectId, joinRequestId, userId, dto.IsApproved);

                return Ok(new JoinRequestViewModel(joinRequest));
            }
            catch (UnauthorizedAccessException ex)
            {
                r_logger.LogWarning(ex, "Unauthorized decision attempt: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return Unauthorized(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning(ex, "Join request not found for decision: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                r_logger.LogWarning(ex, "Invalid operation processing join request decision: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error processing join request decision: ProjectId={ProjectId}, JoinRequestId={JoinRequestId}, Error={Error}", 
                    projectId, joinRequestId, ex.Message);
                return StatusCode(500, new { Message = "An unexpected error occurred while processing the join request decision." });
            }
        }
    }
}
