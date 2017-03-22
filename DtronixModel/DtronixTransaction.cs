﻿using System;
using System.Data.Common;

namespace DtronixModel {
	/// <summary>
	/// Transaction wrapper to handle internal transactions and object states.
	/// </summary>
	public class DtronixTransaction : IDisposable {
		private bool disposed = false;

		/// <summary>
		/// Wrapped transaction for the database.
		/// </summary>
		private readonly DbTransaction transaction;

		/// <summary>
		/// Method to call on transaction disposal.
		/// </summary>
		private readonly Action on_dispose;

		/// <summary>
		/// Creates a wrapped transaction with action on disposal.
		/// </summary>
		/// <param name="transaction">Transaction to wrap.</param>
		/// <param name="on_dispose">Method to call on transaction disposal.</param>
		public DtronixTransaction(DbTransaction transaction, Action on_dispose) {
			this.transaction = transaction;
			this.on_dispose = on_dispose;
		}

		/// <summary>
		/// Commits the database transaction.
		/// </summary>
		public void Commit() {
			transaction.Commit();
		}

		/// <summary>
		/// Rolls back a transaction from a pending state.
		/// </summary>
		public void Rollback() {
			transaction.Rollback();
		}

		/// <summary>
		/// Releases the unmanaged resources used by the System.Data.Common.DbTransaction.
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		/// <summary>
		/// Protected implementation of Dispose pattern. 
		/// </summary>
		/// <param name="disposing">True if the object is in the process of being disposed.</param>
		protected virtual void Dispose(bool disposing) {
			if (disposed)
				return;

			if (disposing) {
				transaction.Dispose();
				on_dispose();
			}

			disposed = true;
		}
	}
}
