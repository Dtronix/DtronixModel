﻿using System;
using System.Linq;
using System.Reflection;
using System.Text;
using DtronixModel.Generator.Ddl;
using Association = DtronixModel.Generator.Ddl.Association;
using Column = DtronixModel.Generator.Ddl.Column;
using Database = DtronixModel.Generator.Ddl.Database;
using Table = DtronixModel.Generator.Ddl.Table;

namespace DtronixModel.Generator.Output
{
    public class CSharpCodeGenerator
    {
        private string[] reservedWords;

        private class AssociationCodeGenerator
        {
            public Table ThisTable { get; set; }
            public Column ThisColumn { get; set; }
            public string ThisAssociationName { get; set; }
            public Cardinality ThisCardinality { get; set; }

            public Table OtherTable { get; set; }
            public Column OtherColumn { get; set; }
            public string OtherAssociationName { get; set; }
            public Cardinality OtherCardinality { get; set; }
        }

        Database database;

        public CSharpCodeGenerator(Database database)
        {
            this.database = database;
            reservedWords = new[] {"for", "with", "while"};
        }

        private bool ColumnIsTypeStruct(Column column)
        {
            switch (column.NetType)
            {
                case "Int64":
                case "Int16":
                case "Int32":
                case "UInt64":
                case "UInt16":
                case "UInt32":
                case "Byte":
                case "Decimal":
                case "Float":
                case "Double":
                case "Boolean":
                case "Char":
                case "DateTimeOffset":
                    return true;
            }

            // See if the type is an enum.
            if (Type.GetType("System." + column.NetType, false) == null)
            {
                return true;
            }

            return true;
        }


        private string ColumnNetType(Column column)
        {
            if (column.NetType == "ByteArray")
            {
                return "byte[]";
            }

            string type = column.NetType;

            if (column.NetType == "Float")
            {
                type = "float";
            }

            if (column.Nullable)
            {
                switch (column.NetType)
                {
                    case "Int64":
                    case "Int16":
                    case "Int32":
                    case "UInt64":
                    case "UInt16":
                    case "UInt32":
                    case "Byte":
                    case "Decimal":
                    case "Float":
                    case "Double":
                    case "Boolean":
                    case "Char":
                    case "DateTimeOffset":
                        type += "?";
                        break;
                }
            }

            return type;
        }

        public string TransformText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// ------------------------------------------------------------------------------");
            sb.AppendLine("//  <auto-generated>");
            sb.AppendLine($"//  Generated by DtronixModeler. Version {Assembly.GetExecutingAssembly().GetName().Version}");
            sb.AppendLine("//  </auto-generated>");
            sb.AppendLine("// ------------------------------------------------------------------------------");
            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.Data.Common;");
            sb.AppendLine($"using System.Collections.Generic;");
            sb.AppendLine($"using System.Collections;");
            sb.AppendLine($"using System.ComponentModel;");
            sb.AppendLine($"using System.Runtime.CompilerServices;");
            sb.AppendLine($"using DtronixModel;");
            sb.AppendLine($"using DtronixModel.Attributes;");

            if (database.ImplementMessagePackAttributes)
                sb.AppendLine("using MessagePack;"); 
            
            if (database.ImplementSystemTextJsonAttributes)
                sb.AppendLine("using System.Text.Json.Serialization;");

            if (database.ImplementDataContractMemberOrder
                || database.ImplementDataContractMemberName)
                sb.AppendLine("using System.Runtime.Serialization;");

            sb.AppendLine();
            sb.AppendLine($"namespace {database.Namespace} {{").AppendLine();
            sb.AppendLine();
            foreach (var enumClass in database.Enumeration)
            {
                sb.AppendLine($"    public enum {enumClass.Name} : int {{");
                int shiftAmount = 0;
                foreach (var enumValue in enumClass.EnumValue)
                {
                    if (enumClass.EnumType == EnumType.Bitwise)
                        sb.AppendLine($"        {enumValue.Name} = 1 << {shiftAmount++},");
                    else if (enumClass.EnumType == EnumType.Increment)
                        sb.AppendLine($"        {enumValue.Name} = {shiftAmount++},");
                }

                sb.AppendLine($"    }}");
                sb.AppendLine();
            }

