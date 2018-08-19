CREATE TABLE [dbo].[WebCalc] (
    [Id]       INT NOT NULL,
    [operator] INT NOT NULL,
    [operand1] INT NOT NULL,
    [operand2] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CK_WebCalc_Operator] CHECK ([operator]>=(1) AND [operator]<=(4))
);

