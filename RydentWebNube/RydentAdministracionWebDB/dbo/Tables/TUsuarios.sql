CREATE TABLE [dbo].[TUsuarios]
(
	[idUsuario] bigint identity(1,1) not null PRIMARY KEY, 
    [idCliente] bigint NULL, 
    [nombreUsuario] VARCHAR(50) NULL, 
    [correoUsuario] VARCHAR(50) NULL, 
    [estado] BIT NULL,
    [codigoExternoUsuario] VARCHAR(200) NULL, 
    FOREIGN KEY (idCliente) REFERENCES TClientes(idCliente)
)
