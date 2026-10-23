# PR description: dotnet/yarp#275 phase 1 (draft)

> Owner: desktop drafted 2026-10-22; the CLI checks it in pre-PR check 9 and fills the `<…>` slots from **its own
> run's** evidence. Timothy edits it into his words before pasting. Everything below the line is what he pastes.
> The optional paragraph is his call (lecture 001 §8): keep it or delete it.

**Title:** `Remove Autofac from tests`

---

Contributes to #275 (first step, as proposed in https://github.com/dotnet/yarp/issues/275#issuecomment-6073544881).

The seven test classes that derived from `TestAutoMockBase` now construct the class under test through its
constructor, passing Moq mocks (and `NullLogger<T>.Instance`) explicitly. `TestAutoMockBase` is deleted, along with
the `Autofac` and `Autofac.Extras.Moq` package references and their versions in `eng/Versions.props`. No product
code changes. Moq stays; removing it could follow separately, folder by folder, if that's still wanted.

Notes for review:

- `HttpSysDelegator` is `internal sealed`, so AutoMock was creating a real instance rather than a mock. The
  middleware tests now construct it explicitly with a mocked `IServer`, so they exercise the same code as before.
- AutoMock's mocks are loose with `DefaultValue.Empty`, the same as `new Mock<T>()`, so mock behavior is unchanged.
- *(Optional)* `ForwarderMiddlewareTests.NoDestinations_503` calls `Verify()` without any setup marked
  `Verifiable()`, so that line checks nothing. I left it as is to keep this PR a pure refactor; happy to fix it here
  or separately.

Verification (macOS 26.3 on Apple M2, SDK `11.0.100-rc.1.26420.103`):

- `./build.sh --configuration Release --rebuild --test`: results identical to `main` for all four test projects
  on both target frameworks, 0 warnings.
- `Yarp.ReverseProxy.Tests`: 1,989 passed per target framework (net8.0 and net9.0), before and after.
- For each rewritten class, I temporarily broke the code under test and confirmed its tests fail.
