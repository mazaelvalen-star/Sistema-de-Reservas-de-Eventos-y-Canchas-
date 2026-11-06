namespace AtepReservas.Api.Models;

// Los enums se guardan en MySQL como texto ("Salon", "Pendiente"...), no como número,
// así la base se entiende sola y coincide con los CHECK de 01_esquema.sql.
// Si agregan un valor acá, agréguenlo también en el CHECK de la tabla.

public enum TipoEspacio { Salon, Quincho, Cantina, Cancha }

public enum CondicionTarifaria { Afiliado, NoAfiliado }

public enum EstadoReserva { Pendiente, Confirmada, Cancelada, Vencida }
