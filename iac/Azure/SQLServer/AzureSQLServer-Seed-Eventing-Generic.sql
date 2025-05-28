-- To be used to keep your SQL database up to date as you change your platform, and subdomain read models.
-- Note: We deliberately do NOT define any referential integrity, or associated structure, in this database, because that violates the architectural rules.
--    The tables pertaining to each subdomain should always remain independent of each other
--    Individual subdomains could be split and deployed into separate databases at any time
--      you are not to be joining across subdomains 
--      you are not to be creating joins across read-models, you can just write the full de-normalized table from your projection 
--      you can write multiple read-models from multiple projections (if you need different representations) 
-- Note: Column Definitions:
--     We are deliberately defining most textual columns (in most of the read-model tables) as "string nvarchar(max)" by default
--       this is to allow for future expansion of the content of the column as you code evolves, without having to change the sizes of the columns.
--       you are free to modify that default to some nominal value (across the board) as you wish (i.e. "nvarchar(800)").
--       columns with indexes cannot be "nvarchar(max)" because of the index size limit of 900 bytes.
--     We are only specifically using other datatypes, where we know they are very un-likely to change over time.
--       if you want to be more specific about data types and want to optimize column design early, you will need to be very careful not to change the code in the future.
--       we recommend optimizing for change, rather than optimizing for performance, until your product has matured and fully developed, or has scaled dramatically.
--       most read-model columns will change from string to JSON(ValueObject) as things change in your domain models, so limiting them too early to specific datatypes can backfire later on in production workloads.
--     We are deliberately defining most columns as NULL for the same reason. To avoid, as much as possible, having to make changes to the schema in the future, when the code changes.
--       clearly there are limits to this strategy, so this strategy is simply minimizing them, since we don't care at this stage about optimizing SQL storage in the cloud (i.e. no longer depend on spinning hard disks, tracks and sectors).  
--
-- noinspection SqlDialectInspectionForFile

USE
    [saastack-sqldatabase]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[APIKeyAuth]')
            AND type in (N'U'))
    DROP TABLE [dbo].[APIKeyAuth]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Audit]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Audit]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[CredentialAuth]')
            AND type in (N'U'))
    DROP TABLE [dbo].[CredentialAuth]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[EmailDelivery]')
            AND type in (N'U'))
    DROP TABLE [dbo].[EmailDelivery]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[EndUser]')
            AND type in (N'U'))
    DROP TABLE [dbo].[EndUser]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Invitation]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Invitation]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Image]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Image]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Membership]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Membership]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[MfaAuthenticator]')
            AND type in (N'U'))
    DROP TABLE [dbo].[MfaAuthenticator]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Organization]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Organization]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[SmsDelivery]')
            AND type in (N'U'))
    DROP TABLE [dbo].[SmsDelivery]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[SSOUser]')
            AND type in (N'U'))
    DROP TABLE [dbo].[SSOUser]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[Subscription]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Subscription]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[UserProfile]')
            AND type in (N'U'))
    DROP TABLE [dbo].[UserProfile]
GO


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

--

