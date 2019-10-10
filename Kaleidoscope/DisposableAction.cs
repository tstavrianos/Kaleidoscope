using System;

namespace Kaleidoscope
{
    internal struct DisposableAction
        : IDisposable
    {
        /// <summary>Initializes a new instance of the <see cref="DisposableAction"/> struct.</summary>
        /// <param name="onDispose">Action to run when <see cref="Dispose"/>is called.</param>
        public DisposableAction( Action onDispose )
        {
            this._onDispose = onDispose ?? throw new ArgumentNullException( nameof( onDispose ) );
        }

        /// <summary>Runs the action provided in the constructor (<see cref="DisposableAction(Action)"/></summary>
        public void Dispose( )
        {
            this._onDispose( );
        }

        /// <summary>Gets a Default disposable action that does nothing</summary>
        public static DisposableAction Nop => new DisposableAction( ()=> { } );

        private readonly Action _onDispose;
    }
}