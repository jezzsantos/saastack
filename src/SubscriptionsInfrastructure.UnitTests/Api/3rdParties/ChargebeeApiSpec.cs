using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using SubscriptionsApplication;
using SubscriptionsInfrastructure.Api._3rdParties;
using UnitTesting.Common;
using Xunit;

namespace SubscriptionsInfrastructure.UnitTests.Api._3rdParties;

[Trait("Category", "Unit")]
public class ChargebeeApiSpec
{
    private readonly ChargebeeApi _api;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IChargebeeApplication> _chargebeeApplication;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<IRecorder> _recorder;

    public ChargebeeApiSpec()
    {
        _recorder = new Mock<IRecorder>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContextAccessor.Setup(hca => hca.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request =
                {
                    IsHttps = true,
                    Headers =
                    {
                        [HttpConstants.Headers.Authorization] =
                            $"Basic {Convert.ToBase64String("ausername:"u8.ToArray())}"
                    }
                }
            });
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallId).Returns("acallid");
        var callerFactory = new Mock<ICallerContextFactory>();
        callerFactory.Setup(cf => cf.Create())
            .Returns(_caller.Object);
        _chargebeeApplication = new Mock<IChargebeeApplication>();
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(s =>
                s.Platform.GetString(ChargebeeStateInterpreter.Constants.WebhookUsernameSettingName,
                    It.IsAny<string>()))
            .Returns("ausername");
        settings.Setup(s =>
                s.Platform.GetString(ChargebeeStateInterpreter.Constants.WebhookPasswordSettingName,
                    It.IsAny<string>()))
            .Returns(string.Empty);

        _api = new ChargebeeApi(_recorder.Object, _httpContextAccessor.Object, callerFactory.Object, settings.Object,
            _chargebeeApplication.Object);
    }

    [Fact]
    public void WhenAuthenticateRequestAndNotHttps_ThenReturnsError()
    {
        _httpContextAccessor.Setup(hca => hca.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request = { IsHttps = false }
            });

        var result = ChargebeeApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            "ausername", "");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndMissingUsername_ThenReturnsError()
    {
        var result =
            ChargebeeApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object, "", "");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndMismatchedUsername_ThenReturnsError()
    {
        var result = ChargebeeApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            "anotheruser", "");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndMismatchedPassword_ThenReturnsError()
    {
        _httpContextAccessor.Setup(hca => hca.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request =
                {
                    IsHttps = true,
                    Headers =
                    {
                        [HttpConstants.Headers.Authorization] =
                            $"Basic {Convert.ToBase64String("ausername:apassword"u8.ToArray())}"
                    }
                }
            });

        var result = ChargebeeApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            "ausername", "anotherpassword");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndMissingPassword_ThenReturnsError()
    {
        _httpContextAccessor.Setup(hca => hca.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request =
                {
                    IsHttps = true,
                    Headers =
                    {
                        [HttpConstants.Headers.Authorization] =
                            $"Basic {Convert.ToBase64String("ausername:apassword"u8.ToArray())}"
                    }
                }
            });

        var result = ChargebeeApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            "ausername", "");

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }

    [Fact]
    public void WhenAuthenticateRequestAndPasswordNotRequiredButProvided_ThenAuthenticates()
    {
        _httpContextAccessor.Setup(hca => hca.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request =
                {
                    IsHttps = true,
                    Headers =
                    {
                        [HttpConstants.Headers.Authorization] =
                            $"Basic {Convert.ToBase64String("ausername:"u8.ToArray())}"
                    }
                }
            });

        var result = ChargebeeApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            "ausername", "apassword");

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenAuthenticateRequestAndPasswordNotRequiredAndNotProvided_ThenAuthenticates()
    {
        _httpContextAccessor.Setup(hca => hca.HttpContext)
            .Returns(new DefaultHttpContext
            {
                Request =
                {
                    IsHttps = true,
                    Headers =
                    {
                        [HttpConstants.Headers.Authorization] =
                            $"Basic {Convert.ToBase64String("ausername:"u8.ToArray())}"
                    }
                }
            });

        var result = ChargebeeApi.AuthenticateRequest(_recorder.Object, _caller.Object, _httpContextAccessor.Object,
            "ausername", "");

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenNotifyWebhookEventAndAnyEvent_ThenNotifies()
    {
        var content = new ChargebeeEventContent
        {
            Customer = new ChargebeeEventCustomer
            {
                Id = "acustomerid",
                PaymentMethod = new ChargebeePaymentMethod
                {
                    Id = "apaymentmethodid",
                    Status = "apaymentstatus",
                    Type = "apaymenttype"
                }
            }
        };
        var result = await _api.NotifyWebhookEvent(new ChargebeeNotifyWebhookEventRequest
        {
            Id = "aneventid",
            EventType = ChargebeeEventType.CustomerChanged.ToString(),
            Content = content
        }, CancellationToken.None);

        result().Value.Should().BeOfType<EmptyResponse>();
        _chargebeeApplication.Verify(app => app.NotifyWebhookEvent(It.Is<ICallerContext>(cc => cc.CallId == "acallid"),
            "aneventid", ChargebeeEventType.CustomerChanged.ToString(), It.Is<ChargebeeEventContent>(c =>
                c == content
            ), It.IsAny<CancellationToken>()));
    }
}