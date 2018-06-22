CREATE TABLE [dbo].[Game] (
    [Id]          INT      IDENTITY (1, 1) NOT NULL,
    [Team1Points] INT      NOT NULL,
    [Team2Points] INT      NOT NULL,
    [CreatedDate] DATETIME NOT NULL,
    [StartedDate] DATETIME NULL,
    CONSTRAINT [PK__Game__3214EC07D19C5FE1] PRIMARY KEY CLUSTERED ([Id] ASC)
);



