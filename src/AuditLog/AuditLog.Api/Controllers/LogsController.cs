using AuditLog.Api.Data;
using AuditLog.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace AuditLog.Api.Controllers;

[ApiController]
[Route("logs")]
public class LogsController(AuditLogRepository repository) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<AuditLogEntry>> Get([FromQuery] LogQuery query)
        => Ok(repository.Find(query));
}
