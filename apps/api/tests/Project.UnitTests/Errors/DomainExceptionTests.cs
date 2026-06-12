using Project.Domain.Errors;

namespace Project.UnitTests.Errors;

public class DomainExceptionTests
{
    [Fact]
    public void DomainException_IsAbstract()
    {
        Assert.True(typeof(DomainException).IsAbstract);
    }

    [Fact]
    public void DomainException_InheritsFromException()
    {
        Assert.True(typeof(Exception).IsAssignableFrom(typeof(DomainException)));
    }

    [Fact]
    public void DomainException_CanBeInstantiatedViaDerivative()
    {
        var ex = new LastSuperadminGuardException("test");
        Assert.IsType<LastSuperadminGuardException>(ex);
        Assert.IsAssignableFrom<DomainException>(ex);
    }

    [Fact]
    public void DomainException_PreservesMessage()
    {
        var message = "Only one active Superadmin must remain.";
        var ex = new LastSuperadminGuardException(message);
        Assert.Equal(message, ex.Message);
    }
}

public class LastSuperadminGuardExceptionTests
{
    [Fact]
    public void Instantiation_WithMessage_SetsMessage()
    {
        var message = "Cannot remove the last active Superadmin.";
        var ex = new LastSuperadminGuardException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Inheritance_IsDomainException()
    {
        var ex = new LastSuperadminGuardException("test");
        Assert.IsAssignableFrom<DomainException>(ex);
    }

    [Fact]
    public void Inheritance_IsException()
    {
        var ex = new LastSuperadminGuardException("test");
        Assert.IsAssignableFrom<Exception>(ex);
    }
}

public class SystemRoleProtectedExceptionTests
{
    [Fact]
    public void Instantiation_WithMessage_SetsMessage()
    {
        var message = "System role 'superadmin' cannot be deleted.";
        var ex = new SystemRoleProtectedException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Inheritance_IsDomainException()
    {
        var ex = new SystemRoleProtectedException("test");
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

public class MenuCycleDetectedExceptionTests
{
    [Fact]
    public void Instantiation_WithMessage_SetsMessage()
    {
        var message = "Menu hierarchy cycle detected: child cannot be ancestor of parent.";
        var ex = new MenuCycleDetectedException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Inheritance_IsDomainException()
    {
        var ex = new MenuCycleDetectedException("test");
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

public class RefreshTokenReuseSignalExceptionTests
{
    [Fact]
    public void Instantiation_WithMessage_SetsMessage()
    {
        var message = "Refresh token reuse detected. Token family revoked.";
        var ex = new RefreshTokenReuseSignalException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Inheritance_IsDomainException()
    {
        var ex = new RefreshTokenReuseSignalException("test");
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

public class InvalidEmailExceptionTests
{
    [Fact]
    public void Instantiation_WithMessage_SetsMessage()
    {
        var message = "Email 'not-an-email' is not valid.";
        var ex = new InvalidEmailException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Inheritance_IsDomainException()
    {
        var ex = new InvalidEmailException("test");
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

public class InvalidPermissionKeyExceptionTests
{
    [Fact]
    public void Instantiation_WithMessage_SetsMessage()
    {
        var message = "Permission key 'Users.Create' is not valid.";
        var ex = new InvalidPermissionKeyException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Inheritance_IsDomainException()
    {
        var ex = new InvalidPermissionKeyException("test");
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}

public class DeletionPolicyViolationExceptionTests
{
    [Fact]
    public void Instantiation_WithMessage_SetsMessage()
    {
        var message = "Hard delete is not allowed for this entity.";
        var ex = new DeletionPolicyViolationException(message);
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Inheritance_IsDomainException()
    {
        var ex = new DeletionPolicyViolationException("test");
        Assert.IsAssignableFrom<DomainException>(ex);
    }
}
