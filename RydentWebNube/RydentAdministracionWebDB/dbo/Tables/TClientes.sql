CREATE TABLE [dbo].[TClientes]
(
    [idCliente] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,

    [clienteGuid] UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TClientes_clienteGuid DEFAULT NEWID(),

    [nombreCliente] NVARCHAR(50) NULL,
    [activoHasta] DATE NULL,
    [observacion] NVARCHAR(MAX) NULL,

    [telefono1] NVARCHAR(30) NULL,
    [telefono2] NVARCHAR(30) NULL,
    [emailContacto] NVARCHAR(120) NULL,
    [direccion] NVARCHAR(200) NULL,
    [ciudad] NVARCHAR(80) NULL,
    [pais] NVARCHAR(80) NULL,

    [clienteDesde] DATE NULL,
    [diaPago] TINYINT NULL,
    [fechaProximoPago] DATE NULL,
    [planNombre] NVARCHAR(50) NULL,

    [usaRydentWeb] BIT NOT NULL CONSTRAINT DF_TClientes_usaRydentWeb DEFAULT (1),
    [usaDataico] BIT NOT NULL CONSTRAINT DF_TClientes_usaDataico DEFAULT (0),
    [usaFacturaTech] BIT NOT NULL CONSTRAINT DF_TClientes_usaFacturaTech DEFAULT (0),

    [billingTenantId] UNIQUEIDENTIFIER NULL,

    [estado] BIT NOT NULL CONSTRAINT DF_TClientes_estado DEFAULT (1),
    [fechaCreacion] DATETIME2 NOT NULL CONSTRAINT DF_TClientes_fechaCreacion DEFAULT SYSDATETIME(),
    [fechaActualizacion] DATETIME2 NULL
);