CREATE TABLE [dbo].[APIKeyAuth]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Description]        [nvarchar](100) NULL,
    [ExpiresOn]          [datetime]      NULL,
    [KeyToken]           [nvarchar](450) NULL,
    [RevokedOn]          [datetime]      NULL,
    [UserId]             [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[APIKeyAuth]
        (
         [Id]
            );
CREATE INDEX KeyToken
    ON [dbo].[APIKeyAuth]
        (
         [KeyToken]
            );
CREATE INDEX UserId
    ON [dbo].[APIKeyAuth]
        (
         [UserId]
            );

CREATE TABLE [dbo].[Audit]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [AgainstId]          [nvarchar](100) NULL,
    [AuditCode]          [nvarchar](max) NULL,
    [Created]            [datetime]      NULL,
    [MessageTemplate]    [nvarchar](max) NULL,
    [OrganizationId]     [nvarchar](100) NULL,
    [RegisteredRegion]   [nvarchar](100) NULL,
    [TemplateArguments]  [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Audit]
        (
         [Id]
            );
CREATE INDEX UserId
    ON [dbo].[Audit]
        (
         [AgainstId]
            );

CREATE TABLE [dbo].[CredentialAuth]
(
    [Id]                            [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]            [datetime]      NULL,
    [IsDeleted]                     [bit]           NULL,
    [AccountLocked]                 [bit]           NULL,
    [IsMfaEnabled]                  [bit]           NULL,
    [MfaAuthenticationExpiresAt]    [datetime]      NULL,
    [MfaAuthenticationToken]        [nvarchar](100) NULL,
    [MfaCanBeDisabled]              [bit]           NULL,
    [PasswordResetToken]            [nvarchar](450) NULL,
    [RegistrationVerificationToken] [nvarchar](max) NULL,
    [RegistrationVerified]          [bit]           NULL,
    [UserEmailAddress]              [nvarchar](max) NULL,
    [UserId]                        [nvarchar](100) NULL,
    [UserName]                      [nvarchar](450) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[CredentialAuth]
        (
         [Id]
            );
CREATE INDEX PasswordResetToken
    ON [dbo].[CredentialAuth]
        (
         [PasswordResetToken]
            );
CREATE INDEX UserId
    ON [dbo].[CredentialAuth]
        (
         [UserId]
            );
CREATE INDEX UserName
    ON [dbo].[CredentialAuth]
        (
         [UserName]
            );
CREATE INDEX MfaAuthenticationToken
    ON [dbo].[CredentialAuth]
        (
         [MfaAuthenticationToken]
            );

CREATE TABLE [dbo].[EmailDelivery]
(
    [Id]                   [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]   [datetime]      NULL,
    [IsDeleted]            [bit]           NULL,
    [Attempts]             [nvarchar](max) NULL,
    [Body]                 [nvarchar](max) NULL,
    [ContentType]          [nvarchar](max) NULL,
    [Created]              [datetime]      NULL,
    [Delivered]            [datetime]      NULL,
    [DeliveryFailed]       [datetime]      NULL,
    [DeliveryFailedReason] [nvarchar](max) NULL,
    [LastAttempted]        [datetime]      NULL,
    [MessageId]            [nvarchar](450) NULL,
    [OrganizationId]       [nvarchar](450) NULL,
    [ReceiptId]            [nvarchar](450) NULL,
    [RegisteredRegion]     [nvarchar](100) NULL,
    [SendFailed]           [datetime]      NULL,
    [Sent]                 [datetime]      NULL,
    [Subject]              [nvarchar](max) NULL,
    [Substitutions]        [nvarchar](max) NULL,
    [Tags]                 [nvarchar](max) NULL,
    [TemplateId]           [nvarchar](max) NULL,
    [ToDisplayName]        [nvarchar](max) NULL,
    [ToEmailAddress]       [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[EmailDelivery]
        (
         [Id]
            );

CREATE INDEX MessageId
    ON [dbo].[EmailDelivery]
        (
         [MessageId]
            );

CREATE INDEX ReceiptId
    ON [dbo].[EmailDelivery]
        (
         [ReceiptId]
            );

CREATE TABLE [dbo].[EndUser]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Access]             [nvarchar](max) NULL,
    [Classification]     [nvarchar](max) NULL,
    [Features]           [nvarchar](max) NULL,
    [RegisteredRegion]   [nvarchar](max) NULL,
    [Roles]              [nvarchar](max) NULL,
    [Status]             [nvarchar](max) NULL,
    [Username]           [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[EndUser]
        (
         [Id]
            );

CREATE TABLE [dbo].[Invitation]
(
    [Id]                   [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]   [datetime]      NULL,
    [IsDeleted]            [bit]           NULL,
    [AcceptedAt]           [datetime]      NULL,
    [AcceptedEmailAddress] [nvarchar](max) NULL,
    [InvitedById]          [nvarchar](100) NULL,
    [InvitedEmailAddress]  [nvarchar](450) NULL,
    [Status]               [nvarchar](100) NULL,
    [Token]                [nvarchar](450) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Invitation]
        (
         [Id]
            );
CREATE INDEX InvitedEmailAddress
    ON [dbo].[Invitation]
        (
         [InvitedEmailAddress]
            );
CREATE INDEX Status
    ON [dbo].[Invitation]
        (
         [Status]
            );
CREATE INDEX Token
    ON [dbo].[Invitation]
        (
         [Token]
            );

CREATE TABLE [dbo].[Image]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [ContentType]        [nvarchar](max) NULL,
    [CreatedById]        [nvarchar](100) NULL,
    [Description]        [nvarchar](max) NULL,
    [Filename]           [nvarchar](max) NULL,
    [Size]               [bigint]        NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Image]
        (
         [Id]
            );

CREATE TABLE [dbo].[Membership]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [Features]           [nvarchar](max) NULL,
    [IsDefault]          [bit]           NULL,
    [OrganizationId]     [nvarchar](100) NULL,
    [Ownership]          [nvarchar](max) NULL,
    [Roles]              [nvarchar](max) NULL,
    [UserId]             [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Membership]
        (
         [Id]
            );

CREATE TABLE [dbo].[MfaAuthenticator]
(
    [Id]                    [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]    [datetime]      NULL,
    [IsDeleted]             [bit]           NULL,
    [BarCodeUri]            [nvarchar](max) NULL,
    [VerifiedState]         [nvarchar](max) NULL,
    [IsActive]              [bit]           NULL,
    [State]                 [nvarchar](max) NULL,
    [OobChannelValue]       [nvarchar](max) NULL,
    [OobCode]               [nvarchar](max) NULL,
    [CredentialId]          [nvarchar](100) NULL,
    [Secret]                [nvarchar](max) NULL,
    [Type]                  [nvarchar](max) NULL,
    [UserId]                [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[MfaAuthenticator]
        (
         [Id]
            );

CREATE TABLE [dbo].[Organization]
(
    [Id]                    [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]    [datetime]      NULL,
    [IsDeleted]             [bit]           NULL,
    [AvatarImageId]         [nvarchar](100) NULL,
    [AvatarUrl]             [nvarchar](max) NULL,
    [BillingSubscriberId]   [nvarchar](100) NULL,
    [BillingSubscriptionId] [nvarchar](100) NULL,
    [CreatedById]           [nvarchar](100) NULL,
    [Name]                  [nvarchar](max) NULL,
    [Ownership]             [nvarchar](max) NULL,
    [RegisteredRegion]      [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Organization]
        (
         [Id]
            );
CREATE INDEX AvatarImageId
    ON [dbo].[Organization]
        (
         [AvatarImageId]
            );

CREATE TABLE [dbo].[SmsDelivery]
(
    [Id]                   [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]   [datetime]      NULL,
    [IsDeleted]            [bit]           NULL,
    [Attempts]             [nvarchar](max) NULL,
    [Body]                 [nvarchar](max) NULL,
    [Created]              [datetime]      NULL,
    [Delivered]            [datetime]      NULL,
    [DeliveryFailed]       [datetime]      NULL,
    [DeliveryFailedReason] [nvarchar](max) NULL,
    [LastAttempted]        [datetime]      NULL,
    [MessageId]            [nvarchar](450) NULL,
    [OrganizationId]       [nvarchar](450) NULL,
    [ReceiptId]            [nvarchar](450) NULL,
    [RegisteredRegion]     [nvarchar](100) NULL,
    [SendFailed]           [datetime]      NULL,
    [Sent]                 [datetime]      NULL,
    [Tags]                 [nvarchar](max) NULL,
    [ToPhoneNumber]        [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[SmsDelivery]
        (
         [Id]
            );

CREATE INDEX MessageId
    ON [dbo].[SmsDelivery]
        (
         [MessageId]
            );

CREATE INDEX ReceiptId
    ON [dbo].[SmsDelivery]
        (
         [ReceiptId]
            );

CREATE TABLE [dbo].[Subscription]
(
    [Id]                    [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]    [datetime]      NULL,
    [IsDeleted]             [bit]           NULL,
    [BuyerId]               [nvarchar](100) NULL,
    [BuyerReference]        [nvarchar](450) NULL,
    [OwningEntityId]        [nvarchar](100) NULL,
    [ProviderName]          [nvarchar](max) NULL,
    [ProviderState]         [nvarchar](max) NULL,
    [SubscriptionReference] [nvarchar](450) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Subscription]
        (
         [Id]
            );
CREATE INDEX BuyerReference
    ON [dbo].[Subscription]
        (
         [BuyerReference]
            );
CREATE INDEX OwningEntityId
    ON [dbo].[Subscription]
        (
         [OwningEntityId]
            );
CREATE INDEX SubscriptionReference
    ON [dbo].[Subscription]
        (
         [SubscriptionReference]
            );

CREATE TABLE [dbo].[SSOUser]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [CountryCode]        [nvarchar](max) NULL,
    [EmailAddress]       [nvarchar](max) NULL,
    [FirstName]          [nvarchar](max) NULL,
    [LastName]           [nvarchar](max) NULL,
    [ProviderName]       [nvarchar](max) NULL,
    [ProviderUId]        [nvarchar](450) NULL,
    [Timezone]           [nvarchar](max) NULL,
    [UserId]             [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[SSOUser]
        (
         [Id]
            );
CREATE INDEX UserId
    ON [dbo].[SSOUser]
        (
         [UserId]
            );
CREATE INDEX ProviderUId
    ON [dbo].[SSOUser]
        (
         [ProviderUId]
            );

CREATE TABLE [dbo].[UserProfile]
(
    [Id]                    [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc]    [datetime]      NULL,
    [IsDeleted]             [bit]           NULL,
    [AvatarImageId]         [nvarchar](100) NULL,
    [AvatarUrl]             [nvarchar](max) NULL,
    [CountryCode]           [nvarchar](max) NULL,
    [DefaultOrganizationId] [nvarchar](100) NULL,
    [DisplayName]           [nvarchar](max) NULL,
    [EmailAddress]          [nvarchar](450) NULL,
    [FirstName]             [nvarchar](max) NULL,
    [LastName]              [nvarchar](max) NULL,
    [PhoneNumber]           [nvarchar](max) NULL,
    [Timezone]              [nvarchar](max) NULL,
    [Type]                  [nvarchar](max) NULL,
    [UserId]                [nvarchar](100) NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[UserProfile]
        (
         [Id]
            );
CREATE INDEX AvatarImageId
    ON [dbo].[UserProfile]
        (
         [AvatarImageId]
            );
CREATE INDEX EmailAddress
    ON [dbo].[UserProfile]
        (
         [EmailAddress]
            );
CREATE INDEX UserId
    ON [dbo].[UserProfile]
        (
         [UserId]
            );