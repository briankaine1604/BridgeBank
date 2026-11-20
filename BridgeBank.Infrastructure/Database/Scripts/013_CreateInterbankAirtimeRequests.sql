CREATE TABLE InterbankAirtimeRequests
(
Id UNIQUEIDENTIFIER PRIMARY KEY,
SourceBankCode VARCHAR(20) NOT NULL,
RequestId VARCHAR(40) NOT NULL,
Network INT NOT NULL CHECK (Network IN (1, 2, 3, 4)),
PhoneNumber VARCHAR(20) NOT NULL,
Amount DECIMAL(18, 2) NOT NULL CHECK (Amount > 0),
ProviderRequestId VARCHAR(40) NULL,
Status INT NOT NULL CHECK (Status IN (1, 2, 3, 4, 5, 6)),
ProviderReference VARCHAR(100) NULL,
ProviderMessage NVARCHAR(500) NULL,
CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
UpdatedAt DATETIME2 NULL
);

CREATE UNIQUE INDEX UX_InterbankAirtimeRequests_Source_RequestId
    ON InterbankAirtimeRequests (SourceBankCode, RequestId);
