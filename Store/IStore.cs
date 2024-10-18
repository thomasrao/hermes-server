namespace HermesSocketServer.Store
{
    public interface IStore<K, V>
    {
        V? Get(K key);
        IDictionary<K, V> Get();
        Task Load();
        void Remove(K? key);
        Task<bool> Save();
        bool Set(K? key, V? value);
    }

    public interface IStore<L, R, V>
    {
        V? Get(L leftKey, R rightKey);
        IDictionary<R, V> Get(L leftKey);
        Task Load();
        void Remove(L? leftKey, R? rightKey);
        Task<bool> Save();
        bool Set(L? leftKey, R? rightKey, V? value);
    }
}