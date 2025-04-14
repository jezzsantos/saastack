-- To be used to use your SQL database to define the read/write models used by all snapshotting aggregates.
-- To be used to keep your SQL database up to date as you change your platform, and subdomains evolve.
--
-- Note: Normalization:
--    We deliberately do NOT define any referential integrity, or associated structure, in this database, because that violates the architectural rules.
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
          WHERE object_id = OBJECT_ID(N'[dbo].[Booking]')
            AND type in (N'U'))
    DROP TABLE [dbo].[Booking]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Booking]
(
    [Id]                 [nvarchar](100) NOT NULL,
    [LastPersistedAtUtc] [datetime]      NULL,
    [IsDeleted]          [bit]           NULL,
    [CreatedAtUtc]       [datetime]      NULL,
    [LastModifiedAtUtc]  [datetime]      NULL,
    [BorrowerId]         [nvarchar](100) NULL,
    [CarId]              [nvarchar](100) NULL,
    [End]                [datetime]      NULL,
    [OrganizationId]     [nvarchar](100) NULL,
    [Start]              [datetime]      NULL,
) ON [PRIMARY]
GO

CREATE INDEX Id
    ON [dbo].[Booking]
        (
         [Id]
            );
CREATE INDEX OrganizationId
    ON [dbo].[Booking]
        (
         [OrganizationId]
            );