CREATE TABLE [dbo].[TSedes]
(
	[idSede] bigint identity(1,1) not null PRIMARY KEY, 
    [idCliente] BIGINT NULL, 
    [nombreSede] VARCHAR(MAX) NULL, 
    [identificadorLocal] VARCHAR(MAX) NULL, 
    [observacion] VARCHAR(MAX) NULL,
	FOREIGN KEY (idCliente) REFERENCES TClientes(idCliente)
)
