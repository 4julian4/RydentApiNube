CREATE TABLE [dbo].[TClientes]
(
	[idCliente] bigint identity(1,1) not null PRIMARY KEY,
    [nombreCliente] VARCHAR(50) NULL, 
    [activoHasta] DATE NULL, 
    [observacion] VARCHAR(MAX) NULL
)
