using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[UsedImplicitly]
public class AuthorizeAttributeSpec
{
    [Trait("Category", "Unit")]
    public class GivenPlatformAuthorization
    {
        [Fact]
        public void WhenConstructedWithOnlyASingleRole_ThenHasAtLeastBasicPlatformFeatureAccess()
        {
            var result = new AuthorizeAttribute(Roles.Platform_Standard);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
            result.Features.Platform.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithASinglePlatformRoleAndDefaultFeature_ThenHasThatAccess()
        {
            var result = new AuthorizeAttribute(Roles.Platform_Standard, Features.Platform_Basic);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Platform.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithASinglePlatformRoleAndAnyFeature_ThenHasThatAccess()
        {
            var result = new AuthorizeAttribute(Roles.Platform_Standard, Features.Platform_PaidTrial);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(level => level == PlatformFeatures.PaidTrial);
            result.Features.Platform.Should().OnlyContain(level => level == PlatformFeatures.PaidTrial);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithMultipleRolesAndNoFeatures_ThenHasAtLeastBasicPlatformFeatureAccess()
        {
            var result = new AuthorizeAttribute(Roles.Platform_Standard | Roles.Platform_Operations);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Platform.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithASinglePlatformFeatureAndDefaultRole_ThenHasThatAccess()
        {
            var result = new AuthorizeAttribute(Features.Platform_Basic, Roles.Platform_Standard);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Platform.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithASinglePlatformFeatureAndAnyRole_ThenHasThatAccess()
        {
            var result = new AuthorizeAttribute(Features.Platform_PaidTrial, Roles.Platform_Operations);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(level => level == PlatformFeatures.PaidTrial);
            result.Features.Platform.Should().OnlyContain(level => level == PlatformFeatures.PaidTrial);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithMultipleFeaturesAndNoRoles_ThenHasAtLeastBasicPlatformRoleAccess()
        {
            var result = new AuthorizeAttribute(Features.Platform_Basic | Features.Platform_PaidTrial);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.PaidTrial);
            result.Features.Platform.Should().OnlyContain(feat => feat == PlatformFeatures.PaidTrial);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithARoleAndMultipleFeatures_ThenHasThatAccess()
        {
            var result =
                new AuthorizeAttribute(Roles.Platform_Operations,
                    Features.Platform_PaidTrial | Features.Platform_Paid2);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Paid2);
            result.Features.Platform.Should().OnlyContain(feat => feat == PlatformFeatures.Paid2);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithAFeatureAndMultipleRoles_ThenHasThatAccess()
        {
            var result = new AuthorizeAttribute(Features.Platform_PaidTrial, Roles.Platform_Standard |
                                                                             Roles.Platform_Operations);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.PaidTrial);
            result.Features.Platform.Should().OnlyContain(feat => feat == PlatformFeatures.PaidTrial);
            result.Features.Tenant.Should().BeEmpty();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenTenantAuthorization
    {
        [Fact]
        public void WhenConstructedWithOnlyASingleRole_ThenHasAtLeastBasicPlatformFeatureAccess()
        {
            var result = new AuthorizeAttribute(Roles.Tenant_Member);

            result.Roles.All.Should().OnlyContain(rol => rol == TenantRoles.Member);
            result.Roles.Platform.Should().BeEmpty();
            result.Roles.Tenant.Should().OnlyContain(rol => rol == TenantRoles.Member);
            result.Features.All.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Platform.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithOnlyMultipleRoles_ThenHasAtLeastBasicPlatformFeatureAccess()
        {
            var result = new AuthorizeAttribute(Roles.Tenant_Member | Roles.Tenant_Owner);

            result.Roles.All.Should().OnlyContain(rol => rol == TenantRoles.Owner);
            result.Roles.Platform.Should().BeEmpty();
            result.Roles.Tenant.Should().OnlyContain(rol => rol == TenantRoles.Owner);
            result.Features.All.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Platform.Should().OnlyContain(level => level == PlatformFeatures.Basic);
            result.Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenConstructedWithOnlyASingleFeature_ThenHasAtLeastBasicPlatformRoleAccess()
        {
            var result = new AuthorizeAttribute(Features.Tenant_PaidTrial);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(level => level == TenantFeatures.PaidTrial);
            result.Features.Platform.Should().BeEmpty();
            result.Features.Tenant.Should().OnlyContain(level => level == TenantFeatures.PaidTrial);
        }

        [Fact]
        public void WhenConstructedWithMultipleFeatures_ThenHasAtLeastBasicPlatformRoleAccess()
        {
            var result = new AuthorizeAttribute(Features.Tenant_PaidTrial | Features.Tenant_Paid2);

            result.Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result.Roles.Tenant.Should().BeEmpty();
            result.Features.All.Should().OnlyContain(feat => feat == TenantFeatures.Paid2);
            result.Features.Platform.Should().BeEmpty();
            result.Features.Tenant.Should().OnlyContain(feat => feat == TenantFeatures.Paid2);
        }

        [Fact]
        public void WhenConstructedWithARoleAndMultipleFeatures_ThenHasThatAccess()
        {
            var result = new AuthorizeAttribute(Roles.Tenant_Member, Features.Tenant_PaidTrial |
                                                                     Features.Tenant_Paid2);

            result.Roles.All.Should().OnlyContain(rol => rol == TenantRoles.Member);
            result.Roles.Platform.Should().BeEmpty();
            result.Roles.Tenant.Should().OnlyContain(rol => rol == TenantRoles.Member);
            result.Features.All.Should().OnlyContain(feat => feat == TenantFeatures.Paid2);
            result.Features.Platform.Should().BeEmpty();
            result.Features.Tenant.Should().OnlyContain(feat => feat == TenantFeatures.Paid2);
        }

        [Fact]
        public void WhenConstructedWithAFeatureAndMultipleRoles_ThenHasThatAccess()
        {
            var result = new AuthorizeAttribute(Features.Tenant_PaidTrial, Roles.Tenant_Owner |
                                                                           Roles.Tenant_Member);

            result.Roles.All.Should().OnlyContain(rol => rol == TenantRoles.Owner);
            result.Roles.Platform.Should().BeEmpty();
            result.Roles.Tenant.Should().OnlyContain(rol => rol == TenantRoles.Owner);
            result.Features.All.Should().OnlyContain(feat => feat == TenantFeatures.PaidTrial);
            result.Features.Platform.Should().BeEmpty();
            result.Features.Tenant.Should().OnlyContain(feat => feat == TenantFeatures.PaidTrial);
        }

        [Fact]
        public void WhenConstructedWithMixedRoles_ThenHasAtLeastBasicPlatformFeatureAccess()
        {
            var result = new AuthorizeAttribute(Roles.Tenant_Owner | Roles.Platform_Operations);

            result.Roles.All.Should().ContainInOrder(PlatformRoles.Operations, TenantRoles.Owner);
            result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result.Roles.Tenant.Should().OnlyContain(rol => rol == TenantRoles.Owner);
            result.Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
            result.Features.Platform.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
            result.Features.Tenant.Should().BeEmpty();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenRolesAndFeatures
    {
        [Fact]
        public void WhenCreatePolicyNameAndNoSets_ThenReturnsNonePolicyName()
        {
            var result = AuthorizeAttribute.CreatePolicyName(new List<IReadOnlyList<string>>());

            result.Should().Be(AuthenticationConstants.Authorization.RolesAndFeaturesPolicyNameForNone);
        }

        [Fact]
        public void WhenCreatePolicyNameAndOnlyEmptySets_ThenReturnsDefaultPolicyName()
        {
            var sets = new List<List<string>>
            {
                new()
            };

            var result = AuthorizeAttribute.CreatePolicyName(sets);

            result.Should()
                .Be(
                    $"POLICY:{{|Features|:{{|Platform|:[|basic_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}");
        }

        [Fact]
        public void WhenCreatePolicyNameAndSetContainsUnknownRolesOrFeatures_ThenReturnsDefaultPolicyName()
        {
            var sets = new List<List<string>>
            {
                new()
                {
                    string.Empty,
                    "anunknownrole"
                }
            };

            var result = AuthorizeAttribute.CreatePolicyName(sets);

            result.Should()
                .Be(
                    $"POLICY:{{|Features|:{{|Platform|:[|basic_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}");
        }

        [Fact]
        public void WhenCreatePolicyNameAndSetContainsOnlyRoleNoFeature_ThenReturnsUniquePolicyName()
        {
            var sets = new List<List<string>>
            {
                new()
                {
                    AuthorizeAttribute.FormatRoleName(Roles.Platform_Operations)
                }
            };

            var result = AuthorizeAttribute.CreatePolicyName(sets);

            result.Should()
                .Be(
                    $"POLICY:{{|Features|:{{|Platform|:[|basic_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Operations.Name}|]}}}}");
        }

        [Fact]
        public void WhenCreatePolicyNameAndSetContainsOnlyFeatureNoRole_ThenReturnsUniquePolicyName()
        {
            var sets = new List<List<string>>
            {
                new()
                {
                    AuthorizeAttribute.FormatFeatureName(Features.Platform_PaidTrial)
                }
            };

            var result = AuthorizeAttribute.CreatePolicyName(sets);

            result.Should()
                .Be(
                    $"POLICY:{{|Features|:{{|Platform|:[|paidtrial_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}");
        }

        [Fact]
        public void WhenCreatePolicyNameAndSetContainsRoleAndFeature_ThenReturnsUniquePolicyName()
        {
            var sets = new List<List<string>>
            {
                new()
                {
                    AuthorizeAttribute.FormatRoleName(Roles.Platform_Operations),
                    AuthorizeAttribute.FormatFeatureName(Features.Platform_PaidTrial)
                }
            };

            var result = AuthorizeAttribute.CreatePolicyName(sets);

            result.Should()
                .Be(
                    $"POLICY:{{|Features|:{{|Platform|:[|paidtrial_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Operations.Name}|]}}}}");
        }

        [Fact]
        public void WhenCreatePolicyNameAndTwoSetsContainingSameRolesAndFeatures_ThenReturnsUniquePolicyNames()
        {
            var sets = new List<List<string>>
            {
                new()
                {
                    AuthorizeAttribute.FormatRoleName(Roles.Platform_Operations),
                    AuthorizeAttribute.FormatFeatureName(Features.Platform_PaidTrial)
                },
                new()
                {
                    AuthorizeAttribute.FormatRoleName(Roles.Platform_Operations),
                    AuthorizeAttribute.FormatFeatureName(Features.Platform_PaidTrial)
                }
            };

            var result = AuthorizeAttribute.CreatePolicyName(sets);

            result.Should()
                .Be(
                    $"POLICY:{{|Features|:{{|Platform|:[|paidtrial_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Operations.Name}|]}}}}"
                    +
                    $"POLICY:{{|Features|:{{|Platform|:[|paidtrial_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Operations.Name}|]}}}}");
        }

        [Fact]
        public void WhenCreatePolicyNameAndTwoSetsContainingDifferentRolesAndFeatures_ThenReturnsUniquePolicyNames()
        {
            var sets = new List<List<string>>
            {
                new()
                {
                    AuthorizeAttribute.FormatRoleName(Roles.Platform_Standard),
                    AuthorizeAttribute.FormatFeatureName(Features.Tenant_Basic)
                },
                new()
                {
                    AuthorizeAttribute.FormatRoleName(Roles.Platform_Operations),
                    AuthorizeAttribute.FormatFeatureName(Features.Tenant_PaidTrial)
                }
            };

            var result = AuthorizeAttribute.CreatePolicyName(sets);

            result.Should()
                .Be(
                    $"POLICY:{{|Features|:{{|Tenant|:[|basic_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}"
                    +
                    $"POLICY:{{|Features|:{{|Tenant|:[|paidtrial_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Operations.Name}|]}}}}");
        }

        [Fact]
        public void WhenCreatePolicyNameAndTwoSetsContainingMultipleRolesAndFeatures_ThenReturnsUniquePolicyNames()
        {
            var sets = new List<List<string>>
            {
                new()
                {
                    AuthorizeAttribute.FormatRoleName(Roles.Platform_Operations),
                    AuthorizeAttribute.FormatRoleName(Roles.Platform_Standard),
                    AuthorizeAttribute.FormatFeatureName(Features.Tenant_PaidTrial),
                    AuthorizeAttribute.FormatFeatureName(Features.Tenant_Basic)
                }
            };

            var result = AuthorizeAttribute.CreatePolicyName(sets);

            result.Should()
                .Be(
                    $"POLICY:{{|Features|:{{|Tenant|:[|paidtrial_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Operations.Name}|]}}}}");
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAPolicy
    {
        [Fact]
        public void WhenParsePolicyNameAndEmpty_ThenReturnsEmpty()
        {
            var result = AuthorizeAttribute.ParsePolicyName(string.Empty);

            result.Should().BeEmpty();
        }

        [Fact]
        public void WhenParsePolicyNameAndHasNoneValue_ThenReturnsEmpty()
        {
            var result =
                AuthorizeAttribute.ParsePolicyName(AuthenticationConstants.Authorization
                    .RolesAndFeaturesPolicyNameForNone);

            result.Should().BeEmpty();
        }

        [Fact]
        public void WhenParsePolicyNameAndHasRole_ThenReturnsRole()
        {
            var result =
                AuthorizeAttribute.ParsePolicyName(
                    $"POLICY:{{|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}");

            result.Count.Should().Be(1);
            result[0].Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result[0].Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result[0].Roles.Tenant.Should().BeEmpty();
            result[0].Features.All.Should().BeEmpty();
        }

        [Fact]
        public void WhenParsePolicyNameAndHasRoleAndFeature_ThenReturnsRoleAndFeature()
        {
            var result = AuthorizeAttribute.ParsePolicyName(
                $"POLICY:{{|Features|:{{|Platform|:[|basic_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}");

            result.Count.Should().Be(1);
            result[0].Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result[0].Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result[0].Roles.Tenant.Should().BeEmpty();
            result[0].Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
            result[0].Features.Platform.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
            result[0].Features.Tenant.Should().BeEmpty();
        }

        [Fact]
        public void WhenParsePolicyNameAndHasMultiplePolicies_ThenReturnsRolesAndFeatures()
        {
            var result = AuthorizeAttribute.ParsePolicyName(
                $"POLICY:{{|Features|:{{|Tenant|:[|basic_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Standard.Name}|]}}}}"
                +
                $"POLICY:{{|Features|:{{|Tenant|:[|paidtrial_features|]}},|Roles|:{{|Platform|:[|{PlatformRoles.Operations.Name}|]}}}}");

            result.Count.Should().Be(2);
            result[0].Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result[0].Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
            result[0].Roles.Tenant.Should().BeEmpty();
            result[0].Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
            result[0].Features.Platform.Should().BeEmpty();
            result[0].Features.Tenant.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
            result[1].Roles.All.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result[1].Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Operations);
            result[1].Roles.Tenant.Should().BeEmpty();
            result[1].Features.All.Should().OnlyContain(feat => feat == PlatformFeatures.PaidTrial);
            result[1].Features.Platform.Should().BeEmpty();
            result[1].Features.Tenant.Should().OnlyContain(feat => feat == PlatformFeatures.PaidTrial);
        }
    }
}