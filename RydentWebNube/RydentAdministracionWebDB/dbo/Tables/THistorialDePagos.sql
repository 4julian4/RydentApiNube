CREATE TABLE [dbo].[THistorialDePagos]
(
	[idHistorialDePago] bigint identity(1,1) not null PRIMARY KEY, 
    [idCliente] bigint NULL, 
    [fechaPago] DATE NULL, 
    [diasPago] INT NULL, 
    [valorPago] DECIMAL NULL,
    FOREIGN KEY (idCliente) REFERENCES TClientes(idCliente)
)
