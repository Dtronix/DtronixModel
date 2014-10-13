﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Win32;
using DtxModeler.Ddl;
using System.Xml.Serialization;
using System.Threading;
using System.Collections.ObjectModel;
using DtxModeler.Generator.Sqlite;
using DtxModeler.Generator;
using System.IO;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DtxModeler.Xaml {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private object deserialized_clipboard;

		private TypeTransformer type_transformer = new SqliteTypeTransformer();
		

		public MainWindow() {
			InitializeComponent();

			ColumnDbType.ItemsSource = type_transformer.DbTypes();

			ColumnNetType.ItemsSource = Enum.GetValues(typeof(NetTypes)).Cast<NetTypes>();

			BindCommand(ApplicationCommands.New, new KeyGesture(Key.N, ModifierKeys.Control), Command_New);
			BindCommand(ApplicationCommands.Open, new KeyGesture(Key.O, ModifierKeys.Control), Command_Open);
			BindCommand(ApplicationCommands.Save, new KeyGesture(Key.S, ModifierKeys.Control), Command_Save);
			BindCommand(ApplicationCommands.SaveAs, new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Alt), Command_SaveAs);
			BindCommand(ApplicationCommands.Close, new KeyGesture(Key.F4, ModifierKeys.Alt), Command_Exit);
		}

		private void BindCommand(ICommand command, KeyGesture gesture, ExecutedRoutedEventHandler event_handler) {
			InputBindings.Add(new InputBinding(command, gesture));
			CommandBinding cb = new CommandBinding(command);
			cb.Executed += event_handler;

			CommandBindings.Add(cb);
		}



		private void Command_New(object obSender, ExecutedRoutedEventArgs e) {
			_DatabaseExplorer.CreateDatabase();
		}

		private void Command_Open(object obSender, ExecutedRoutedEventArgs e) {
			_DatabaseExplorer.LoadDatabase();
		}

		private void Command_Save(object obSender, ExecutedRoutedEventArgs e) {
			_DatabaseExplorer.Save();
			UpdateTitle();
		}

		private void Command_SaveAs(object obSender, ExecutedRoutedEventArgs e) {
			_DatabaseExplorer.Save(true);
			UpdateTitle();
		}

		private void Command_Exit(object obSender, ExecutedRoutedEventArgs e) {
			Exit(new System.ComponentModel.CancelEventArgs());
		}

		
		private void Exit(System.ComponentModel.CancelEventArgs e) {
			if (_DatabaseExplorer.CloseAllDatabases() == false) {
				e.Cancel = true;
			}
		}

		private Column GetSelectedColumn() {
			return _dagColumnDefinitions.SelectedItem as Column;
		}

		private Column[] GetSelectedColumns() {
			try {
				return _dagColumnDefinitions.SelectedItems.Cast<Column>().ToArray();
			} catch {
				return new Column[0];
			}
		}


		void TableColumn_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
			Column column = sender as Column;
			_DatabaseExplorer.SelectedDatabase._Modified = true;

			// Temporarily remove this event so that we do not get stuck with a stack overflow.
			column.PropertyChanged -= TableColumn_PropertyChanged;


			switch (e.PropertyName) {
				case "DbType":
					column.NetType = type_transformer.DbToNetType(column.DbType);
					break;

				case "NetType":
					column.DbType = type_transformer.NetToDbType(column.NetType);
					break;

				case "IsAutoIncrement":
					if (column.IsAutoIncrement) {

						if (new NetTypes[] { NetTypes.Int16, NetTypes.Int32, NetTypes.Int64 }.Contains(column.NetType) == false) {
							MessageBox.Show("An auto incremented column has to be an integer type.", "Invalid Option");
							column.IsAutoIncrement = false;
						} else if (column.Nullable) {
							MessageBox.Show("Can not auto increment a column that is nullable.", "Invalid Option");
							column.IsAutoIncrement = false;
						}
					}
					break;

				case "Nullable":
					if (column.Nullable && column.IsAutoIncrement) {
						MessageBox.Show("Can not make an auto incremented value nullable.", "Invalid Option");
						column.Nullable = false;
					}
					break;

				case "DefaultValue":
					if (column.IsAutoIncrement) {
						MessageBox.Show("An auto incremented value can not have a default value.", "Invalid Option");
						column.DefaultValue = null;
					}
					break;
			}





			// Rebind this event to allow us to listen again.
			column.PropertyChanged += TableColumn_PropertyChanged;
		}


		private void _DatabaseExplorer_LoadedDatabase(object sender, ExplorerControl.DatabaseEventArgs e) {
			foreach (var table in e.Database.Table) {
				Utilities.BindChangedCollection<Column>(table.Column, null, TableColumn_PropertyChanged);
			}
		}


		private void _DatabaseExplorer_UnloadedDatabase(object sender, ExplorerControl.DatabaseEventArgs e) {
			_DagConfigurations.ItemsSource = null;
		}

		private void _DatabaseExplorer_ChangedSelection(object sender, ExplorerControl.SelectionChangedEventArgs e) {
			_dagColumnDefinitions.ItemsSource = null;
			_LstAssociations.ItemsSource = null;
			_TxtTableDescription.IsEnabled = false;
			_TxtTableDescription.Text = _TxtColumnDescription.Text = "";

			if (e.SelectionType != ExplorerControl.Selection.None) {
				_DagConfigurations.ItemsSource = e.Database.Configuration;
			}


			if (e.SelectionType == ExplorerControl.Selection.TableItem) {
				_dagColumnDefinitions.ItemsSource = e.Table.Column;
				_LstAssociations.ItemsSource = e.Database.Association;
				//_tabTable.IsSelected = true;

				_TxtTableDescription.IsEnabled = true;
				_TxtTableDescription.DataContext = e.Table;

				_LstAssociations.ItemsSource = e.Database.GetAssociations(e.Table);
			} else if (e.SelectionType == ExplorerControl.Selection.Database) {
				//_tabConfig.IsSelected = true;
			}

			UpdateTitle();
		}


		private void _DatabaseExplorer_DatabaseModified(object sender, ExplorerControl.DatabaseEventArgs e) {
			UpdateTitle();
		}

		private void UpdateTitle() {
			var database = _DatabaseExplorer.SelectedDatabase;

			if(database != null){
				if (database._FileLocation != null) {
					this.Title = "Dtronix Modeler - " + database.Name + " (" + Path.GetFileName(database._FileLocation) + ")";
				} else {
					this.Title = "Dtronix Modeler - " + database.Name + "(Usaved)";
				}

				if (database._Modified) {
					this.Title += " [Unsaved Changes]";
				}
			} else {
				this.Title = "Dtronix Modeler";
			}
		}

		private void _dagColumnDefinitions_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			var column = GetSelectedColumn();
			_TxtColumnDescription.IsEnabled = _CmiDeleteColumn.IsEnabled = _CmiCopyColumn.IsEnabled = _CmiPasteColumn.IsEnabled = _CmiMoveColumnDown.IsEnabled = _CmiMoveColumnUp.IsEnabled = false;
			
			if (column != null) {

				if (_dagColumnDefinitions.SelectedItems.Count == 1) {
					_CmiCreateAssociationWith.IsEnabled = true;
				} else {
					_CmiCreateAssociationWith.IsEnabled = false;
				}

				_CmiDeleteColumn.IsEnabled = _CmiCopyColumn.IsEnabled = _CmiMoveColumnDown.IsEnabled = _CmiMoveColumnUp.IsEnabled = true;

				// Only allow paste if the clipboard is valid XML.
				if (Clipboard.ContainsText() && (deserialized_clipboard = Utilities.XmlDeserializeString<DtxModeler.Ddl.Column[]>(Clipboard.GetText())) != null) {
					_CmiPasteColumn.IsEnabled = true;
				}
			}
		}


		private void _CmiDeleteColumn_Click(object sender, RoutedEventArgs e) {
			var columns = GetSelectedColumns();
			string text_columns = "";
			foreach (var column in columns) {
				text_columns += column.Name + "\r\n";
			}

			var result = MessageBox.Show("Are you sure you want to delete the following columns? \r\n" + text_columns, "Confirm Column Deletion", MessageBoxButton.YesNo);

			if (result != MessageBoxResult.Yes) {
				return;
			}

			foreach (var column in columns) {
				_DatabaseExplorer.SelectedTable.Column.Remove(column);
			}

		}

		private void _CmiMoveColumnUp_Click(object sender, RoutedEventArgs e) {
			var columns = GetSelectedColumns();
			var all_columns = _DatabaseExplorer.SelectedTable.Column;

			foreach (var column in columns) {

				int old_index = all_columns.IndexOf(column);

				if (old_index <= 0) {
					return;
				}

				all_columns.Move(old_index, old_index - 1);
			}
		}

		private void _CmiMoveColumnDown_Click(object sender, RoutedEventArgs e) {
			var columns = GetSelectedColumns();
			var all_columns = _DatabaseExplorer.SelectedTable.Column;

			foreach (var column in columns) {
				int old_index = all_columns.IndexOf(column);
				int max = all_columns.Count - 1;

				if (old_index >= max) {
					return;
				}

				all_columns.Move(old_index, old_index + 1);
			}

		}

		private void _CmiCopyColumn_Click(object sender, RoutedEventArgs e) {
			var text = Utilities.XmlSerializeObject(GetSelectedColumns());
			Clipboard.SetText(text, TextDataFormat.UnicodeText);
		}

		private void _CmiPasteColumn_Click(object sender, RoutedEventArgs e) {
			Column[] columns = deserialized_clipboard as Column[];
			var all_columns = _DatabaseExplorer.SelectedTable.Column;
			if (columns == null) {
				return;
			}
			
			if (_MiValidateColumnsOnPaste.IsChecked) {
				foreach (var column in columns) {
					var found_column = all_columns.FirstOrDefault(col => col.Name.ToLower() == column.Name.ToLower());

					if (found_column != null) {
						InputDialogBox.Show("Column Naming Collision", "Enter a new name for the old \"" + found_column.Name + "\" Column.", found_column.Name, value => {
							column.Name = value;
						});

						continue;
					}
				}
			}


			// Add in reverse order to allow for insertion in logical order.
			for (int i = columns.Length - 1; i >= 0; i--) {
				_DatabaseExplorer.SelectedTable.Column.Insert(_dagColumnDefinitions.SelectedIndex + 1, columns[i]);
			}
			
		}



		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			Exit(e);
		}


		private void _MiCommandlineToggle_Click(object sender, RoutedEventArgs e) {
			if (_MiCommandlineToggle.IsChecked) {
				Program.CommandlineShow();
			} else {
				Program.CommandlineHide();
			}
			
		}

		private void MenuItem_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			_MiCommandlineToggle.IsChecked = Program.CommandlineVisible;
		}

		private void OutputGenerateAll_Click(object sender, RoutedEventArgs e) {
			Program.ExecuteOptions(GenerateOptions(), _DatabaseExplorer.SelectedDatabase);
		}

		private ModelGenOptions GenerateOptions() {
			var database = _DatabaseExplorer.SelectedDatabase;
			var options = new ModelGenOptions(null) {
				DbType = "sqlite",
				InputType = "ddl"
			};

			string base_ddl_filename = Path.Combine(Path.GetDirectoryName( database._FileLocation), 
				Path.GetFileNameWithoutExtension( database._FileLocation));

			// If we are set to output in the ddl, then set a default name.
			if (database.GetConfiguration<bool>("output.sql_tables", false)) {
				options.SqlOutput = base_ddl_filename + ".sql";
			}

			// If we are set to output in the ddl, then set a default name.
			if (database.GetConfiguration<bool>("output.cs_classes", true)) {
				options.CodeOutput = base_ddl_filename + ".cs";
			}

			return options;
		}

		private void AssociationDelete_Click(object sender, RoutedEventArgs e) {
			var association = _LstAssociations.SelectedItem as Association;
			var database = _DatabaseExplorer.SelectedDatabase;

			var result = MessageBox.Show("Are you sure you want to delete the association " + association.DisplayName + "?", "Confirm", MessageBoxButton.YesNo);

			if (result == MessageBoxResult.Yes) {
				database.Association.Remove(association);
				_LstAssociations.ItemsSource = database.GetAssociations(_DatabaseExplorer.SelectedTable);
			}
		}

		private void AssociationCreate_Click(object sender, RoutedEventArgs e) {
			var association = new Association();
			var database = _DatabaseExplorer.SelectedDatabase;
			var column = GetSelectedColumn();

			if (_DatabaseExplorer.SelectedTable != null) {
				association.Table1 = _DatabaseExplorer.SelectedTable.Name;
			}

			if (column != null) {
				association.Table1Column = column.Name;
			}

			if (_DatabaseExplorer.SelectedTable != null) {
				association.Table1 = _DatabaseExplorer.SelectedTable.Name;
			}

			// See if we can determine what other table and column this column is referencing.
			if (column != null && column.Name.Contains('_')) {
				int index = column.Name.IndexOf('_');
				string sel_table = column.Name.Substring(0, index);
				string sel_column = column.Name.Substring(index + 1);
				Table found_table = null;

				if ((found_table = database.Table.FirstOrDefault(t => t.Name == sel_table)) != null) {
					association.Table2 = sel_table;

					if (found_table.Column.FirstOrDefault(c => c.Name == sel_column) != null) {
						association.Table2Column = sel_column;
						association.Table1Cardinality = Cardinality.Many;
						association.Table2Cardinality = Cardinality.One;
					}
				}
			}


			var association_window = new AssociationWindow(database, association);
			association_window.Owner = this;

			if (association_window.ShowDialog() == true) {
				database.Association.Add(association_window.Association);
				_LstAssociations.ItemsSource = database.GetAssociations(_DatabaseExplorer.SelectedTable);
			}
		}

		private void AssociationEdit_Click(object sender, RoutedEventArgs e) {
			var original_association = _LstAssociations.SelectedItem as Association;
			var database = _DatabaseExplorer.SelectedDatabase;

			var association_window = new AssociationWindow(database, original_association.Clone());
			association_window.Owner = this;

			if (association_window.ShowDialog() == true) {
				database.Association.Remove(original_association);
				database.Association.Add(association_window.Association);
				_LstAssociations.ItemsSource = database.GetAssociations(_DatabaseExplorer.SelectedTable);
			}
		}

		private void _LstAssociations_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			_CmiCreateAssociation.IsEnabled = _CmiDeleteAssociation.IsEnabled = _CmiEditAssociation.IsEnabled = false;

			if (_DatabaseExplorer.SelectedTable != null) {
				_CmiCreateAssociation.IsEnabled = true;
			}

			var association = _LstAssociations.SelectedItem as Association;

			if (association != null) {
				_CmiDeleteAssociation.IsEnabled = _CmiEditAssociation.IsEnabled = true;
			}
		}

		private void _dagColumnDefinitions_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var columns = GetSelectedColumns();
			if (columns.Length == 1) {
				_TxtColumnDescription.IsEnabled = true;
				_TxtColumnDescription.DataContext = columns[0];
			} else {
				_TxtColumnDescription.IsEnabled = false;
				_TxtColumnDescription.DataContext = null;
			}

		}

		private void Window_KeyUp(object sender, KeyEventArgs e) {
			if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.S) {
				_DatabaseExplorer.Save();
				UpdateTitle();
			}
		}

	}

}
