using BPS.Web.Models.Queue;
using BPS.Web.Services.Queue;
using Microsoft.AspNetCore.Mvc;

namespace BPS.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public sealed class QueueController : ControllerBase
{
    private readonly IQueueStore _queueStore;

    public QueueController(IQueueStore queueStore)
    {
        _queueStore = queueStore;
    }

    [HttpPost("submit")]
    public ActionResult<JobDto> Submit([FromBody] SubmitJobRequest request)
    {
        var created = _queueStore.Submit(request ?? new SubmitJobRequest());
        return Ok(created);
    }

    [HttpGet("list")]
    public ActionResult<IReadOnlyCollection<JobDto>> ListAll()
    {
        return Ok(_queueStore.List(null));
    }

    [HttpGet("list/{priority}")]
    public ActionResult<IReadOnlyCollection<JobDto>> ListByPriority([FromRoute] JobPriority priority)
    {
        return Ok(_queueStore.List(priority));
    }

    [HttpGet("status/{jobId}")]
    public ActionResult<JobDto> GetStatus([FromRoute] string jobId)
    {
        var job = _queueStore.Get(jobId);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(job);
    }

    [HttpPost("status/{jobId}/{jobAction}")]
    public IActionResult Control([FromRoute] string jobId, [FromRoute] string jobAction)
    {
        JobStatus targetStatus;
        switch (jobAction.ToLowerInvariant())
        {
            case "start":
            case "resume":
                targetStatus = JobStatus.Queued;
                break;
            case "stop":
                targetStatus = JobStatus.Stopped;
                break;
            default:
                return BadRequest(new { error = "Action must be start, stop, or resume." });
        }

        var updated = _queueStore.TryUpdateJobStatus(jobId, targetStatus);
        if (!updated)
        {
            return Conflict(new { error = "Invalid state transition or job not found." });
        }

        return Ok();
    }

    [HttpGet("configuration")]
    public ActionResult<QueueConfiguration> GetConfiguration()
    {
        return Ok(_queueStore.GetConfiguration());
    }

    [HttpPost("configuration")]
    public IActionResult SetConfiguration([FromBody] QueueConfiguration configuration)
    {
        var updated = _queueStore.TryUpdateConfiguration(configuration);
        if (!updated)
        {
            return BadRequest(new { error = "Invalid configuration values." });
        }

        return Ok(_queueStore.GetConfiguration());
    }

    [HttpPost("worker/claim")]
    public IActionResult Claim()
    {
        var claimed = _queueStore.TryClaimNext();
        if (claimed is null)
        {
            return NoContent();
        }

        return Ok(claimed);
    }

    [HttpPost("worker/{jobId}/complete")]
    public IActionResult Complete([FromRoute] string jobId, [FromBody] WorkerCompleteRequest request)
    {
        var updated = _queueStore.TryComplete(jobId, request.Status, request.Error);
        if (!updated)
        {
            return Conflict(new { error = "Job is not in a completable state." });
        }

        return Ok();
    }
}
