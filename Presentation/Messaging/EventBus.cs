using System;

namespace InventoryERP.Presentation.Messaging
{
    /// <summary>
    /// R-219: Simple Event Bus for cross-ViewModel communication.
    /// Used to notify ViewModels when data changes that affect multiple views.
    /// </summary>
    public static class EventBus
    {
        /// <summary>
        /// R-219: Raised when a cash transaction (Receipt or Payment) is saved.
        /// CashAccountListViewModel subscribes to this to refresh balances instantly.
        /// </summary>
        public static event EventHandler? CashTransactionSaved;

        /// <summary>
        /// R-219: Raises the CashTransactionSaved event.
        /// Call this after a Receipt or Payment dialog returns successfully.
        /// </summary>
        public static void RaiseCashTransactionSaved()
        {
            CashTransactionSaved?.Invoke(null, EventArgs.Empty);
        }
    }
}
