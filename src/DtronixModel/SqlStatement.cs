﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DtronixModel.Attributes;

namespace DtronixModel
{
    /// <summary>
    /// Class to help in quick and simple CRUD operations on a database table.
    /// </summary>
    /// <typeparam name="T">Model this class will be working with.</typeparam>
    public sealed partial class SqlStatement<T> : IDisposable, IAsyncDisposable
        where T : TableRow, new()
    {
        /// <summary>
        /// Mode this statement will be bound to.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Setup in execution mode.
            /// </summary>
            Execute,

            /// <summary>
            /// Setup in selection mode.
            /// </summary>
            Select,

            /// <summary>
            /// Setup in insertion mode.
            /// </summary>
            Insert,

            /// <summary>
            /// Setup in Update mode.
            /// </summary>
            Update,

            /// <summary>
            /// Setup in deletion mode.
            /// </summary>
            Delete
        }

        internal readonly DbCommand Command;


        private readonly Context _context;

        /// <summary>
        /// Internal mode that the statement was setup with.
        /// </summary>
        private readonly Mode _mode;

        /// <summary>
        /// Name of the table this statement is querying.
        /// </summary>
        private readonly string _tableName;

        /// <summary>
        /// List containing groupings for the query.
        /// </summary>
        private List<string> _sqlGroups;

        /// <summary>
        /// Limits the number of returned rows in the query. -1 for no limits.
        /// </summary>
        private int _sqlLimitCount = -1;

        /// <summary>
        /// Offset for the query limits. -1 for no offset.
        /// </summary>
        private int _sqlLimitOffset = -1;

        /// <summary>
        /// Model array of the current rows to be inserted, deleted or updated.
        /// </summary>
        private T[] _sqlRows;

        /// <summary>
        /// Contains a dictionary list of the sort orders for this query.
        /// </summary>
        private Dictionary<string, SortDirection> _sqlOrders;

        /// <summary>
        /// Holds the columns to select.
        /// </summary>
        private string _sqlSelect = "*";

        /// <summary>
        /// Contains the bound where portion of the query.
        /// </summary>
        private string _sqlWhere;

        /// <summary>
        /// Class logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// True to close the command at the end of the query.
        /// </summary>
        public bool AutoCloseCommand { get; set; } = true;

        /// <summary>
        /// Starts a Statement in the specified mode of operation. Always dispose of the statement to ensure underlying
        /// DbCommand is disposed and prevent memory leaks.
        /// </summary>
        /// <param name="mode">Mode that this query will operate in. Prevents invalid operations.</param>
        /// <param name="context">Context that this query will operate inside of.</param>
        public SqlStatement(Mode mode, Context context)
            : this(mode, context, null)
        {

        }

        /// <summary>
        /// Starts a Statement in the specified mode of operation. Always dispose of the statement to ensure underlying
        /// DbCommand is disposed and prevent memory leaks.
        /// </summary>
        /// <param name="mode">Mode that this query will operate in. Prevents invalid operations.</param>
        /// <param name="context">Context that this query will operate inside of.</param>
        /// <param name="logger">Logger for the current statement execution.</param>
        public SqlStatement(Mode mode, Context context, ILogger logger)
        {
            _context = context;
            _mode = mode;
            Command = context.Connection.CreateCommand();
            _logger = logger;

            try
            {
                if (mode != Mode.Execute)
                    _tableName = AttributeCache<T, TableAttribute>.GetAttribute().Name;
            }
            catch (Exception)
            {
                throw new Exception("Class passed does not have a TableAttribute");
            }
        }

