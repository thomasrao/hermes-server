namespace HermesSocketServer.Store
{
    public interface IStore<K, V>
    {
        V? Get(K key);
        IEnumerable<V> Get();
        Task Load();
        void Remove(K? key);
        Task<bool> Save();
        bool Set(K? key, V? value);
    }
}