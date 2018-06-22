CREATE TABLE [dbo].[Value] (
    [Id]       INT          IDENTITY (1, 1) NOT NULL,
    [Name]     VARCHAR (10) NOT NULL,
    [Char]     NVARCHAR (1) NOT NULL,
    [Points]   INT          NOT NULL,
    [Sequence] INT          NOT NULL,
    CONSTRAINT [PK__Value__3214EC07D62ED94D] PRIMARY KEY CLUSTERED ([Id] ASC)
);





