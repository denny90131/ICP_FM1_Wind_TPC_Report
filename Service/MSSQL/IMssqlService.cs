using System.Linq.Expressions;

public interface IRepository<T> where T : class
{
    // 取得所有資料
    Task<IEnumerable<T>> GetAllAsync();
    
    // 根據條件尋找資料 (支援 LINQ)
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    
    // 新增單筆資料
    Task AddAsync(T entity);
    
    // 新增多筆資料 (適合批次寫入風機數據)
    Task AddRangeAsync(IEnumerable<T> entities);
    
    // 更新資料
    void Update(T entity);
    
    // 刪除資料
    void Delete(T entity);
    
    // 儲存變更 (Unit of Work)
    Task<int> SaveChangesAsync();
}