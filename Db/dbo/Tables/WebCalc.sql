﻿CREATE TABLE [dbo].[WebCalc] (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [operator] INT NOT NULL,
    [operand1] FLOAT NOT NULL,
    [operand2] FLOAT NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_WebCalc_Operator] CHECK ([operator]>=(1) AND [operator]<=(4))
);


GO

CREATE INDEX [_Id] ON [dbo].[WebCalc] ([ID])

GO

CREATE INDEX [_Operator_Id] ON [dbo].[WebCalc] ([operator], [ID])
