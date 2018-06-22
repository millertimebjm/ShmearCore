CREATE TABLE [dbo].[Card] (
    [Id]      INT IDENTITY (1, 1) NOT NULL,
    [SuitId]  INT NOT NULL,
    [ValueId] INT NOT NULL,
    CONSTRAINT [PK__Card__3214EC070261639A] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Card_Suit] FOREIGN KEY ([SuitId]) REFERENCES [dbo].[Suit] ([Id]),
    CONSTRAINT [FK_Card_Value] FOREIGN KEY ([ValueId]) REFERENCES [dbo].[Value] ([Id])
);



