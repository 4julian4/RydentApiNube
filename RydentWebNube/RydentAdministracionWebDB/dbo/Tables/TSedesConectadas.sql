CREATE TABLE [dbo].[TSedesConectadas]
(
	[idSedeConectada] bigint identity(1,1) not null PRIMARY KEY, 
    [idCliente] BIGINT NULL, 
    [idSede] BIGINT NULL, 
    [idActualSignalR] VARCHAR(MAX) NULL, 
    [fechaUltimoAcceso] DATE NULL, 
    [activo] BIT NULL,
    
    FOREIGN KEY (idCliente) REFERENCES TClientes(idCliente),
    FOREIGN KEY (idSede) REFERENCES TSedes(idSede)
)
