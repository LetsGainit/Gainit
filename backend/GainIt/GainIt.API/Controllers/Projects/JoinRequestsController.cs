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
            r_logger.LogInformation("Creating join request: ProjectId={ProjectId}, Role={Role}", projectId, dto.RequestedRole);

            if (projectId == Guid.Empty)
                return BadRequest(new { Message = "Project ID cannot be empty." });

            try
            {
                var userId = GetUserId();
                var req = await r_JoinRequestService.CreateJoinRequestAsync(projectId, userId, dto.RequestedRole, dto.Message);

                r_logger.LogInformation("Join request created: JoinRequestId={JoinRequestId}", req.JoinRequestId);
                return CreatedAtAction(nameof(GetJoinRequestById), new { projectId, joinRequestId = req.JoinRequestId }, req);
            }
            catch (KeyNotFoundException ex)
            {
                r_logger.LogWarning(ex, "Project or user not found.");
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                r_logger.LogWarning(ex, "Invalid operation while creating join request.");
                return BadRequest(new { Message = ex.Message });
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
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                var joinRequest = await r_JoinRequestService.GetJoinRequestByIdAsync(projectId, joinRequestId, userId);
                return Ok(new JoinRequestViewModel(joinRequest));
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error getting join request {JoinRequestId} for project {ProjectId}", joinRequestId, projectId);
                return BadRequest(new { ex.Message });
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
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                var joinRequests = await r_JoinRequestService.GetJoinRequestsForProjectAsync(projectId, userId, status);
                return Ok(joinRequests.Select(j => new JoinRequestViewModel(j)));
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error getting join requests for project {ProjectId}", projectId);
                return BadRequest(new { ex.Message });
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
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                var joinRequest = await r_JoinRequestService.CancelJoinRequestAsync(
                    projectId, joinRequestId, userId, dto.Reason);

                return Ok(new JoinRequestViewModel(joinRequest));
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error cancelling join request {JoinRequestId} for project {ProjectId}", joinRequestId, projectId);
                return BadRequest(new { ex.Message });
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
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                var joinRequest = await r_JoinRequestService.JoinRequestDecisionAsync(
                    projectId, joinRequestId, userId, dto.IsApproved, dto.Reason);

                return Ok(new JoinRequestViewModel(joinRequest));
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error deciding join request {JoinRequestId} for project {ProjectId}", joinRequestId, projectId);
                return BadRequest(new { ex.Message });
            }
        }
    }
}
