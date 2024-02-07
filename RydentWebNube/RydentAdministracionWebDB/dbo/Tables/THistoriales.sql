CREATE TABLE [dbo].[THistoriales]
(
	[idHistorial] bigint identity(1,1) not null PRIMARY KEY, 
    [idCliente] BIGINT NULL, 
    [idUsuario] BIGINT NULL, 
    [fecha] DATE NULL, 
    [hora] TIMESTAMP NULL, 
    [observacion] VARCHAR(MAX) NULL,
    FOREIGN KEY (idCliente) REFERENCES TClientes(idCliente),    
    FOREIGN KEY (idUsuario) REFERENCES TUsuarios(idUsuario)
)
