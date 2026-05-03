<!-- markdownlint-disable-file -->
# Release Changes: Search Functionality Rework (Review IV-001, IV-002)

**Related Plan**: search-functionality-plan.instructions.md
**Implementation Date**: 2026-05-03

## Summary

Address the two Major findings from the search-functionality review: add error handling (502) on the `SearchBabbles` endpoint for AI service failures, and add the missing test coverage for that failure path.

## Changes

### Added

_None_

### Modified

* prompt-babbler-service/src/Api/Controllers/BabbleController.cs - Wrapped `SearchAsync` call in try/catch returning 502 ProblemDetails on AI service failure (IV-001)
* prompt-babbler-service/tests/unit/Infrastructure.UnitTests/Services/BabbleServiceTests.cs - Added `SearchAsync_EmbeddingServiceFails_PropagatesException` test (IV-002)
* prompt-babbler-service/tests/unit/Api.UnitTests/Controllers/BabbleControllerTests.cs - Added `SearchBabbles_ServiceThrows_Returns502` test (IV-002)

### Removed

_None_

## Additional or Deviating Changes

_None_

## Release Summary

Total files affected: 3 (0 created, 3 modified, 0 removed)

**API Layer:**
- `BabbleController.cs` — `SearchBabbles` endpoint wrapped in try/catch; AI service failures now return 502 with ProblemDetails (matches GenerateTitle/UploadAudio pattern)

**Tests:**
- `BabbleServiceTests.cs` — `SearchAsync_EmbeddingServiceFails_PropagatesException` added; verifies exception propagates from service layer
- `BabbleControllerTests.cs` — `SearchBabbles_ServiceThrows_Returns502` added; verifies controller returns 502 on AI service failure

**Validation:**
- `dotnet test --filter TestCategory=Unit` — 230/230 passed (228 original + 2 new), 0 failed
