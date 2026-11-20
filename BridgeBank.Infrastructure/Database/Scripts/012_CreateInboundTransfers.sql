CREATE TABLE InboundTransfers
(
Id UNIQUEIDENTIFIER PRIMARY KEY,
AccountId UNIQUEIDENTIFIER NOT NULL,
SourceBankCode VARCHAR(20) NOT NULL,
SourceAccountNumber VARCHAR(20) NOT NULL,
Amount DECIMAL(18, 2) NOT NULL CHECK (Amount > 0),
ClientReference VARCHAR(40) NOT NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
);

CREATE UNIQUE INDEX UX_InboundTransfers_Source_ClientReference
    ON InboundTransfers (SourceBankCode, ClientReference);
