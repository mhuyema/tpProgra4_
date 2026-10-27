namespace Payment.Api.Core.Interface
{
    public interface IService<T> where T : class
    {
        Task<List<T>> GetAllAsync();
        Task<T> GetByIdAsync(Guid id);

        Task<bool> DeleteById (Guid id);

    }
}
