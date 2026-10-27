using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Booking.Api.Core.Exceptions;
using Booking.Api.Core.Interface;

namespace Booking.Api.Core
{
    /// <summary>
    /// Controlador base abstracto para operaciones CRUD comunes.
    /// Proporciona endpoints genéricos para la lectura y eliminación de entidades.
    /// Requiere autenticación por defecto; usá <see cref="AllowAnonymousAttribute"/>
    /// en el controlador derivado si algún endpoint debe ser público.
    /// </summary>
    /// <typeparam name="T">Tipo de la entidad de dominio que hereda de <see cref="Entity"/>.</typeparam>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public abstract class CrudControllerBase<T> : ControllerBase where T : Entity
    {
        /// <summary>
        /// Servicio de aplicación encargado de ejecutar la lógica de negocio y acceso a datos de la entidad <typeparamref name="T"/>.
        /// </summary>
        protected readonly IService<T> _service;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CrudControllerBase{T}"/>.
        /// </summary>
        /// <param name="service">Instancia del servicio base para la entidad de tipo <typeparamref name="T"/>.</param>
        public CrudControllerBase(IService<T> service)
        {
            _service = service;
        }

        /// <summary>
        /// Obtiene el listado completo de entidades guardadas.
        /// </summary>
        /// <remarks>
        /// Este endpoint utiliza caché de salida (<see cref="OutputCacheAttribute"/>) para optimizar el rendimiento
        /// en peticiones repetitivas de lectura.
        /// </remarks>
        /// <returns>Una lista de todas las entidades encontradas.</returns>
        /// <response code="200">Devuelve la lista completa de entidades de tipo <typeparamref name="T"/>.</response>
        /// <response code="401">El usuario no está autenticado.</response>
        [HttpGet]
        [OutputCache]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public virtual async Task<IActionResult> GetAll()
        {
            var entities = await _service.GetAllAsync();
            return Ok(entities);
        }

        /// <summary>
        /// Obtiene una entidad específica mediante su identificador único (GUID).
        /// </summary>
        /// <param name="id">Identificador único de la entidad en formato GUID.</param>
        /// <returns>La entidad encontrada o una respuesta de no encontrado.</returns>
        /// <response code="200">Devuelve la entidad solicitada correspondiente al ID provisto.</response>
        /// <response code="404">No se encontró ninguna entidad con el ID especificado.</response>
        /// <response code="401">El usuario no está autenticado.</response>
        [HttpGet("{id:guid}")]
        [OutputCache]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public virtual async Task<IActionResult> GetById(Guid id)
        {
            var entity = await _service.GetByIdAsync(id)
                ?? throw new NotFoundException($"No se encontró ninguna entidad de tipo '{typeof(T).Name}' con el ID '{id}'.");

            return Ok(entity);
        }

        /// <summary>
        /// Elimina una entidad del sistema mediante su identificador único (GUID).
        /// </summary>
        /// <param name="id">Identificador único de la entidad que se desea eliminar.</param>
        /// <returns>Sin contenido en caso de éxito, o error 404 si la entidad no existe.</returns>
        /// <response code="204">La entidad fue eliminada exitosamente del sistema.</response>
        /// <response code="404">No se encontró ninguna entidad con el ID especificado para eliminar.</response>
        /// <response code="401">El usuario no está autenticado.</response>
        /// <response code="403">El usuario no posee permisos suficientes para eliminar este recurso.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public virtual async Task<IActionResult> DeleteById(Guid id)
        {
            bool deleted = await _service.DeleteById(id);

            if (!deleted)
                throw new NotFoundException($"No se encontró ninguna entidad de tipo '{typeof(T).Name}' con el ID '{id}'.");

            return NoContent();
        }
    }
}