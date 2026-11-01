using System;
using R3;

namespace LocalDataStorage.R3
{
    /// <summary>
    /// R3 bridge over <see cref="ILocalDataStorage"/> events. Each extension exposes one of the
    /// storage's three events as an R3 <see cref="Observable{T}"/> so hosts can compose debouncing,
    /// filtering, lifetime binding and <c>CombineLatest</c> patterns without wiring handlers by hand.
    /// </summary>
    public static class ReactiveLocalDataStorageExtensions
    {
        /// <summary>Returns a stream of saved types; completes when the observable is disposed.</summary>
        public static Observable<Type> OnDataSaved(this ILocalDataStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            return Observable.Create<Type>(observer =>
            {
                void Handler(Type type) => observer.OnNext(type);
                storage.DataSaved += Handler;
                return Disposable.Create(() => storage.DataSaved -= Handler);
            });
        }

        /// <summary>Returns a stream of saves filtered to <typeparamref name="TData"/>.</summary>
        public static Observable<Unit> OnDataSaved<TData>(this ILocalDataStorage storage) where TData : LocalData
        {
            Type target = typeof(TData);
            return storage.OnDataSaved()
                .Where(t => t == target)
                .Select(_ => Unit.Default);
        }

        /// <summary>Returns a stream of deleted types.</summary>
        public static Observable<Type> OnDataDeleted(this ILocalDataStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            return Observable.Create<Type>(observer =>
            {
                void Handler(Type type) => observer.OnNext(type);
                storage.DataDeleted += Handler;
                return Disposable.Create(() => storage.DataDeleted -= Handler);
            });
        }

        /// <summary>Returns a stream of deletes filtered to <typeparamref name="TData"/>.</summary>
        public static Observable<Unit> OnDataDeleted<TData>(this ILocalDataStorage storage) where TData : LocalData
        {
            Type target = typeof(TData);
            return storage.OnDataDeleted()
                .Where(t => t == target)
                .Select(_ => Unit.Default);
        }

        /// <summary>Returns a stream that pushes once per <see cref="ILocalDataStorage.DeleteAll"/>.</summary>
        public static Observable<Unit> OnAllDeleted(this ILocalDataStorage storage)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            return Observable.Create<Unit>(observer =>
            {
                void Handler() => observer.OnNext(Unit.Default);
                storage.AllDeleted += Handler;
                return Disposable.Create(() => storage.AllDeleted -= Handler);
            });
        }
    }
}
