CREATE TABLE [dbo].[TSesionesUsuario]
(
    [idSesion] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,

    [idUsuario] BIGINT NOT NULL,
    [idCliente] BIGINT NULL,

    [correoUsuario] VARCHAR(200) NOT NULL,
    [sessionId] VARCHAR(100) NOT NULL,

    [activa] BIT NOT NULL DEFAULT 1,

    [fechaLogin] DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    [fechaUltimaActividad] DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    [fechaCierre] DATETIME2 NULL,
    [motivoCierre] VARCHAR(100) NULL,

    [ip] VARCHAR(80) NULL,
    [userAgent] VARCHAR(500) NULL,

    CONSTRAINT [FK_TSesionesUsuario_TUsuarios]
        FOREIGN KEY ([idUsuario]) REFERENCES [dbo].[TUsuarios]([idUsuario]),

    CONSTRAINT [FK_TSesionesUsuario_TClientes]
        FOREIGN KEY ([idCliente]) REFERENCES [dbo].[TClientes]([idCliente])
);
