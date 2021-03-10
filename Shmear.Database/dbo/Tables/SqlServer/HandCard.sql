CREATE TABLE [dbo].[HandCard] (
    [Id]       INT IDENTITY (1, 1) NOT NULL,
    [GameId]   INT NOT NULL,
    [PlayerId] INT NOT NULL,
    [CardId]   INT NOT NULL,
    CONSTRAINT [PK__HandCard__3214EC07F841351A] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_HandCard_Card] FOREIGN KEY ([CardId]) REFERENCES [dbo].[Card] ([Id]),
    CONSTRAINT [FK_HandCard_Game] FOREIGN KEY ([GameId]) REFERENCES [dbo].[Game] ([Id]),
    CONSTRAINT [FK_HandCard_Player] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Player] ([Id])
);





