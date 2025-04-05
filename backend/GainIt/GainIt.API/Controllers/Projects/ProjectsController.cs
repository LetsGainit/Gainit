using GainIt.API.Services.Projects.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GainIt.API.Controllers.Projects
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService r_ProjectService;

        public ProjectsController(IProjectService i_ProjectService)
        {
            r_ProjectService = i_ProjectService;
        }
    }
