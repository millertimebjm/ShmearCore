CREATE TABLE [dbo].[TrickCard] (
    [Id]       INT IDENTITY (1, 1) NOT NULL,
    [TrickId]  INT NOT NULL,
    [PlayerId] INT NOT NULL,
    [CardId]   INT NOT NULL,
    [Sequence] INT NOT NULL,
    CONSTRAINT [PK__TrickCar__3214EC07AA10B1EC] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TrickCard_Card] FOREIGN KEY ([CardId]) REFERENCES [dbo].[Card] ([Id]),
    CONSTRAINT [FK_TrickCard_Player] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Player] ([Id]),
    CONSTRAINT [FK_TrickCard_Trick] FOREIGN KEY ([TrickId]) REFERENCES [dbo].[Trick] ([Id])
);





