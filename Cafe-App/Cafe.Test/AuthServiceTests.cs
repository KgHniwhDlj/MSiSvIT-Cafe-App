using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using Cafe_App.Services;
using Enums.Users;

namespace Cafe.Test;

[TestFixture]
public class AuthServiceTests
{
    private AuthService CreateService(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, AuthService.AUTH_TYPE_KEY);
        var user = new ClaimsPrincipal(identity);

        var context = new DefaultHttpContext { User = user };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(context);

        return new AuthService(accessorMock.Object);
    }

    [Test]
    public void IsAuthenticated_ReturnsFalse_WhenNoIdClaim()
    {
        var service = CreateService();
        Assert.False(service.IsAuthenticated());
    }

    [Test]
    public void IsAuthenticated_ReturnsTrue_WhenIdClaimExists()
    {
        var service = CreateService(new Claim(AuthService.CLAIM_TYPE_ID, "123"));
        Assert.True(service.IsAuthenticated());
    }

    [Test]
    public void GetName_ReturnsGuest_WhenNoNameClaim()
    {
        var service = CreateService();
        Assert.AreEqual("Гость", service.GetName());
    }

    [TestCase("User")]
    [TestCase("Иван")]
    [TestCase("AdminUser")]
    public void GetName_ReturnsCorrectName(string name)
    {
        var service = CreateService(new Claim(AuthService.CLAIM_TYPE_NAME, name));
        Assert.AreEqual(name, service.GetName());
    }

    [Test]
    public void GetUserId_ReturnsNull_WhenNoIdClaim()
    {
        var service = CreateService();
        Assert.IsNull(service.GetUserId());
    }

    [TestCase("42", 42)]
    [TestCase("100", 100)]
    public void GetUserId_ReturnsInt_WhenIdClaimExists(string idStr, int expected)
    {
        var service = CreateService(new Claim(AuthService.CLAIM_TYPE_ID, idStr));
        Assert.AreEqual(expected, service.GetUserId());
    }

    [Test]
    public void GetUserId_ThrowsFormatException_WhenIdIsNotInt()
    {
        var service = CreateService(new Claim(AuthService.CLAIM_TYPE_ID, "abc"));
        Assert.Throws<FormatException>(() => service.GetUserId());
    }

    [Test]
    public void GetRole_ThrowsException_WhenNoRoleClaim()
    {
        var service = CreateService(new Claim(AuthService.CLAIM_TYPE_ID, "1"));
        Assert.Throws<Exception>(() => service.GetRole());
    }

    [TestCase((int)Roles.Admin, Roles.Admin)]
    [TestCase((int)(Roles.User), Roles.User)]
    public void GetRole_ReturnsCorrectRole(int roleInt, Roles expectedRole)
    {
        var service = CreateService(
            new Claim(AuthService.CLAIM_TYPE_ID, "1"),
            new Claim(AuthService.CLAIM_TYPE_ROLE, roleInt.ToString())
        );
        Assert.AreEqual(expectedRole, service.GetRole());
    }

    [Test]
    public void GetRole_ThrowsFormatException_WhenRoleIsNotInt()
    {
        var service = CreateService(
            new Claim(AuthService.CLAIM_TYPE_ID, "1"),
            new Claim(AuthService.CLAIM_TYPE_ROLE, "abc")
        );
        Assert.Throws<FormatException>(() => service.GetRole());
    }

    [Test]
    public void IsAdmin_ReturnsTrue_WhenUserIsAdmin()
    {
        var service = CreateService(
            new Claim(AuthService.CLAIM_TYPE_ID, "1"),
            new Claim(AuthService.CLAIM_TYPE_ROLE, ((int)Roles.Admin).ToString())
        );
        Assert.True(service.IsAdmin());
    }

    [Test]
    public void IsAdmin_ReturnsFalse_WhenUserIsNotAdmin()
    {
        var service = CreateService(
            new Claim(AuthService.CLAIM_TYPE_ID, "1"),
            new Claim(AuthService.CLAIM_TYPE_ROLE, ((int)Roles.User).ToString())
        );
        Assert.False(service.IsAdmin());
    }

    [TestCase(Roles.User, true)]
    [TestCase(Roles.Admin, false)]
    public void HasRole_ReturnsCorrectValue(Roles role, bool expected)
    {
        var service = CreateService(
            new Claim(AuthService.CLAIM_TYPE_ID, "1"),
            new Claim(AuthService.CLAIM_TYPE_ROLE, ((int)Roles.User).ToString())
        );
        Assert.AreEqual(expected, service.HasRole(role));
    }
}