            sb.AppendLine($"    public partial class {database.ContextClass} : Context {{");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Set a default constructor to allow use of parameter-less context calling.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public static Func<DbConnection> DefaultConnection {{ get; set; }}");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Sets the query string to retrieve the last insert ID.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public new static string LastInsertIdQuery {{ get; set; }} = null;");
            sb.AppendLine();
            sb.AppendLine($"        private static TargetDb _DatabaseType;");
            sb.AppendLine();
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Type of database this context will target.  Automatically sets proper database specific values.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public static TargetDb DatabaseType");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            get {{ return _DatabaseType; }}");
            sb.AppendLine($"            set ");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                _DatabaseType = value;");
            sb.AppendLine($"                switch (value) ");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    case TargetDb.MySql:");
            sb.AppendLine($"                        LastInsertIdQuery = \"SELECT last_insert_id()\";");
            sb.AppendLine($"                        break;");
            sb.AppendLine($"                    case TargetDb.Sqlite:");
            sb.AppendLine($"                        LastInsertIdQuery = \"SELECT last_insert_rowid()\";");
            sb.AppendLine($"                        break;");
            sb.AppendLine($"                    case TargetDb.Other:");
            sb.AppendLine($"                        break;");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}").AppendLine();

            foreach (var table in database.Table)
            {
                sb.AppendLine($"        private Table<{table.Name}> _{table.Name};");
                sb.AppendLine();
                sb.AppendLine($"        public Table<{table.Name}> {table.Name} {{");
                sb.AppendLine($"            get {{");
                sb.AppendLine($"                if (_{table.Name} == null) {{");
                sb.AppendLine($"                    _{table.Name} = new Table<{table.Name}>(this);");
                sb.AppendLine($"                }}");
                sb.AppendLine();
                sb.AppendLine($"                return _{table.Name};");
                sb.AppendLine($"            }}");
                sb.AppendLine($"        }}").AppendLine();
            }

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Create a new context of this database's type.  Can only be used if a default connection is specified.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public {database.ContextClass}() : base(DefaultConnection, LastInsertIdQuery) {{ }}");
            sb.AppendLine($"");
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Create a new context of this database's type with a specific connection.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        /// <param name=\"connection\">Existing open database connection to use.</param>");
            sb.AppendLine($"        public {database.ContextClass}(DbConnection connection) : base(connection, LastInsertIdQuery) {{ }}");
            sb.AppendLine($"");
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Sets the default connection creation method.");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        /// <param name=\"defaultConnection\">Method to be called on each context creation and return the new connection.</param>");
            sb.AppendLine($"        /// <param name=\"targetDb\">Type of DB this is connecting to.</param>");
            sb.AppendLine($"        public override void SetDefaultConnection(Func<DbConnection> defaultConnection, TargetDb targetDb)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"           DefaultConnection = defaultConnection;");
            sb.AppendLine($"           DatabaseType = targetDb;");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}").AppendLine();
            foreach (var table in database.Table)
            {
                sb.AppendLine("");
                Column pk_column = null;

                foreach (var column in table.Column)
                {
                    if (pk_column == null && column.IsPrimaryKey)
                    {
                        pk_column = column;
                    }
                }

                sb.AppendLine($"    [Table(Name = \"{table.Name}\")]");
                if (this.database.ImplementProtobufNetDataContracts)
                    sb.AppendLine($"    [ProtoBuf.ProtoContract]");

                if (this.database.ImplementMessagePackAttributes)
                    sb.AppendLine($"    [MessagePackObject]");

                if (this.database.ImplementDataContractMemberOrder || database.ImplementDataContractMemberName)
                    sb.AppendLine($"    [DataContract]");

                sb.Append($"    public partial class {table.Name} : TableRow");
                if (this.database.ImplementINotifyPropertyChanged)
                    sb.Append(", System.ComponentModel.INotifyPropertyChanged");

                sb.AppendLine();

                sb.AppendLine($"    {{");

                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Contains all the column names in this row, excluding the primary key.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static readonly string[] Columns = new [] {{");
                foreach (var column in table.Column)
                {
                    // We skip the primary key since it is never inserted.
                    if (column.IsPrimaryKey)
                        continue;

                    sb.AppendLine($"            \"{column.Name}\",");
                }
                // Remove the trailing comma.
                sb.Remove(sb.Length - 2, 1);

                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Contains all the columns types, excluding the primary key.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static readonly Type[] ColumnTypes = new [] {{");
                foreach (var column in table.Column)
                {
                    // We skip the primary key since it is never inserted.
                    if (column.IsPrimaryKey)
                        continue;

                    sb.AppendLine($"            typeof({ColumnNetType(column)}),");
                }
                // Remove the trailing comma.
                sb.Remove(sb.Length - 2, 1);

                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Gets the name of the row primary key if one is set.");
                sb.AppendLine($"        /// </summary>");
                sb.Append($"        public static readonly string PrimaryKeyName = ").AppendLine(pk_column == null ? "null; " : $"\"{pk_column.Name}\"; ");

                sb.AppendLine("");
                if (this.database.ImplementINotifyPropertyChanged)
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Implementation for INotifyPropertyChanged.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;");
                }
                sb.AppendLine();

                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Bit array which contains the flags for each table column.");
                sb.AppendLine($"        /// </summary>");

                var totalColumns = Enumerable.Count<Column>(table.Column);
                if (this.database.ImplementProtobufNetDataContracts)
                    sb.AppendLine($"        [ProtoBuf.ProtoMember({totalColumns + 1})]");

                if (this.database.ImplementMessagePackAttributes)
                    sb.AppendLine($"        [Key({totalColumns})]");

                if (this.database.ImplementDataContractMemberOrder)
                    sb.AppendLine($"        [DataMember(Order = {totalColumns})]");

                if (this.database.ImplementDataContractMemberName)
                    sb.AppendLine($"        [DataMember(Name = \"ChangedFlags\")]");

                sb.AppendLine($"        public BitArray ChangedFlags {{ get; set; }}");
                sb.AppendLine();

                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Values which are returned but not part of this table.");
                sb.AppendLine($"        /// </summary>");

                if (this.database.ImplementProtobufNetDataContracts)
                    sb.AppendLine($"        [ProtoBuf.ProtoMember({totalColumns + 2})]");

                if (this.database.ImplementMessagePackAttributes)
                    sb.AppendLine($"        [Key({totalColumns + 1})]");

                if (this.database.ImplementDataContractMemberOrder)
                    sb.AppendLine($"        [DataMember(Order = {totalColumns + 1})]");

                if (this.database.ImplementDataContractMemberName)
                    sb.AppendLine($"        [DataMember(Name = \"AdditionalValues\")]");

                sb.AppendLine($"        public Dictionary<string, object> AdditionalValues {{ get; set; }}");
                sb.AppendLine();

                for (int i = 0; i < totalColumns; i++)
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Column name.");
                    var description = table.Column[i].Description;
                    var descriptionLines = description?.Split('\n');
                    if (string.IsNullOrWhiteSpace(description) == false)
                    {
                        foreach (var descriptionLine in descriptionLines)
                            sb.AppendLine($"        /// {descriptionLine}");
                    }

                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine(
                        $"        public const string {table.Column[i].Name}Column = \"{table.Column[i].Name}\";");
                    sb.AppendLine($"");
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Backing field for the {table.Column[i].Name} property.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        private {ColumnNetType(table.Column[i])} _{table.Column[i].Name};");
                    sb.AppendLine($"");
                    if (string.IsNullOrWhiteSpace(description) == false)
                    {
                        sb.AppendLine($"        /// <summary>");
                        foreach (var descriptionLine in descriptionLines)
                            sb.AppendLine($"        /// {descriptionLine}");
                        sb.AppendLine($"        /// </summary>");
                    }

                    if (this.database.ImplementProtobufNetDataContracts)
                        sb.AppendLine($"        [ProtoBuf.ProtoMember({i + 1})]");

                    if (this.database.ImplementMessagePackAttributes)
                        sb.AppendLine($"        [Key({i})]");

                    if (this.database.ImplementDataContractMemberOrder)
                        sb.AppendLine($"        [DataMember(Order = {i})]");

                    if (this.database.ImplementDataContractMemberName)
                        sb.AppendLine($"        [DataMember(Name = \"{table.Column[i].Name}\")]");

                    sb.Append($"        public {ColumnNetType(table.Column[i])} ");
                    if (reservedWords.Contains(table.Column[i].Name))
                        sb.Append("@");
                    sb.AppendLine(table.Column[i].Name);
                    sb.AppendLine("        {");
                    sb.AppendLine($"            get => _{table.Column[i].Name};");

                    if (table.Column[i].IsReadOnly == false
                        || this.database.ImplementProtobufNetDataContracts
                        || database.ImplementMessagePackAttributes
                        || database.ImplementDataContractMemberOrder
                        || database.ImplementDataContractMemberName)
                    {
                        sb.Append("            ");
                        if ((database.ImplementProtobufNetDataContracts
                             || database.ImplementMessagePackAttributes
                             || database.ImplementDataContractMemberOrder
                             || database.ImplementDataContractMemberName) && table.Column[i].IsReadOnly)
                        {
                            sb.Append("private ");
                        }

                        sb.AppendLine("set");
                        sb.AppendLine("            {");
                        if (table.Column[i].DbLength != 0 && table.Column[i].NetType == "String")
                        {
                            sb.AppendLine($"                if(value != null && value.Length > {table.Column[i].DbLength})");
                            sb.AppendLine($"                    throw new ArgumentOutOfRangeException(\" {table.Column[i].Name}\", \"String {table.Column[i].Name} is too long. Max length allowed is {table.Column[i].DbLength} characters. Passed string is \" + value.Length.ToString() + \" characters.\");");
                        }

                        sb.AppendLine($"                _{table.Column[i].Name} = value;");
                        sb.AppendLine($"                ChangedFlags.Set({i}, true);");
                        if (this.database.ImplementINotifyPropertyChanged)
                        {
                            sb.AppendLine($"                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof({table.Column[i].Name})));");
                        }

                        sb.AppendLine("            }");
                    }

                    sb.AppendLine("        }");
                    sb.AppendLine("");

                }

                foreach (var db_assoc in database.Association)
                {
                    var reference = db_assoc.ReferencesTable(table);
                    var assoc = new AssociationCodeGenerator();

                    if (reference == Association.Reference.R1)
                    {
                        assoc.ThisAssociationName = db_assoc.Table1Name;
                        assoc.ThisColumn = db_assoc.GetReferenceColumn(database, Association.Reference.R1);
                        assoc.ThisTable = table;
                        assoc.ThisCardinality = db_assoc.Table1Cardinality;

                        assoc.OtherAssociationName = db_assoc.Table2Name;
                        assoc.OtherColumn = db_assoc.GetReferenceColumn(database, Association.Reference.R2);
                        assoc.OtherTable = Enumerable.Single<Table>(database.Table, t => t.Name == db_assoc.Table2);
                        assoc.OtherCardinality = db_assoc.Table2Cardinality;


                    }
                    else if (reference == Association.Reference.R2)
                    {
                        assoc.ThisAssociationName = db_assoc.Table2Name;
                        assoc.ThisColumn = db_assoc.GetReferenceColumn(database, Association.Reference.R2);
                        assoc.ThisTable = table;
                        assoc.ThisCardinality = db_assoc.Table2Cardinality;

                        assoc.OtherAssociationName = db_assoc.Table1Name;
                        assoc.OtherColumn = db_assoc.GetReferenceColumn(database, Association.Reference.R1);
                        assoc.OtherTable = Enumerable.Single<Table>(database.Table, t => t.Name == db_assoc.Table1);
                        assoc.OtherCardinality = db_assoc.Table1Cardinality;

                    }
                    else
                    {
                        continue;
                    }

                    string fieldType = assoc.OtherTable.Name;
                    if (assoc.OtherCardinality == Cardinality.Many)
                    {
                        fieldType += "[]";
                    }

                    var fetchType = assoc.OtherCardinality == Cardinality.Many ? "ExecuteFetchAll();" : "ExecuteFetch();";

                    sb.AppendLine($"        private {fieldType} _{assoc.OtherAssociationName};");

                    if (database.ImplementSystemTextJsonAttributes)
                        sb.AppendLine($"        [JsonIgnore]");

                    if (database.ImplementMessagePackAttributes 
                        || database.ImplementProtobufNetDataContracts)
                    {
                        sb.AppendLine($"        [IgnoreMember]");
                    }
                    if (database.ImplementDataContractMemberOrder
                        || database.ImplementDataContractMemberName)
                        sb.AppendLine($"        [IgnoreDataMember]");

                    sb.AppendLine($"        public {fieldType} {assoc.OtherAssociationName}");
                    sb.AppendLine($"        {{");
                    sb.AppendLine($"            get ");
                    sb.AppendLine($"            {{");
                    sb.AppendLine($"                if (_{assoc.OtherAssociationName} != null)");
                    sb.AppendLine($"                    return _{assoc.OtherAssociationName};");
                    sb.AppendLine($"                ");
                    sb.AppendLine($"                try ");
                    sb.AppendLine($"                {{");
                    sb.AppendLine($"                    _{assoc.OtherAssociationName} = (({database.ContextClass})Context).{assoc.OtherTable.Name}.Select().Where(\"{assoc.OtherColumn.Name} = {{0}}\", _{assoc.ThisColumn.Name}).{fetchType}");
                    sb.AppendLine($"                }}");
                    sb.AppendLine($"                catch ");
                    sb.AppendLine($"                {{");
                    sb.AppendLine($"                    //Accessing a property outside of its database context is not allowed.  Access an association inside the database context to cache the values for later use.");
                    sb.AppendLine($"                    _{assoc.OtherAssociationName} = null;");
                    sb.AppendLine($"                }}");
                    sb.AppendLine($"                return _{assoc.OtherAssociationName};");
                    sb.AppendLine($"            }}");
                    sb.AppendLine($"        }}");
                    sb.AppendLine();
                }

                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Clones a {table.Name} row.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <param name=\"source\">Source {table.Name} row to clone from.</param>");
                sb.AppendLine($"        /// <param name=\"onlyChanged\">True to only clone the changes from the source. False to clone all the values regardless of changed or unchanged.</param>");
                sb.AppendLine($"        public {table.Name}({table.Name} source, bool onlyChanged = false)");
                sb.AppendLine($"        {{ ");
                for (int i = 0; i < Enumerable.Count<Column>(table.Column); i++)
                {
                    if (table.Column[i].IsPrimaryKey)
                        sb.AppendLine($"            _{table.Column[i].Name} = source._{table.Column[i].Name};");

                    sb.AppendLine($"            if (onlyChanged == false || source.ChangedFlags.Get({i}))");
                    sb.AppendLine($"                _{table.Column[i].Name} = source._{table.Column[i].Name};");
                }

                sb.AppendLine();
                sb.AppendLine($"            ChangedFlags = new BitArray(source.ChangedFlags);");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Creates a empty {table.Name} row. Use this for creating a new row and inserting into the database.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public {table.Name}() : this(null, null) {{ }}");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Creates a {table.Name} row and reads the row information from the table into this row.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <param name=\"reader\">Instance of a live data reader for this row's table.</param>");
                sb.AppendLine($"        /// <param name=\"context\">The current context of the database.</param>");
                sb.AppendLine($"        public {table.Name}(DbDataReader reader, Context context)");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            ChangedFlags = new BitArray({Enumerable.Count<Column>(table.Column)});");
                sb.AppendLine($"            Read(reader, context);");
                sb.AppendLine($"        }}");
                sb.AppendLine($"");
                var primary_key = Enumerable.FirstOrDefault<Column>(table.Column, c => c.IsPrimaryKey);
                if (primary_key != null)
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// Creates a {table.Name} row and with the specified Id.");
                    sb.AppendLine($"        /// Useful when creating a new matching row on a remote connection.");
                    sb.AppendLine($"        /// </summary>");
                    sb.AppendLine($"        /// <param name=\"id\">Id to set the row to.</param>");
                    sb.AppendLine($"        public {table.Name}({ColumnNetType(primary_key)} id)");
                    sb.AppendLine($"        {{");
                    sb.AppendLine($"            ChangedFlags = new BitArray({Enumerable.Count<Column>(table.Column)});");
                    sb.AppendLine($"            _{primary_key.Name} = id;");
                    sb.AppendLine($"        }}");
                }

                sb.AppendLine($"");
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Reads the row information from the table into this row.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <param name=\"reader\">Instance of a live data reader for this row's table.</param>");
                sb.AppendLine($"        /// <param name=\"context\">The current context of the database.</param>");
                sb.AppendLine($"        public override void Read(DbDataReader reader, Context context) {{");
                sb.AppendLine($"            Context = context;");
                sb.AppendLine($"            if (reader == null)");
                sb.AppendLine($"                return;");
                sb.AppendLine($"");
                sb.AppendLine($"            var length = reader.FieldCount;");
                sb.AppendLine($"            for (var i = 0; i < length; i++)");
                sb.AppendLine($"            {{");
                sb.AppendLine($"                switch (reader.GetName(i))");
                sb.AppendLine($"                {{");


                foreach (var column in table.Column)
                {
                    string type = ColumnNetType(column);
                    string reader_get = column.NetType;

                    if (reader_get == "DateTimeOffset")
                        reader_get = "DateTime";

                    if (Enumerable.Any<Enumeration>(this.database.Enumeration, en => en.Name == column.NetType))
                    {
                        sb.AppendLine($"                    case \"{column.Name}\":");
                        sb.AppendLine($"                        _{column.Name} = ({column.NetType})reader.GetInt32(i);");
                        sb.AppendLine($"                        break;");
                    }
                    else if (column.NetType == "ByteArray")
                    {
                        sb.AppendLine($"                    case \"{column.Name}\":");
                        sb.AppendLine($"                        _{column.Name} = reader.IsDBNull(i) ? null : reader.GetFieldValue<byte[]>(i);");
                        sb.AppendLine($"                        break;");
                    }
                    else if (column.NetType.StartsWith("UInt"))
                    {
                        sb.AppendLine($"                    case \"{column.Name}\":");
                        sb.AppendLine($"                        _{column.Name} = reader.IsDBNull(i) ? default : ({type.Replace("?", "")})reader.GetValue(i);");
                        sb.AppendLine($"                        break;");
                    }
                    else if (column.NetType.StartsWith("SByte"))
                    {
                        sb.AppendLine($"                    case \"{column.Name}\":");
                        sb.AppendLine($"                        _{column.Name} = reader.IsDBNull(i) ? default : ({type.Replace("?", "")})reader.GetByte(i);");
                        sb.AppendLine($"                        break;");
                    }
                    else if (column.Nullable)
                    {
                        sb.AppendLine($"                    case \"{column.Name}\":");
                        sb.AppendLine($"                        _{column.Name} = reader.IsDBNull(i) ? default({type}) : reader.Get{reader_get}(i);");
                        sb.AppendLine($"                        break;");
                    }
                    else if (column.NetType == "String")
                    {
                        sb.AppendLine($"                    case \"{column.Name}\":");
                        sb.AppendLine($"                        _{column.Name} = reader.GetValue(i) as string;");
                        sb.AppendLine($"                        break;");
                    }
                    else
                    {
                        sb.AppendLine($"                    case \"{column.Name}\":");
                        sb.AppendLine($"                        _{column.Name} = reader.Get{reader_get}(i);");
                        sb.AppendLine($"                        break;");
                    }
                }

                sb.AppendLine($"                    default: ");
                sb.AppendLine($"                        if(AdditionalValues == null)");
                sb.AppendLine($"                            AdditionalValues = new Dictionary<string, object>();");
                sb.AppendLine();
                sb.AppendLine($"                        AdditionalValues.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i)); ");
                sb.AppendLine($"                        break;");
                sb.AppendLine($"                }}");
                sb.AppendLine($"            }}");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Gets all the instance values in the row which have been changed.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <returns>Dictionary with the keys of the column names and values of the properties.</returns>");
                sb.AppendLine($"        public override Dictionary<string, object> GetChangedValues()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            var changed = new Dictionary<string, object>();");
                for (var i = 0; i < table.Column.Count; i++)
                {
                    if (table.Column[i].IsPrimaryKey)
                        continue;

                    sb.AppendLine($"            if (ChangedFlags.Get({i}))");
                    sb.AppendLine($"                changed.Add(\"{table.Column[i].Name}\", _{table.Column[i].Name});");
                }

                sb.AppendLine();
                sb.AppendLine($"            return changed;");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Returns true if any of the values have been modified from the properties.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <returns>True if the row has any modified values.</returns>");
                sb.AppendLine($"        public override bool IsChanged()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            var length = ChangedFlags.Length;");
                sb.AppendLine($"            for(var i = 0; i < length; i++){{");
                sb.AppendLine($"                if(ChangedFlags.Get(i))");
                sb.AppendLine($"                    return true;");
                sb.AppendLine($"            }}");
                sb.AppendLine($"            return false;");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Return all the instance values for the entire row.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <returns>An object array with all the values of this row.</returns>");
                sb.AppendLine($"        public override object[] GetAllValues()");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            return new object[] {{");
                foreach (var column in table.Column)
                {
                    if (column.IsPrimaryKey)
                        continue;

                    sb.AppendLine($"                _{column.Name},");
                }

                sb.AppendLine($"            }};");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// Gets the value of the primary key.");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        /// <returns>The value of the primary key.</returns>");
                sb.AppendLine($"        public override object GetPKValue()");
                sb.AppendLine($"        {{");
                sb.Append($"            return ");
                sb.AppendLine(pk_column == null ? "null; " : $"{pk_column.Name}; ");
                sb.AppendLine($"        }}");

                if (database.ImplementINotifyPropertyChanged)
                {
                    sb.AppendLine();
                    sb.AppendLine("        /// <summary>");
                    sb.AppendLine("        /// Raises the PropertyChanged event if set.");
                    sb.AppendLine("        /// </summary>");
                    sb.AppendLine("        /// <param name=\"name\">Name of the property being raised.  Will default to the current calling property</param>");
                    sb.AppendLine("        protected void OnPropertyChanged([CallerMemberName] string name = null)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));");
                    sb.AppendLine("        }");
                }


                sb.AppendLine($"    }}");
            }
/*
 *
 




 *
 */

            sb.AppendLine($"}}");

            return sb.ToString();

        }

        
    }



}

