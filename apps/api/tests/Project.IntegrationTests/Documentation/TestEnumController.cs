extern alias ApiControllers;

using Microsoft.AspNetCore.Mvc;

namespace Project.IntegrationTests.Documentation;

/// <summary>
/// Test-only controller that exposes an endpoint returning an enum-typed DTO.
/// Used by <see cref="ApiContractEvidenceTests"/> to verify enum serialization.
/// </summary>
[ApiController]
[Route("test-enum")]
public sealed class TestEnumController : ControllerBase
{
    [HttpGet("sample")]
    public IActionResult GetSample()
    {
        return Ok(new EnumSampleDto(TestEnum.Active));
    }
}

public enum TestEnum
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}

public sealed record EnumSampleDto(TestEnum Status);