        /// <summary>
        /// Begins selection process and the specifies columns to return from the database.
        /// </summary>
        /// <param name="select">Columns to select.  Selecting "*" will select all the columns in the table</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> Select(string select)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            _sqlSelect = select;
            return this;
        }

        /// <summary>
        /// Begins selection process and the specifies columns to return from the database.
        /// </summary>
        /// <param name="select">Array of columns to select.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> Select(string[] select)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            _sqlSelect = string.Join(", ", select);
            return this;
        }

        /// <summary>
        /// Updates the specified rows in the database. The rows must have their primary keys set.
        /// </summary>
        /// <param name="models">Rows to update with their new values.</param>
        public void Update(T[] models)
        {
            var updateTask = UpdateAsync(models, CancellationToken.None);
            updateTask.Wait();
        }

        /// <summary>
        /// Updates the specified rows in the database. The rows must have their primary keys set.
        /// </summary>
        /// <param name="models">Rows to update with their new values.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task UpdateAsync(T[] models, CancellationToken cancellationToken = default)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            // Open the connection.
            await _context.OpenAsync(cancellationToken);

            _sqlRows = models;
            await ExecuteAsync(cancellationToken);
            Command.Dispose();
        }


        /// <summary>
        /// Deletes the specified rows from the database. The rows must have their primary keys set.
        /// </summary>
        /// <param name="models">Rows to delete.</param>
        public void Delete(T[] models)
        {
            var deleteTask = DeleteAsync(models, CancellationToken.None);
            deleteTask.Wait();
        }

        /// <summary>
        /// Deletes the specified primary keys from the table.
        /// </summary>
        /// <param name="primaryIds">Ids to delete.</param>
        public void Delete(long[] primaryIds)
        {
            var deleteTask = DeleteAsync(primaryIds, CancellationToken.None);

            deleteTask.Wait();
        }

        /// <summary>
        /// Deletes the specified primary keys from the table.
        /// </summary>
        /// <param name="primaryIds">Ids to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteAsync(long[] primaryIds, CancellationToken cancellationToken = default)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            // Open the connection.
            await _context.OpenAsync(cancellationToken);

            var pkName = AttributeCache<T, TableAttribute>.GetAttribute().PrimaryKey;
            WhereIn(pkName, primaryIds.Cast<object>().ToArray());
            await ExecuteAsync(cancellationToken);
        }

        /// <summary>
        /// Deletes the specified rows from the database. The rows must have their primary keys set.
        /// </summary>
        /// <param name="models">Rows to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task DeleteAsync(T[] models, CancellationToken cancellationToken = default)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            // Open the connection.
            await _context.OpenAsync(cancellationToken);

            Where(models);

            _sqlRows = models;
            await ExecuteAsync(cancellationToken);
        }

        /// <summary>
        /// Specifies a column to match the specified values.
        /// </summary>
        /// <param name="column">Column to match against.</param>
        /// <param name="values">Values to check against the specified column.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> WhereIn<T2>(string column, T2[] values)
        {
            ValidateWhere();

            if (string.IsNullOrWhiteSpace(column))
                throw new ArgumentException("Column parameter can not be empty.");

            var sql = new StringBuilder();
            sql.Append(column).Append(" IN(");

            foreach (var value in values)
                sql.Append(CoreBindParameter(value)).Append(',');
            sql.Remove(sql.Length - 1, 1).Append(')');

            _sqlWhere = sql.ToString();

            return this;
        }
        
        /// <summary>
        /// Sets where to the provided row's primary key.
        /// </summary>
        /// <param name="model">Row to provide the primary key for.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> Where(T model)
        {
            return Where(new[] { model });
        }

        /// <summary>
        /// Sets where to the provided row's primary keys.
        /// </summary>
        /// <param name="models">Row to provide the primary key for.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> Where(T[] models)
        {
            ValidateWhere();

            // Set the update by the primary key.
            if (models == null || models.Length == 0)
                throw new ArgumentException("Models parameter can not be null or empty.");

            // Get the primary key for the table parameter 
            var pkName = AttributeCache<T, TableAttribute>.GetAttribute().PrimaryKey;

            // If this is a single model, use a simple "WHERE =" statement.
            if (models.Length == 1)
            {
                _sqlWhere = $"{pkName} = {CoreBindParameter(models[0].GetPKValue())}";
                return this;
            }

            // If we have multiple models, use the WHERE IN statement.
            var sql = new StringBuilder();
            sql.Append(pkName).Append(" IN(");

            foreach (var model in models)
                sql.Append(CoreBindParameter(model.GetPKValue())).Append(',');
            sql.Remove(sql.Length - 1, 1).Append(')');

            _sqlWhere = sql.ToString();

            return this;
        }


        /// <summary>
        /// Specifies a custom where string to be applied to the query. Use the String.Format type arguments for this method.
        /// </summary>
        /// <param name="where">
        /// Where string to apply to the query. Use String.Format holders ({0}, {1}, etc...) for the bound
        /// parameters.
        /// </param>
        /// <param name="parameters">Parameters to bind to the query.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> Where(string where, params object[] parameters)
        {
            ValidateWhere();

            _sqlWhere = SqlBindParameters(where, parameters);

            return this;
        }

        /// <summary>
        /// Limits the rows returned by the server.
        /// </summary>
        /// <param name="count">Number of rows to return.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> Limit(int count)
        {
            return Limit(count, -1);
        }

        /// <summary>
        /// Limits the rows returned by the server.
        /// </summary>
        /// <param name="count">Number of rows to return.</param>
        /// <param name="offset">Number of rows offset the counter into the return set.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> Limit(int count, int offset)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            if (_mode != Mode.Select)
                throw new InvalidOperationException("Can not use the LIMIT method except in SELECT mode.");

            _sqlLimitOffset = offset;
            _sqlLimitCount = count;

            return this;
        }

        /// <summary>
        /// Orders the found results by the specified column.  Call multiple times to specify multiple orders.
        /// </summary>
        /// <param name="column">Column to order.</param>
        /// <param name="direction">Direction to order the specified column.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> OrderBy(string column, SortDirection direction)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            if (_mode != Mode.Select)
                throw new InvalidOperationException("Can not use the ORDER BY method except in SELECT mode.");

            if (_sqlOrders == null)
                _sqlOrders = new Dictionary<string, SortDirection>();

            _sqlOrders.Add(column, direction);

            return this;
        }

        /// <summary>
        /// Groups the statement by the specified column.  Call multiple times to specify multiple groups.
        /// </summary>
        /// <param name="column">Column to add to the group statement.</param>
        /// <returns>Current statement for chaining.</returns>
        public SqlStatement<T> GroupBy(string column)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            if (_mode != Mode.Select)
                throw new InvalidOperationException("Can not use the GROUP BY method except in SELECT mode.");

            if (_sqlGroups == null)
                _sqlGroups = new List<string>();

            _sqlGroups.Add(column);

            return this;
        }
        
        /// <summary>
        /// Builds the SQL statement that this class currently represents.
        /// </summary>
        /// <param name="model">Model to base this query on.</param>
        private void BuildSql(T model)
        {
            var sql = new StringBuilder();

            switch (_mode)
            {
                case Mode.Select:
                    sql.Append("SELECT ").AppendLine(_sqlSelect);
                    sql.Append("FROM ").AppendLine(_tableName);
                    break;
                case Mode.Insert:
                    throw new InvalidOperationException("Can not build an SQL query in the INSERT mode.");
                case Mode.Update:
                    sql.Append("UPDATE ").AppendLine(_tableName);
                    sql.Append("SET ");

                    var changedFields = model.GetChangedValues();

                    // If there are no fields to update, then why are we updating?.
                    if (changedFields.Count == 0)
                        throw new InvalidOperationException("Could not update rows as no values have changed.");

                    foreach (var field in changedFields)
                        sql.Append(field.Key).Append(" = ").Append(CoreBindParameter(field.Value)).Append(", ");

                    sql.Remove(sql.Length - 2, 2).AppendLine();
                    break;
                case Mode.Delete:
                    sql.Append("DELETE FROM ").AppendLine(_tableName);
                    break;

                case Mode.Execute:
                    throw new InvalidOperationException("Can not use all functions in Execute mode.");
            }


            // WHERE
            if (_mode != Mode.Insert && _sqlWhere != null)
                sql.Append("WHERE ").AppendLine(_sqlWhere);

            // GROUP BY
            if (_mode == Mode.Select && _sqlGroups != null)
            {
                sql.Append("GROUP BY ");
                foreach (var groupColumn in _sqlGroups)
                    sql.Append(groupColumn).Append(", ");
                sql.Remove(sql.Length - 2, 2).AppendLine();
            }

            // ORDER BY
            if (_mode == Mode.Select && _sqlOrders != null)
            {
                sql.Append("ORDER BY ");
                foreach (var orderColumn in _sqlOrders.Keys)
                {
                    sql.Append(orderColumn);

                    switch (_sqlOrders[orderColumn])
                    {
                        case SortDirection.Ascending:
                            sql.Append(" ASC, ");
                            break;
                        case SortDirection.Descending:
                            sql.Append(" DESC, ");
                            break;
                    }
                }

                // Remove the trailing ", "
                sql.Remove(sql.Length - 2, 2).AppendLine();
            }

            if (_mode == Mode.Select && _sqlLimitCount != -1)
            {
                sql.Append("LIMIT ");

                if (_sqlLimitOffset != -1)
                    sql.Append(_sqlLimitOffset).Append(", ");

                sql.Append(_sqlLimitCount);
            }

            Command.CommandText = sql.ToString();
            _logger?.Debug("Query: \r\n" + Command.CommandText);
        }


        /// <summary>
        /// Binds a parameter in the current command.
        /// </summary>
        /// <param name="value">Value to bind.</param>
        /// <returns>Parameter name for the binding reference.</returns>
        private string CoreBindParameter(object value)
        {
            var key = "@p" + Command.Parameters.Count;
            var param = Command.CreateParameter();
            param.ParameterName = key;
            param.Value = PrepareParameterValue(value);

            // Logging to output bound parameters to stdout.
            _logger?.Debug("Parameter: " + key + " = " + value);

            Command.Parameters.Add(param);
            return key;
        }

        /// <summary>
        /// Binds a parameter in the current command.
        /// </summary>
        /// <param name="value">Value to bind.</param>
        /// <returns>Parameter name for the binding reference.</returns>
        public string BindParameter(object value)
        {
            if (_mode != Mode.Execute)
                throw new InvalidOperationException("Need to be in Execute mode to use this method.");

            return CoreBindParameter(value);
        }

        /// <summary>
        /// Prepares parameter values by translating them into their proper value types.
        /// </summary>
        /// <param name="value">Parameter to prepare.</param>
        /// <returns>Prepared parameter.</returns>
        private static object PrepareParameterValue(object value)
        {
            if (value is DateTimeOffset offset)
                value = offset.ToString("o");

            return value;
        }

        /// <summary>
        /// Inserts multiple rows into the database.
        /// </summary>
        /// <remarks>
        /// This method by default wraps all inserts into a transaction.
        /// If one of the inserts fails, then all of the inserts are rolled back.
        /// </remarks>
        /// <param name="models">Rows to insert.</param>
        /// <returns>Inserted IDs of the rows.</returns>
        public long[] Insert(T[] models)
        {
            var insertTask = InsertAsync(models, CancellationToken.None);

            return insertTask.Result;
        }

        /// <summary>
        /// Inserts multiple rows into the database.
        /// </summary>
        /// <remarks>
        /// This method by default wraps all inserts into a transaction.
        /// If one of the inserts fails, then all of the inserts are rolled back.
        /// </remarks>
        /// <param name="models">Rows to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Inserted IDs of the rows.</returns>
        public async Task<long[]> InsertAsync(T[] models, CancellationToken cancellationToken = default)
        {
            if (_mode == Mode.Execute)
                throw new InvalidOperationException("Can not use all functions in Execute mode.");

            if (models == null || models.Length == 0)
                throw new ArgumentException("Model array is empty.");

            if (_mode != Mode.Insert)
                throw new InvalidOperationException("Can not insert when statement is not in INSERT mode.");

            // Open the connection.
            await _context.OpenAsync(cancellationToken);

            var columns = AttributeCache<T, TableAttribute>.GetAttribute().ColumnNames;

            var sbSql = new StringBuilder();
            sbSql.Append("INSERT INTO ").Append(_tableName).Append(" (");

            // Add all the column names.
            foreach (var column in columns)
                sbSql.Append("").Append(column).Append(", ");

            // Remove the last ", " from the query.
            sbSql.Remove(sbSql.Length - 2, 2);

            // Add the values.
            sbSql.Append(") VALUES (");
            for (var i = 0; i < columns.Length; i++)
                sbSql.Append("@v").Append(i).Append(", ");

            // Remove the last ", " from the query.
            sbSql.Remove(sbSql.Length - 2, 2);
            sbSql.Append(");");

            long[] newRowIds = null;

            if (_context.LastInsertIdQuery != null)
            {
                sbSql.Append(_context.LastInsertIdQuery);
                newRowIds = new long[models.Length];
            }
            SqlTransaction transaction = null;

            try
            {
                // Start a transaction if one does not already exist for fast bulk inserts.
                if (_context.Transaction == null)
                    transaction = _context.BeginTransaction();

                Command.CommandText = sbSql.ToString();
                Command.Transaction = _context.Transaction.Transaction;

                // Create the parameters for bulk inserts.
                for (var i = 0; i < columns.Length; i++)
                {
                    var parameter = Command.CreateParameter();
                    parameter.ParameterName = "@v" + i;
                    Command.Parameters.Add(parameter);
                }

                // Loop through watch of the provided models.
                for (var i = 0; i < models.Length; i++)
                {
                    var values = models[i].GetAllValues();

                    for (var x = 0; x < values.Length; x++)
                        Command.Parameters[x].Value = PrepareParameterValue(values[x]);

                    if (_context.LastInsertIdQuery != null)
                    {
                        var newRow = await Command.ExecuteScalarAsync(cancellationToken);
                        if (newRow == null)
                            throw new Exception("Unable to insert row");

                        if (newRowIds != null)
                            newRowIds[i] = Convert.ToInt64(newRow);
                    }
                    else
                    {
                        if (await Command.ExecuteNonQueryAsync(cancellationToken) != 1)
                            throw new Exception("Unable to insert row");
                    }

                    _logger?.Debug("Insert: \r\n" + Command.CommandText);
                }

                // Commit all inserts.
                transaction?.Commit();
            }
            catch (Exception)
            {
                // If we encountered an error, rollback the transaction.
                transaction?.Rollback();

                throw;
            }
            finally
            {
                transaction?.Dispose();
            }

            if(newRowIds != null)
                _logger?.Debug("Insert new Row IDs: \r\n" + string.Join(", ", newRowIds));

            return newRowIds;
        }


        /// <summary>
        /// Binds the specified parameters to the partial SQL statement.
        /// </summary>
        /// <param name="sql">SQL to bind the parameters to.</param>
        /// <param name="binding">Objects to bind to the partial SQL statement.</param>
        /// <returns>Formatted SQL string to put into the final SQL query.</returns>
        private string SqlBindParameters(string sql, object[] binding)
        {
            if (binding == null)
                return sql;

            var sqlParamHolder = new object[binding.Length];
            for (var i = 0; i < binding.Length; i++)
                sqlParamHolder[i] = CoreBindParameter(binding[i]);

            try
            {
                return string.Format(sql, sqlParamHolder);
            }
            catch (Exception e)
            {
                throw new Exception("Invalid number of placement parameters for the WHERE statement.", e);
            }
        }

        /// <summary>
        /// Validates the current state of the class and checks to see if a where statement is allowed to be called.
        /// </summary>
        private void ValidateWhere()
        {
            switch (_mode)
            {
                case Mode.Execute:
                    throw new InvalidOperationException("Can not use all functions in Execute mode.");
                case Mode.Insert:
                    throw new InvalidOperationException("Can not use the WHERE method in INSERT mode.");
            }

            if (_sqlWhere != null)
                throw new InvalidOperationException("The WHERE statement has already been defined.");
        }

        /// <summary>
        /// Disposes all the resources held by this statement.
        /// </summary>
        public void Dispose()
        {
            Command.Dispose();
        }

        /// <summary>
        /// Disposes all the resources held by this statement.
        /// </summary>
        /// <returns>Task for the completion of the disposal call.</returns>
        public ValueTask DisposeAsync()
        {
            return Command.DisposeAsync();
        }
    }
}