-- noinspection SqlResolveForFile
USE [TestDatabase] -- Same as found in the appsettings.Testing.json
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[testentities]')
            AND type in (N'U'))
    DROP TABLE [dbo].[testentities]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[testincompatibleentities]')
            AND type in (N'U'))
    DROP TABLE [dbo].[testincompatibleentities]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[firstjoiningtestentities]')
            AND type in (N'U'))
    DROP TABLE [dbo].[firstjoiningtestentities]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[secondjoiningtestentities]')
            AND type in (N'U'))
    DROP TABLE [dbo].[secondjoiningtestentities]
GO

IF EXISTS(SELECT *
          FROM sys.objects
          WHERE object_id = OBJECT_ID(N'[dbo].[EventStore]')
            AND type in (N'U'))
    DROP TABLE [dbo].[EventStore]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[testentities]
(
    [Id]                                 [nvarchar](100)     NOT NULL,
    [LastPersistedAtUtc]                 [datetime2]         NULL,
    [IsDeleted]                          [bit]               NULL,
    [ABinaryValue]                       [varbinary](max)    NULL,
    [ABooleanValue]                      [bit]               NULL,
    [AComplexObjectValue]                [nvarchar](max)     NULL,
    [ADateTimeOffsetValue]               [datetimeoffset](7) NULL,
    [ADateTimeUtcValue]                  [datetime2]         NULL,
    [ADecimalValue]                      [decimal](18, 3)    NULL,
    [ADoubleValue]                       [float]             NULL,
    [AGuidValue]                         [nvarchar](36)      NULL,
    [ALongValue]                         [bigint]            NULL,
    [AnIntValue]                         [int]               NULL,
    [ANullableEnumValue]                 [nvarchar](max)     NULL,
    [AComplexNonValueObjectValue]        [nvarchar](max)     NULL,
    [AnOptionalComplexObjectValue]       [nvarchar](max)     NULL,
    [AnOptionalDateTimeUtcValue]         [datetime2]         NULL,
    [AnOptionalEnumValue]                [nvarchar](max)     NULL,
    [AnOptionalNullableDateTimeUtcValue] [datetime2]         NULL,
    [AnOptionalNullableStringValue]      [nvarchar](max)     NULL,
    [AnOptionalStringValue]              [nvarchar](max)     NULL,
    [AnOptionalValueObjectValue]         [nvarchar](max)     NULL,
    [ANullableBooleanValue]              [bit]               NULL,
    [ANullableDateTimeOffsetValue]       [datetimeoffset](7) NULL,
    [ANullableDateTimeUtcValue]          [datetime2]         NULL,
    [ANullableDecimalValue]              [decimal](18, 3)    NULL,
    [ANullableDoubleValue]               [float]             NULL,
    [ANullableGuidValue]                 [nvarchar](36)      NULL,
    [ANullableIntValue]                  [int]               NULL,
    [ANullableLongValue]                 [bigint]            NULL,
    [AStringValue]                       [nvarchar](max)     NULL,
    [AValueObjectValue]                  [nvarchar](max)     NULL,
    [EnumValue]                          [nvarchar](max)     NULL,
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[testincompatibleentities]
(
    [Id]                                 [nvarchar](100)     NOT NULL,
    [LastPersistedAtUtc]                 [datetime2]         NULL,
    [IsDeleted]                          [bit]               NULL,
    [DefaultSortByUtc]                   [datetime2]         NULL,
    [AnIdProperty]                       [nvarchar](max)     NULL,
    [AnSourceOnlyProperty]               [nvarchar](max)     NULL,
    [AnSourceProperty]                   [nvarchar](max)     NULL,
    [AnTargetCalculatedProperty]         [nvarchar](max)     NULL,
    [AnTargetMappedProperty]             [nvarchar](max)     NULL,
    [AnTargetOnlyProperty]               [nvarchar](max)     NULL,
    [AUnixTimeStamp]                     [bigint]            NULL,
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[firstjoiningtestentities]
(
    [Id]                 [nvarchar](100)  NOT NULL,
    [LastPersistedAtUtc] [datetime2]      NULL,
    [IsDeleted]          [bit]            NULL,
    [AnIntValue]         [int]            NULL,
    [AStringValue]       [nvarchar](1000) NULL,
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[secondjoiningtestentities]
(
    [Id]                 [nvarchar](100)  NOT NULL,
    [LastPersistedAtUtc] [datetime2]      NULL,
    [IsDeleted]          [bit]            NULL,
    [AnIntValue]         [int]            NULL,
    [ALongValue]         [bigint]         NULL,
    [AStringValue]       [nvarchar](1000) NULL,
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[EventStore]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime2]     NULL,
    [IsDeleted]          [bit]           NULL,
    [Version]            [bigint]        NULL,
    [EntityType]         [nvarchar](max) NULL,
    [EntityName]         [nvarchar](max) NULL,
    [EventType]          [nvarchar](max) NULL,
    [Data]               [nvarchar](max) NULL,
    [Metadata]           [nvarchar](max) NULL,
    [StreamName]         [nvarchar](max) NULL,
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[BlobStore] (
    [Id]          nvarchar(100) NOT NULL,
    [BlobName]    NVARCHAR(256) NOT NULL,
    [ContentType] NVARCHAR(256) NULL,
    [Data]        VARBINARY(MAX) NULL,
);
GO